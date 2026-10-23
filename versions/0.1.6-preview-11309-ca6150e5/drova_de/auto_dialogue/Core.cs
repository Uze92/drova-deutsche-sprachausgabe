using Drova_Modding_API.Access;
using HarmonyLib;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
using MelonLoader;
using UnityEngine;
using Il2CppDrova.GUI.Dialogue;

[assembly: MelonInfo(typeof(DrovaAutoDialogue.Core), "Drova - Automatische Dialoge", "0.1.3", "Benchler")]
[assembly: MelonGame("Just2D", "Drova")]
[assembly: MelonAdditionalDependencies("Drova_Modding_API")]

namespace DrovaAutoDialogue;

public sealed class Core : MelonMod
{
    public const string EnabledKey = "Benchler_AutoDialogue_Enabled";
    public const string DelayKey = "Benchler_AutoDialogue_DelayMs";
    public const int DefaultDelayMs = 500;
    const string Localization = "BenchlerAutoDialogue";
    static Pending? _pending;

    sealed class Pending
    {
        public readonly DS_DialogueUGUI UI;
        public readonly IntPtr Node;
        public readonly AudioClip Clip;
        public readonly IDialogueActor? Actor;
        public readonly AudioEndGate Gate = new();
        public IntPtr Source;
        public bool Ready;
        public Pending(DS_DialogueUGUI ui, IntPtr node, AudioClip clip,IDialogueActor? actor)
        { UI = ui; Node = node; Clip = clip; Actor=actor; }
    }

    public override void OnInitializeMelon()
    {
        OptionMenuAccess.Instance.OnOptionMenuOpen += BuildOptions;
        HarmonyInstance.Patch(AccessTools.Method(typeof(DS_DialogueUGUI), nameof(DS_DialogueUGUI.OnSubtitlesRequest)),
            postfix: new HarmonyMethod(typeof(Core), nameof(Subtitle)));
        HarmonyInstance.Patch(AccessTools.Method(typeof(GUI_DialogueInputHandler), nameof(GUI_DialogueInputHandler.CheckShouldSkipDialog)),
            postfix: new HarmonyMethod(typeof(Core), nameof(AfterContinueCheck)));
        foreach (string method in new[] { "OnMultipleChoiceRequest", "OnDialogueFinished", "OnDialoguePaused", "OnDisable", "ResetUI" })
            HarmonyInstance.Patch(AccessTools.Method(typeof(DS_DialogueUGUI), method),
                prefix: new HarmonyMethod(typeof(Core), nameof(Cancel)));
        LoggerInstance.Msg("Automatische Dialoge bereit. Schalter: Optionen > Modding > Automatische Dialoge.");
    }

    static void BuildOptions()
    {
        try
        {
            var builder = OptionMenuAccess.Instance.GetBuilder(Localization);
            if (builder == null) return;
            builder.CreateTitle(LocalizationAccess.GetLocalizedString(Localization, "Title"))
                .CreateSwitch(LocalizationAccess.GetLocalizedString(Localization, "Enabled"),
                    LocalizationAccess.GetLocalizedString(Localization, "On"),
                    LocalizationAccess.GetLocalizedString(Localization, "Off"), EnabledKey, true)
                .CreateSlider(LocalizationAccess.GetLocalizedString(Localization, "Delay"), DelayKey, 0, 1500, DefaultDelayMs)
                .CreateDisclaimer(LocalizationAccess.GetLocalizedString(Localization, "Help"))
                .Build();
        }
        catch (Exception error) { MelonLogger.Warning("Auto-Dialog-Menü konnte nicht erstellt werden: " + error.Message); }
    }

    static bool Enabled() => !ConfigAccessor.TryGetConfigValue(EnabledKey, out bool value) || value;

    public static void Cancel(DS_DialogueUGUI __instance)
    {
        if (_pending?.UI.Pointer == __instance.Pointer) _pending = null;
    }

    public static void Subtitle(DS_DialogueUGUI __instance, SubtitlesRequestInfo info)
    {
        _pending = null;
        try
        {
            if (!Enabled() || !ConfigAccessor.TryGetConfigValue("EnableDialogueAudio", out bool audioEnabled)
                || !audioEnabled || info?.statement?.audio == null || __instance.DLGTree == null) return;
            var node = __instance.GetCurrentNode(__instance.DLGTree)?.TryCast<DS_StatementNode>();
            if (node == null) return; // Never advance an answer selection or a hub.
            _pending = new Pending(__instance, node.Pointer, info.statement.audio,info.actor);
            MelonLogger.Msg("[AutoDialog] Vertonte Zeile erfasst; warte auf Wiedergabeende.");
        }
        catch (Exception error) { MelonLogger.Warning("Auto-Dialog wartet auf manuelle Eingabe: " + error.Message); }
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName) => _pending = null;

    public override void OnUpdate()
    {
        var pending = _pending;
        if (pending == null) return;
        try
        {
            if (!pending.UI || !pending.UI.isActiveAndEnabled || !pending.Clip || !Enabled() || !ConfigAccessor.TryGetConfigValue("EnableDialogueAudio",out bool audioEnabled) || !audioEnabled) { _pending = null; return; }
            // A menu pauses our decision even when the game's audio continues underneath it.
            if (OptionMenuAccess.Instance.IsMenuOpen || Time.timeScale <= 0 || AudioListener.pause || !Application.isFocused) return;
            var ui = pending.UI;
            if (ui.DLGTree == null || ui.GetCurrentNode(ui.DLGTree)?.Pointer != pending.Node)
            { _pending = null; return; }
            var window = ui.WindowMgr?.GetCurrentWindow();
            if (window == null || !window || !window.gameObject.activeInHierarchy) return;
            // Native HandleAudio prefers the speaker's AudioSource, falling back
            // to the UI source. Watching the fallback alone misses voiced NPCs.
            var source = pending.Actor?.transform?.GetComponent<AudioSource>();
            if(!source)source=ui.localSource;
            if (source == null || !source) return;
            var playingClip = source.clip;
            if (playingClip == null || !playingClip || playingClip.Pointer != pending.Clip.Pointer) return;
            if (pending.Source != IntPtr.Zero && pending.Source != source.Pointer)
            { _pending = null; return; }
            pending.Source = source.Pointer;
            int delay = ConfigAccessor.TryGetConfigValue(DelayKey, out int milliseconds) ? milliseconds : DefaultDelayMs;
            bool send = pending.Gate.Tick(source.isPlaying, source.time, pending.Clip.length,
                Time.unscaledDeltaTime, false, Math.Clamp(delay, 0, 1500) / 1000.0);
            if (pending.Gate.Cancelled)
            { _pending = null; MelonLogger.Msg("[AutoDialog] Wiedergabe unterbrochen oder nicht gestartet; manuell fortsetzen."); return; }
            if (!send) return;
            if (ui.InputHandler == null) { _pending = null; return; }
            // _inputDown controls text skipping only. The native HandleWaitIndicator
            // coroutine polls CheckShouldSkipDialog instead (verified in GameAssembly).
            // Keep readiness until the game consumes it and leaves this exact node.
            pending.Ready = true;
            MelonLogger.Msg("[AutoDialog] Aufnahme beendet; automatische Weitergabe bereit.");
        }
        catch (Exception error)
        {
            _pending = null;
            MelonLogger.Warning("Auto-Dialog sicher angehalten; manuell fortsetzen: " + error.Message);
        }
    }

    public static void AfterContinueCheck(GUI_DialogueInputHandler __instance, ref bool __result)
    {
        var pending = _pending;
        if (pending == null) return;
        try
        {
            var ui = pending.UI;
            if (!ui || ui.InputHandler == null || ui.InputHandler.Pointer != __instance.Pointer) return;
            // Real manual input wins and invalidates any delayed auto action.
            if (__result) { _pending = null; return; }
            if(!ConfigAccessor.TryGetConfigValue("EnableDialogueAudio",out bool audioEnabled)||!audioEnabled){_pending=null;return;}
            if (!pending.Ready || !Enabled() || OptionMenuAccess.Instance.IsMenuOpen
                || Time.timeScale <= 0 || AudioListener.pause || !Application.isFocused) return;
            if (ui.DLGTree == null || ui.GetCurrentNode(ui.DLGTree)?.Pointer != pending.Node)
            { _pending = null; return; }
            var window = ui.WindowMgr?.GetCurrentWindow();
            if (window == null || !window || !window.gameObject.activeInHierarchy) return;
            var source=pending.Actor?.transform?.GetComponent<AudioSource>();
            if(!source)source=ui.localSource;
            if(!ui.isActiveAndEnabled || !source || source!.Pointer!=pending.Source || !source.clip || source.clip!.Pointer!=pending.Clip.Pointer || source.isPlaying)
            {_pending=null;return;}
            __result = true;
        }
        catch (Exception error)
        {
            _pending = null;
            MelonLogger.Warning("Auto-Dialog-Weitergabe angehalten: " + error.Message);
        }
    }

    public override void OnDeinitializeMelon()
    {
        _pending = null;
        OptionMenuAccess.Instance.OnOptionMenuOpen -= BuildOptions;
    }
}
