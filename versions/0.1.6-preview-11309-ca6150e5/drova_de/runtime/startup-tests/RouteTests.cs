using System.Text.Json;
using System.Security.Cryptography;
using DrovaVoices;
using Drova_Modding_API.Access;

var manifest=JsonSerializer.Deserialize<Manifest>(File.ReadAllText(args[0]))!;
var lookup=new RouteLookup(manifest.Routes);
int assertions=0;
foreach(var r in manifest.Routes)
foreach(string actor in r.ActorAliases)
{
    var found=lookup.Find(r.Tree,r.Path,r.Key,actor.ToLowerInvariant());
    if(found!=r)throw new Exception("Packaged route failed: "+r.Audio+" actor "+actor);
    assertions++;
}
var source=manifest.Routes[0];
try {new RouteLookup(new[]{source,new Route{Tree=source.Tree,Path=source.Path,Key=source.Key,ActorAliases=source.ActorAliases,Audio="other.wav",VoiceId="wrong"}});throw new Exception("Conflicting route accepted");}
catch(InvalidDataException){assertions++;}
// Exercise the actual provider path for the reported localization keys.
string fixture=Path.Combine(Path.GetTempPath(),"DrovaRoutes_"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(fixture,"audio"));
try
{
    var selected=manifest.Routes.Where(r=>(r.Path=="Dialog/NPC_Asmus"&&r.Key=="Plh_456") || (r.Path=="Dialog/BlaanKarimBattleBuddies"&&r.Key=="Plh_103") || r.Audio.EndsWith("/drova_cb12683d8eff9823ef0c.wav")).ToArray();
    if(selected.Length!=3)throw new Exception("Reported lines or native actor regression missing from fixed package");
    var busy=selected.Single(r=>r.Audio.EndsWith("/drova_cb12683d8eff9823ef0c.wav"));
    if(!busy.ActorAliases.Contains("Carima")||busy.ActorAliases.Contains("Blaan")||busy.VoiceId!=selected.Single(r=>r.Key=="Plh_103").VoiceId)throw new Exception("Stale Blaan cache was used instead of Carima's fixed identity");
    foreach(var r in selected)File.Copy(Path.Combine(Path.GetDirectoryName(args[0])!,r.Audio),Path.Combine(fixture,r.Audio));
    var provider=new WaveProvider(fixture,new Manifest{Mode="play_reviewed",Routes=selected});
    ConfigAccessor.Enabled=true;
    foreach(var r in selected)
    {
    UnityEngine.AudioClip? shared=null;
    foreach(string actor in r.ActorAliases)
    {
        var loaded=provider.GetAudioClip(r.Tree,r.Path,r.Key,actor,null).GetAwaiter().GetResult();
        if(loaded==null)throw new Exception("Actual provider returned silence for reported line");
        if(shared!=null&&!ReferenceEquals(shared,loaded))throw new Exception("Alias creates redundant audio clips instead of sharing fixed route");
        shared=loaded;assertions++;
        if(provider.GetAudioClip(r.Tree,r.Path,r.Key,actor,0).GetAwaiter().GetResult()!=null)throw new Exception("Player choice voiced");
        assertions+=2;
    }
    shared!.Alive=false;
    if(ReferenceEquals(shared,provider.GetAudioClip(r.Tree,r.Path,r.Key,r.ActorAliases[0],null).GetAwaiter().GetResult()))throw new Exception("Destroyed cached clip was reused");
    assertions++;
    }
    ConfigAccessor.Enabled=false;
    if(provider.GetAudioClip(selected[0].Tree,selected[0].Path,selected[0].Key,selected[0].ActorAliases[0],null).GetAwaiter().GetResult()!=null)throw new Exception("Disabled dialogue played");
    assertions++;
}
finally
{
    if(Path.GetDirectoryName(Path.GetFullPath(fixture))!=Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)||!Path.GetFileName(fixture).StartsWith("DrovaRoutes_"))throw new Exception("Unsafe fixture path");
    Directory.Delete(fixture,true);
}
Console.WriteLine(JsonSerializer.Serialize(new{routes=manifest.Routes.Length,assertions,actual_provider_reported_lines=2,native_actor_parameter_regression_lines=1,game_started=false}));
