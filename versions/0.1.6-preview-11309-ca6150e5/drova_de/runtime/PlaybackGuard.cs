using Drova_Modding_API.Access;
using HarmonyLib;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
using UnityEngine;

namespace DrovaVoices;

// Own generated clips only. A request is a playback lifetime, never a permanent
// blacklist: deliberately asking for the same dialogue again must still work.
public static class PlaybackGuard
{
    sealed class Lease
    {
        public readonly DS_DialogueUGUI UI;
        public readonly AudioClip Clip;
        public readonly IntPtr Request;
        public bool Muted;
        public Lease(DS_DialogueUGUI ui,AudioClip clip,IntPtr request){UI=ui;Clip=clip;Request=request;}
        public readonly Dictionary<IntPtr,BorrowedSource> Sources=new();
    }
    sealed class BorrowedSource
    {
        public readonly AudioSource Source;
        public readonly bool Awake,Loop;
        public BorrowedSource(AudioSource source){Source=source;Awake=source.playOnAwake;Loop=source.loop;}
    }
    sealed class Routine
    {
        public readonly Lease Lease;
        public Routine(Lease lease){Lease=lease;}
        public bool Started;
    }
    static readonly Dictionary<IntPtr,AudioClip> Clips=new();
    static readonly Dictionary<IntPtr,Lease> Requests=new();
    static readonly Dictionary<IntPtr,Routine> Routines=new();
    static readonly Dictionary<IntPtr,Lease> SourceOwners=new();
    public static void RegisterClip(AudioClip clip)=>Clips[clip.Pointer]=clip;
    static bool Own(AudioClip? clip)=>clip && Clips.TryGetValue(clip!.Pointer,out var registered)&&registered;
    static bool Enabled()=>ConfigAccessor.TryGetConfigValue("EnableDialogueAudio",out bool enabled)&&enabled;

    public static void Install(HarmonyLib.Harmony harmony)
    {
        var type=typeof(DS_DialogueUGUI);
        harmony.Patch(AccessTools.Method(type,"OnSubtitlesRequest"),prefix:new HarmonyMethod(typeof(PlaybackGuard),nameof(Begin)));
        foreach(string name in new[]{"OnMultipleChoiceRequest","OnDialogueFinished","OnDialoguePaused","OnDisable","ResetUI","OnDialogueStarted"})
            harmony.Patch(AccessTools.Method(type,name),prefix:new HarmonyMethod(typeof(PlaybackGuard),nameof(End)));
        harmony.Patch(AccessTools.Method(typeof(DS_DialogueUGUI._StartRoutine_d__45),"MoveNext"),prefix:new HarmonyMethod(typeof(PlaybackGuard),nameof(BeforeStart)));
        harmony.Patch(AccessTools.Method(typeof(DS_DialogueUGUI._HandleSubTitleRequest_d__63),"MoveNext"),prefix:new HarmonyMethod(typeof(PlaybackGuard),nameof(BeforeSubtitle)),postfix:new HarmonyMethod(typeof(PlaybackGuard),nameof(AfterSubtitle)));
        harmony.Patch(AccessTools.Method(typeof(DS_DialogueUGUI._HandleAudio_d__50),"MoveNext"),
            prefix:new HarmonyMethod(typeof(PlaybackGuard),nameof(BeforeHandle)),postfix:new HarmonyMethod(typeof(PlaybackGuard),nameof(AfterHandle)));
        harmony.Patch(AccessTools.Method(typeof(DS_DialogueUGUI._PlayAudio_d__51),"MoveNext"),
            prefix:new HarmonyMethod(typeof(PlaybackGuard),nameof(BeforeWait)),postfix:new HarmonyMethod(typeof(PlaybackGuard),nameof(AfterWait)));
    }
    public static bool Begin(DS_DialogueUGUI __instance,SubtitlesRequestInfo info)
    {
        if(Own(info?.statement?.audio)&&Requests.TryGetValue(__instance.Pointer,out var existing)&&existing.Request==info!.Pointer)return false;
        End(__instance);
        if(Own(info?.statement?.audio))Requests[__instance.Pointer]=new Lease(__instance,info!.statement.audio!,info.Pointer){Muted=!Enabled()};
        return true;
    }
    public static void End(DS_DialogueUGUI __instance)
    {
        if(!Requests.Remove(__instance.Pointer,out var lease))return;
        foreach(var borrowed in lease.Sources.Values.ToArray())Release(lease,borrowed);
        foreach(var key in Routines.Where(p=>ReferenceEquals(p.Value.Lease,lease)).Select(p=>p.Key).ToArray())Routines.Remove(key);
    }
    static void Release(Lease lease,BorrowedSource borrowed)
    {
        var source=borrowed.Source;
        if(SourceOwners.TryGetValue(source.Pointer,out var owner)&&ReferenceEquals(owner,lease))
        {
            // Another system may have taken the source over. Never stop its audio.
            if(source)
            {
                if(Own(source.clip)) {source.Stop();source.clip=null;source.playOnAwake=borrowed.Awake;source.loop=borrowed.Loop;}
                else if(!source.clip) {source.playOnAwake=borrowed.Awake;source.loop=borrowed.Loop;}
            }
            SourceOwners.Remove(source.Pointer);
        }
        lease.Sources.Remove(source.Pointer);
    }
    public static void AfterSubtitle(DS_DialogueUGUI._HandleSubTitleRequest_d__63 __instance,bool __result)
    {
        // IL2CPP inlines the HandleAudio factory. Bind its yielded iterator here,
        // before Unity first runs it, using the parent's exact request object.
        if(!__result)return;
        var child=__instance.__2__current?.TryCast<DS_DialogueUGUI._HandleAudio_d__50>();
        if(child!=null && Own(child.audio) && Requests.TryGetValue(__instance.__4__this.Pointer,out var lease)
            && lease.Request==__instance.info.Pointer && lease.Clip.Pointer==child.audio.Pointer)
            Routines[child.Pointer]=new Routine(lease);
    }
    static bool Valid(Routine routine,DS_DialogueUGUI ui,AudioClip audio)
        =>ui && ui.isActiveAndEnabled && Enabled() && !routine.Lease.Muted && Requests.TryGetValue(ui.Pointer,out var current)
          && ReferenceEquals(current,routine.Lease)&&current.Clip.Pointer==audio.Pointer;
    static bool ValidRequest(DS_DialogueUGUI ui,SubtitlesRequestInfo info,ref bool result)
    {
        if(!Own(info?.statement?.audio))return true;
        if(ui && ui.isActiveAndEnabled && Requests.TryGetValue(ui.Pointer,out var lease)&&lease.Request==info!.Pointer)return true;
        result=false;return false;
    }
    public static bool BeforeStart(DS_DialogueUGUI._StartRoutine_d__45 __instance,ref bool __result)
        =>ValidRequest(__instance.__4__this,__instance.info,ref __result);
    public static bool BeforeSubtitle(DS_DialogueUGUI._HandleSubTitleRequest_d__63 __instance,ref bool __result)
        =>ValidRequest(__instance.__4__this,__instance.info,ref __result);
    public static bool BeforeHandle(DS_DialogueUGUI._HandleAudio_d__50 __instance,ref bool __result)
    {
        if(!Own(__instance.audio))return true;
        if(!Routines.TryGetValue(__instance.Pointer,out var routine)||!Valid(routine,__instance.__4__this,__instance.audio))
        {__result=false;return false;}
        if(__instance.__1__state!=0)return true;
        if(routine.Started){__result=false;return false;}
        routine.Started=true;
        var source=__instance.actor?.transform?.GetComponent<AudioSource>();
        if(!source)source=__instance.__4__this.localSource;
        if(!source){__result=false;return false;}
        var lease=routine.Lease;
        if(SourceOwners.TryGetValue(source!.Pointer,out var previous)&&!ReferenceEquals(previous,lease))
            Release(previous,previous.Sources[source.Pointer]);
        if(!lease.Sources.ContainsKey(source!.Pointer))lease.Sources[source.Pointer]=new BorrowedSource(source);
        SourceOwners[source.Pointer]=lease;
        // The native routine sets clip and calls Play, but never disables these.
        // Keeping a completed speech clip with playOnAwake can replay on activation.
        source.playOnAwake=false;source.loop=false;
        return true;
    }
    public static void AfterHandle(DS_DialogueUGUI._HandleAudio_d__50 __instance,bool __result)
    {
        if(!Own(__instance.audio))return;
        if(__result && Routines.TryGetValue(__instance.Pointer,out var routine))
        {
            var wait=__instance.__2__current?.TryCast<DS_DialogueUGUI._PlayAudio_d__51>();
            if(wait!=null)Routines[wait.Pointer]=new Routine(routine.Lease);
        }
        if(!__result)Routines.Remove(__instance.Pointer);
    }
    public static bool BeforeWait(DS_DialogueUGUI._PlayAudio_d__51 __instance,ref bool __result)
    {
        if(!Own(__instance.audio))return true;
        if(Routines.TryGetValue(__instance.Pointer,out var routine)&&Valid(routine,__instance.__4__this,__instance.audio)
            && __instance.playSource && __instance.playSource.clip && __instance.playSource.clip!.Pointer==__instance.audio.Pointer)return true;
        __result=false;return false;
    }
    public static void AfterWait(DS_DialogueUGUI._PlayAudio_d__51 __instance,bool __result)
    {if(!__result)Routines.Remove(__instance.Pointer);}
    public static void Tick()
    {
        foreach(var lease in Requests.Values.ToArray())
        {
            if(!lease.UI || !lease.UI.isActiveAndEnabled){End(lease.UI);continue;}
            if(!Enabled())
            {
                lease.Muted=true;
                foreach(var borrowed in lease.Sources.Values.ToArray())Release(lease,borrowed);
                foreach(var key in Routines.Where(p=>ReferenceEquals(p.Value.Lease,lease)).Select(p=>p.Key).ToArray())Routines.Remove(key);
            }
        }
    }
    public static void ResetAll()
    {foreach(var lease in Requests.Values.ToArray())End(lease.UI);Routines.Clear();}
}
