namespace MelonLoader
{
    public sealed class MelonInfoAttribute(Type type,string name,string version,string author):Attribute {}
    public sealed class MelonGameAttribute(string company,string game):Attribute {}
    public sealed class MelonAdditionalDependenciesAttribute(params string[] names):Attribute {}
    public class MelonMod
    {
        public TestLogger LoggerInstance {get;}=new();
        public object HarmonyInstance {get;}=new();
        public virtual void OnInitializeMelon() {}
        public virtual void OnSceneWasLoaded(int index,string name) {}
        public virtual void OnUpdate() {}
        public virtual void OnDeinitializeMelon() {}
    }
    public sealed class TestLogger
    {
        public List<string> Messages {get;}=new();
        public void Msg(string value)=>Messages.Add(value);
        public void Error(string value)=>throw new Exception(value);
    }
}
namespace MelonLoader.Utils { public static class MelonEnvironment {public static string GameRootDirectory="";} }
namespace Il2CppInterop.Runtime.InteropTypes.Arrays {public sealed class Il2CppStructArray<T>(T[] data) {} }
namespace UnityEngine
{
    public sealed class AudioClip
    {
        public bool Alive=true;
        public static implicit operator bool(AudioClip? clip)=>clip!=null&&clip.Alive;
        public static AudioClip Create(string name,int count,int channels,int rate,bool stream)=>new();
        public bool SetData(object samples,int offset)=>true;
    }
}
namespace Drova_Modding_API.Access
{
    public static class ConfigAccessor
    {
        public static bool Enabled;
        public static bool TryGetConfigValue(string name,out bool value){value=Enabled;return true;}
    }
}
namespace Drova_Modding_API.Systems.WorldEvents
{
    public sealed class WorldEventSystemManager {public static WorldEventSystemManager? Instance;}
}
namespace Drova_Modding_API.Systems.Audio
{
    public interface IAudioProvider {Task<UnityEngine.AudioClip> GetAudioClip(string tree,string path,string key,string actor,int? choice);}
    public sealed class DefaultDialogueAudioConnector(IAudioProvider provider) {}
    public sealed class StartupSensitiveDefaultProvider:IAudioProvider
    {
        public StartupSensitiveDefaultProvider()
        {
            if(WorldEvents.WorldEventSystemManager.Instance==null)throw new InvalidOperationException("The world does not exist yet");
            Probe.StaticInitializations++;
        }
        public Task<UnityEngine.AudioClip> GetAudioClip(string tree,string path,string key,string actor,int? choice)=>Task.FromResult<UnityEngine.AudioClip>(null!);
    }
    public static class Probe {public static int StaticInitializations;public static int Connections;}
    public static class AudioManager
    {
        // Same beforefieldinit pattern as the real API; this throws permanently
        // when touched during OnInitializeMelon instead of gameplay readiness.
        static DefaultDialogueAudioConnector _connector=new(new StartupSensitiveDefaultProvider());
        public static void ReplaceDialogueAudioConnector(DefaultDialogueAudioConnector value){_connector=value;Probe.Connections++;}
    }
}
namespace DrovaVoices
{
    public static class PlaybackGuard
    {
        public static void RegisterClip(UnityEngine.AudioClip clip) {}
        public static void Install(object harmony) {}
        public static void Tick() {}
        public static void ResetAll() {}
    }
    public static class RuntimeObserver
    {
        public static int Installs;
        public static void Install(string folder,object harmony)=>Installs++;
    }
}
