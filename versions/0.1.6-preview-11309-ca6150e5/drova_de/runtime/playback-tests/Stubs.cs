// Headless fixtures; these types never enter the installed DLL.
namespace Il2CppSystem
{
    public class Object
    {
        static int next;
        public IntPtr Pointer=(IntPtr)(++next);
        public T? TryCast<T>() where T:class=>this as T;
    }
}
namespace Il2CppSystem.Collections { public class IEnumerator : Il2CppSystem.Object {} }
namespace UnityEngine
{
    public class Object : Il2CppSystem.Object
    {
        public bool Alive=true;
        public static implicit operator bool(Object? obj)=>obj!=null&&obj.Alive;
    }
    public class AudioClip : Object {}
    public class AudioSource : Object
    {
        public bool playOnAwake=true,loop,isPlaying;
        public AudioClip? clip;
        public int Starts,Stops;
        public void Stop(){isPlaying=false;Stops++;}
        public void Play(){if(clip){isPlaying=true;Starts++;}}
        public void Reactivate(){if(playOnAwake)Play();}
    }
    public class Transform : Object
    {
        public AudioSource? Source;
        public T? GetComponent<T>() where T:class=>Source as T;
    }
}
namespace Drova_Modding_API.Access
{
    public static class ConfigAccessor
    {
        public static bool Enabled=true;
        public static bool TryGetConfigValue(string key,out bool value){value=Enabled;return true;}
    }
}
namespace HarmonyLib
{
    public class HarmonyMethod(Type type,string name) {}
    public static class AccessTools {public static object Method(Type type,string name)=>new();}
    public class Harmony {public int Count;public void Patch(object target,HarmonyMethod? prefix=null,HarmonyMethod? postfix=null){Count++;}}
}
namespace Il2CppNodeCanvas.DialogueTrees
{
    public class IDialogueActor {public UnityEngine.Transform? transform;}
    public class Statement {public UnityEngine.AudioClip? audio;}
    public class SubtitlesRequestInfo : Il2CppSystem.Object {public Statement statement=new();}
}
namespace Il2CppNodeCanvas.DialogueTrees.UI
{
    using UnityEngine;
    public class DS_DialogueUGUI : Object
    {
        public bool isActiveAndEnabled=true;
        public AudioSource localSource=new();
        public class _HandleAudio_d__50 : Il2CppSystem.Collections.IEnumerator
        {public DS_DialogueUGUI __4__this=null!;public AudioClip audio=null!;public IDialogueActor? actor;public int __1__state;public Il2CppSystem.Object? __2__current;}
        public class _PlayAudio_d__51 : Il2CppSystem.Collections.IEnumerator
        {public DS_DialogueUGUI __4__this=null!;public AudioClip audio=null!;public AudioSource playSource=null!;}
        public class _StartRoutine_d__45 : Il2CppSystem.Object
        {public DS_DialogueUGUI __4__this=null!;public SubtitlesRequestInfo info=null!;}
        public class _HandleSubTitleRequest_d__63 : Il2CppSystem.Object
        {public DS_DialogueUGUI __4__this=null!;public SubtitlesRequestInfo info=null!;public Il2CppSystem.Object? __2__current;}
    }
}
