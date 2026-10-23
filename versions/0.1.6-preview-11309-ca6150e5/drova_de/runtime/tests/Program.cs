using DrovaVoices;
var startup = new RuntimeStartGate();
int connects = 0;
for (int i=0;i<100;i++)
    if (startup.TryStart(false, () => throw new Exception("Audio static constructor touched before gameplay")))
        throw new Exception("Started before gameplay");
if (startup.Attempted || startup.Started) throw new Exception("Early probe poisoned startup state");
if (!startup.TryStart(true, () => connects++)) throw new Exception("Failed to start after gameplay");
for (int i=0;i<100;i++) startup.TryStart(true, () => connects++);
if (connects != 1 || !startup.Started) throw new Exception("Duplicate audio initialization");
var failed = new RuntimeStartGate();
try {failed.TryStart(true, () => throw new InvalidOperationException("Simulated connection failure"));}
catch (InvalidOperationException) {}
if (failed.Started || failed.TryStart(true, () => throw new Exception("Repeated failed initializer")))
    throw new Exception("Failed initializer retried each frame");
Console.WriteLine("Runtime lifecycle checks passed: early callbacks, gameplay readiness, single connection, failure latch.");
int passed=0;
foreach(string path in args)
{
    byte[] data=File.ReadAllBytes(path);var samples=WaveReader.Read(data);
    if(samples.Length==0 || samples.Any(x=>!float.IsFinite(x)||x<-1||x>=1))throw new Exception("Invalid decoded samples");passed++;
    try {WaveReader.Read(data.Take(35).ToArray());throw new Exception("Accepted truncated file");}catch(InvalidDataException){passed++;}
    var invalid=(byte[])data.Clone();invalid[0]=0;
    try {WaveReader.Read(invalid);throw new Exception("Accepted invalid header");}catch(InvalidDataException){passed++;}
}
if(passed==0)throw new Exception("No fixture tested");
Console.WriteLine("WAV parser checks passed: "+passed);
