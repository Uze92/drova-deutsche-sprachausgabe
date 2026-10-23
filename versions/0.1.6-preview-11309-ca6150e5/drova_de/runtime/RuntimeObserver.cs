using System.Reflection;
using System.Text.Json;
using Drova_Modding_API.Access;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using UnityEngine;
namespace DrovaVoices;
public static class RuntimeObserver
{
    static string _folder="";
    public static void Install(string folder,HarmonyLib.Harmony harmony)
    {
        _folder=folder;
        var type=typeof(Drova_Modding_API.Systems.Audio.AudioManager).Assembly.GetType("Drova_Modding_API.Systems.Audio.DefaultAudioHandler");
        var method=type?.GetMethod("HandleSubtitleRequest",BindingFlags.Public|BindingFlags.Instance);
        if(method==null){MelonLogger.Warning("Drova subtitle observer unavailable; no complete runtime evidence");return;}
        harmony.Patch(method,postfix:new HarmonyMethod(typeof(RuntimeObserver).GetMethod(nameof(AfterSubtitle))));
    }
    static object? Get(object? value,string name)
    {
        if(value==null)return null;
        return value.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(value)
            ?? value.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(value);
    }
    public static void AfterSubtitle(object[] __args)
    {
        try
        {
            var info=__args.FirstOrDefault();var actor=Get(info,"actor");var statement=Get(info,"statement");
            string name=Get(actor,"name")?.ToString()??"unknown";
            string region="unknown";bool? cave=null;
            try{if(RegionAccess.TryGetCurrentRegion(out var r))region=r.ToString();cave=RegionAccess.IsInCave();}catch{}
            var component=(actor as Il2CppObjectBase)?.TryCast<Component>();
            var sources=component?.GetComponentsInChildren<AudioSource>(true);
            List<object> audio=new();
            if(sources!=null)foreach(var source in sources)
            {
                if(!source)continue;
                audio.Add(new {clip=source.clip?source.clip.name:null,mixer=source.outputAudioMixerGroup?source.outputAudioMixerGroup.name:null,
                    volume=source.volume,pitch=source.pitch,spatialBlend=source.spatialBlend,reverbZoneMix=source.reverbZoneMix,
                    bypassEffects=source.bypassEffects,bypassReverbZones=source.bypassReverbZones,playing=source.isPlaying,
                    position=new {x=source.transform.position.x,y=source.transform.position.y,z=source.transform.position.z}});
            }
            var record=new {observed_utc=DateTime.UtcNow,actor=name,region,cave,
                locaKey=Get(statement,"locaKey")?.ToString(),text=Get(statement,"text")?.ToString(),audio_sources=audio,
                evidence_limit="Sources visible at subtitle dispatch; not all game audio or final acoustic impulse response"};
            File.AppendAllText(Path.Combine(_folder,"runtime-scenes.jsonl"),JsonSerializer.Serialize(record)+"\n");
        }
        catch(Exception error){MelonLogger.Warning("Drova runtime observation failed: "+error.Message);}
    }
}
