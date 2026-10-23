using DrovaVoices;
using Drova_Modding_API.Systems.Audio;
using Drova_Modding_API.Systems.WorldEvents;
using MelonLoader.Utils;

string root=Path.Combine(Path.GetTempPath(),"DrovaStartup_"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(root,"Mods","DrovaVoices"));
File.WriteAllText(Path.Combine(root,"Mods","DrovaVoices","routes.json"),"{\"Mode\":\"observe\",\"Routes\":[],\"GameSourceHashes\":{}}");
MelonEnvironment.GameRootDirectory=root;
var core=new Core();
core.OnInitializeMelon();
for(int i=0;i<100;i++){core.OnUpdate();core.OnSceneWasLoaded(0,"MainMenu");}
if(Probe.StaticInitializations!=0 || Probe.Connections!=0)throw new Exception("Early startup touched the poisoned audio initializer");
WorldEventSystemManager.Instance=new();
core.OnSceneWasLoaded(1,"Gameplay_Main");
for(int i=0;i<100;i++)core.OnUpdate();
if(Probe.StaticInitializations!=1 || Probe.Connections!=1 || RuntimeObserver.Installs!=1)throw new Exception("Gameplay did not connect the audio provider exactly once");
if(!core.LoggerInstance.Messages.Any(m=>m.Contains("initialized after gameplay")))throw new Exception("Connection success not logged");
Console.WriteLine("Actual Core startup regression passed: manifest/main menu safe; world-ready audio connects once.");
float[] unchanged={0.1f,-0.3f};
WaveReader.ApplyGain(unchanged,0);
if(unchanged[0]!=0.1f || unchanged[1]!=-0.3f)throw new Exception("Default gain changed existing audio");
float[] adjusted={0.1f,-0.3f};
WaveReader.ApplyGain(adjusted,1.5f);
float factor=MathF.Pow(10,1.5f/20);
if(Math.Abs(adjusted[0]-0.1f*factor)>0.000001 || Math.Abs(adjusted[1]+0.3f*factor)>0.000001)throw new Exception("Gain altered waveform beyond constant scaling");
foreach(float unsafeGain in new[]{float.NaN,7f})
{
    bool rejected=false;
    try{WaveReader.ApplyGain(new[]{0.1f},unsafeGain);}catch(InvalidDataException){rejected=true;}
    if(!rejected)throw new Exception("Unsafe gain accepted");
}
float[] clipping={0.1f,0.99f};
bool clipped=false;
try{WaveReader.ApplyGain(clipping,1.5f);}catch(InvalidDataException){clipped=true;}
if(!clipped || clipping[0]!=0.1f)throw new Exception("Clipping guard failed or partially changed audio");
foreach(string path in args)
{
    float[] actual=WaveReader.Read(File.ReadAllBytes(path));
    WaveReader.ApplyGain(actual,1.5f);
    if(actual.Max(Math.Abs)>=1)throw new Exception("Actual corrected speech clips");
}
Console.WriteLine("Playback gain passed: default unchanged, linear scaling, invalid values/clipping rejected; actual speech files="+args.Length);
// Only our new, dedicated temporary fixture folder is removed.
root=Path.GetFullPath(root);
if(Path.GetDirectoryName(root)!=Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)
   || !Path.GetFileName(root).StartsWith("DrovaStartup_",StringComparison.Ordinal))
    throw new Exception("Unsafe temporary fixture cleanup path");
Directory.Delete(root,true);
