using System.Security.Cryptography;
using System.Text.Json;
using Drova_Modding_API.Access;
using Drova_Modding_API.Systems.Audio;
using Drova_Modding_API.Systems.WorldEvents;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;

[assembly: MelonInfo(typeof(DrovaVoices.Core),"Drova Voices DE","0.1.4","Benchler")]
[assembly: MelonGame("Just2D","Drova")]
[assembly: MelonAdditionalDependencies("Drova_Modding_API")]

namespace DrovaVoices;
public sealed class Route
{
    public string Tree {get;set;}="";public string Path {get;set;}="";public string Key {get;set;}="";
    public string[] ActorAliases {get;set;}=Array.Empty<string>();public string Audio {get;set;}="";
    public string AudioSha256 {get;set;}="";public string VoiceId {get;set;}="";
    public float PlaybackGainDb {get;set;}=0;
}
public sealed class Manifest
{
    public string Mode {get;set;}="observe"; public Route[] Routes {get;set;}=Array.Empty<Route>();
    public Dictionary<string,string> GameSourceHashes {get;set;}=new();
}
public sealed class Core : MelonMod
{
    readonly RuntimeStartGate _start = new();
    WaveProvider? _provider;
    string _folder = "";
    public override void OnInitializeMelon()
    {
        try
        {
            string folder=System.IO.Path.Combine(MelonEnvironment.GameRootDirectory,"Mods","DrovaVoices");
            var manifest=JsonSerializer.Deserialize<Manifest>(File.ReadAllText(System.IO.Path.Combine(folder,"routes.json"))) ?? throw new InvalidDataException("Missing manifest");
            foreach(var pair in manifest.GameSourceHashes)
            {
                string root=System.IO.Path.GetFullPath(MelonEnvironment.GameRootDirectory)+System.IO.Path.DirectorySeparatorChar;string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(root,pair.Key));
                if(!path.StartsWith(root,StringComparison.OrdinalIgnoreCase) || !File.Exists(path) || Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()!=pair.Value)throw new InvalidDataException("Game source changed: "+pair.Key);
            }
            _folder = folder;
            _provider = new WaveProvider(folder,manifest);
            LoggerInstance.Msg("Drova voice manifest ready; waiting for gameplay world, routes="+manifest.Routes.Length);
        }
        catch(Exception error){LoggerInstance.Error("Drova voices disabled: "+error);}
    }
    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    { if(_start.Attempted)PlaybackGuard.ResetAll();TryConnect(); }
    public override void OnUpdate()
    { TryConnect();if(_start.Attempted)PlaybackGuard.Tick(); }
    public override void OnDeinitializeMelon()=>PlaybackGuard.ResetAll();
    void TryConnect()
    {
        if (_provider == null || _start.Attempted) return;
        // The API (priority -1) creates this component on gameplay scene load.
        // Do not touch AudioManager in this method: its beforefieldinit static
        // initializer must not run even when this method is JIT-compiled early.
        if (WorldEventSystemManager.Instance == null) return;
        try { _start.TryStart(true, ConnectReadyProvider); }
        catch(Exception error) { LoggerInstance.Error("Drova voice connection failed: "+error); }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    void ConnectReadyProvider()
    {
        PlaybackGuard.Install(HarmonyInstance);
        AudioManager.ReplaceDialogueAudioConnector(new DefaultDialogueAudioConnector(_provider!));
        RuntimeObserver.Install(_folder,HarmonyInstance);
        LoggerInstance.Msg("Drova voice provider initialized after gameplay world became ready.");
    }
}
public sealed class WaveProvider : IAudioProvider
{
    readonly string _folder;readonly Manifest _manifest;readonly int _thread;
    readonly RouteLookup _lookup;
    readonly Dictionary<string,AudioClip> _cache=new();readonly HashSet<string> _seen=new();
    public WaveProvider(string folder,Manifest manifest){_folder=System.IO.Path.GetFullPath(folder)+System.IO.Path.DirectorySeparatorChar;_manifest=manifest;_thread=Environment.CurrentManagedThreadId;_lookup=new RouteLookup(manifest.Routes);}
    public Task<AudioClip> GetAudioClip(string dialogeName,string filePath,string locaKey,string actorName,int? choiceId)
    {
        string key=dialogeName+"|"+filePath+"|"+locaKey+"|"+actorName+"|"+choiceId;
        bool firstRequest=_seen.Add(key);
        if(firstRequest)
        {
            File.AppendAllText(System.IO.Path.Combine(_folder,"runtime-requests.jsonl"),JsonSerializer.Serialize(new {tree=dialogeName,path=filePath,key=locaKey,actor=actorName,choice=choiceId,observed_utc=DateTime.UtcNow})+"\n");
        }
        if(_manifest.Mode!="play_reviewed" || choiceId.HasValue || !ConfigAccessor.TryGetConfigValue("EnableDialogueAudio",out bool enabled) || !enabled)return Task.FromResult<AudioClip>(null!);
        var route=_lookup.Find(dialogeName,filePath,locaKey,actorName);
        if(route==null)
        {
            if(firstRequest)File.AppendAllText(System.IO.Path.Combine(_folder,"runtime-lookup-results.jsonl"),JsonSerializer.Serialize(new {tree=dialogeName,path=filePath,key=locaKey,actor=actorName,result="no_matching_route",observed_utc=DateTime.UtcNow})+"\n");
            return Task.FromResult<AudioClip>(null!);
        }
        // Alias/case variants and repeated graph requests share the same waveform.
        string cacheKey=route.Audio+"|"+route.AudioSha256+"|"+route.PlaybackGainDb.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if(_cache.TryGetValue(cacheKey,out var existing) && existing)return Task.FromResult(existing);
        if(Environment.CurrentManagedThreadId!=_thread)throw new InvalidOperationException("AudioClip creation must run on the initialization thread");
        string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(_folder,route.Audio));
        if(!path.StartsWith(_folder,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Audio path outside mod folder");
        byte[] data=File.ReadAllBytes(path);
        if(Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant()!=route.AudioSha256)throw new InvalidDataException("Reviewed audio changed");
        float[] samples=WaveReader.Read(data);WaveReader.ApplyGain(samples,route.PlaybackGainDb);
        var clip=AudioClip.Create("DrovaDE_"+locaKey,samples.Length,1,24000,false);
        if(!clip.SetData(new Il2CppStructArray<float>(samples),0))throw new InvalidDataException("Could not set audio samples");
        PlaybackGuard.RegisterClip(clip);
        _cache[cacheKey]=clip;return Task.FromResult(clip);
    }
}
