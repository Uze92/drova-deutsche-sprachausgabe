// Headless integration fixtures only. No fake game assemblies enter the mod package.
namespace UnityEngine
{
    public class Object
    {
        static int _next;
        public IntPtr Pointer { get; set; } = (IntPtr)(++_next);
        public bool Alive = true;
        public static implicit operator bool(Object? value) => value != null && value.Alive;
    }
    public class GameObject : Object { public bool activeInHierarchy = true; }
    public class AudioClip : Object { public float length = 1; }
    public class Transform : Object {public AudioSource? Source;public T? GetComponent<T>() where T:class => Source as T;}
    public class AudioSource : Object { public AudioClip? clip; public float time; public bool isPlaying; }
    public static class Time { public static float timeScale = 1, unscaledDeltaTime = 0.02f; }
    public static class AudioListener { public static bool pause; }
    public static class Application { public static bool isFocused = true; }
}
namespace HarmonyLib
{
    public class HarmonyMethod { public HarmonyMethod(Type type, string name) {} }
    public class Harmony { public void Patch(object original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null) {} }
    public static class AccessTools { public static object Method(Type type, string name) => new(); }
}
namespace MelonLoader
{
    [AttributeUsage(AttributeTargets.Assembly)] public class MelonInfoAttribute : Attribute { public MelonInfoAttribute(Type type, string name, string version, string author) {} }
    [AttributeUsage(AttributeTargets.Assembly)] public class MelonGameAttribute : Attribute { public MelonGameAttribute(string studio, string game) {} }
    [AttributeUsage(AttributeTargets.Assembly)] public class MelonAdditionalDependenciesAttribute : Attribute { public MelonAdditionalDependenciesAttribute(string name) {} }
    public class Log { public void Msg(string message) {} }
    public static class MelonLogger { public static void Msg(string message) {} public static void Warning(string message) => throw new Exception("Unexpected warning: " + message); }
    public class MelonMod
    {
        public Log LoggerInstance = new(); public HarmonyLib.Harmony HarmonyInstance = new();
        public virtual void OnInitializeMelon() {} public virtual void OnSceneWasLoaded(int i, string scene) {}
        public virtual void OnUpdate() {} public virtual void OnDeinitializeMelon() {}
    }
}
namespace Il2CppNodeCanvas.DialogueTrees
{
    public class Node : UnityEngine.Object { public T? TryCast<T>() where T : class => this as T; }
    public class DS_StatementNode : Node {}
    public class DialogueTree : UnityEngine.Object { public Node? current; }
    public class Statement { public UnityEngine.AudioClip? audio; }
    public class IDialogueActor {public UnityEngine.Transform? transform;}
    public class SubtitlesRequestInfo { public Statement statement = new(); public IDialogueActor? actor; }
}
namespace Il2CppNodeCanvas.DialogueTrees.UI
{
    using Il2CppNodeCanvas.DialogueTrees;
    public class Window : UnityEngine.Object { public UnityEngine.GameObject gameObject = new(); }
    public class Windows { public Window? Current = new(); public Window? GetCurrentWindow() => Current; }
    public class DS_DialogueUGUI : UnityEngine.Object
    {
        public bool isActiveAndEnabled=true;
        public DialogueTree? DLGTree = new(); public Windows? WindowMgr = new();
        public UnityEngine.AudioSource? localSource = new(); public Il2CppDrova.GUI.Dialogue.GUI_DialogueInputHandler? InputHandler = new();
        public Node? GetCurrentNode(DialogueTree tree) => tree.current;
        public void OnSubtitlesRequest(SubtitlesRequestInfo info) {}
    }
}
namespace Il2CppDrova.GUI.Dialogue
{
    public class GUI_DialogueInputHandler : UnityEngine.Object
    {
        public bool _inputDown;
        // Mirrors the relevant native distinction: text-skip state is NOT the
        // continue result. Native CheckShouldSkipDialog polls input actions.
        public bool CheckShouldSkipDialog() => false;
    }
}
namespace Drova_Modding_API.Access
{
    public static class ConfigAccessor
    {
        public static Dictionary<string, object> Values = new();
        public static bool TryGetConfigValue<T>(string key, out T value)
        {
            if (Values.TryGetValue(key, out var obj) && obj is T typed) { value = typed; return true; }
            value = default!; return false;
        }
    }
    public class Builder
    {
        public List<string> Labels = new();
        public Builder CreateTitle(string title) { Labels.Add(title); return this; }
        public Builder CreateSwitch(string title, string on, string off, string key, bool value)
        { Labels.Add(title); if (!ConfigAccessor.Values.ContainsKey(key)) ConfigAccessor.Values[key] = value; return this; }
        public Builder CreateSlider(string title, string key, int min, int max, int value)
        { Labels.Add(title); if (!ConfigAccessor.Values.ContainsKey(key)) ConfigAccessor.Values[key] = value; return this; }
        public Builder CreateDisclaimer(string text) { Labels.Add(text); return this; }
        public void Build() {}
    }
    public class OptionMenuAccess
    {
        public static OptionMenuAccess Instance = new(); public bool IsMenuOpen;
        public event Action? OnOptionMenuOpen;
        public Builder Last = new();
        public Builder? GetBuilder(string key) { Last = new(); return Last; }
        public void Open() => OnOptionMenuOpen?.Invoke();
    }
    public static class LocalizationAccess { public static string GetLocalizedString(string group, string key) => key; }
}
