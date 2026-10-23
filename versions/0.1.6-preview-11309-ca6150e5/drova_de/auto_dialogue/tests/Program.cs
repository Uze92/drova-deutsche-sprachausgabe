using DrovaAutoDialogue;

int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); passed++; }
AudioEndGate Played()
{
    var gate = new AudioEndGate();
    for (int i = 0; i < 100; i++)
        Check(!gate.Tick(true, i / 100.0, 1, 0.01, false, 0.25), "No skip while playing");
    return gate;
}
var normal = Played();
for (int i = 0; i < 24; i++) Check(!normal.Tick(false, 0, 1, 0.01, false, 0.25), "Delay respected");
Check(normal.Tick(false, 0, 1, 0.02, false, 0.25), "End advances once");
Check(!normal.Tick(false, 0, 1, 10, false, 0.25), "No repeated advance");
var missing = new AudioEndGate();
for (int i = 0; i < 16; i++) Check(!missing.Tick(false, 0, 1, 1, false, 0), "No skip without playback");
Check(missing.Cancelled, "Playback timeout");
var stopped = new AudioEndGate();
stopped.Tick(true, 2, 10, 0.01, false, 0);
Check(!stopped.Tick(false, 0, 10, 0.01, false, 0) && stopped.Cancelled, "Interrupted playback stays manual");
var paused = Played();
Check(!paused.Tick(false, 0, 1, 100, true, 0), "Pause cannot finish");
Check(!paused.Finished, "Suspension retains state");
Check(paused.Tick(false, 0, 1, 0.02, false, 0), "Resume can finish");
var afterEndPause = Played();
afterEndPause.Tick(false, 0, 1, 0.1, false, 0.25);
Check(!afterEndPause.Tick(false, 0, 1, 100, true, 0.25), "Menu does not consume delay");
Check(afterEndPause.Tick(false, 0, 1, 0.2, false, 0.25), "Delay resumes");
var restarting = Played();
restarting.Tick(false, 0, 1, 0.01, false, 0.25);
Check(!restarting.Tick(true, 0, 1, 0.01, false, 0.25) && restarting.Cancelled, "Restart is not an end");
var invalid = new AudioEndGate();
Check(!invalid.Tick(true, 0, double.NaN, 0.01, false, 0) && invalid.Cancelled, "Invalid audio fails closed");
Console.WriteLine($"Audio end checks passed: {passed}. No game process started.");

// Exercise the actual mod bridge, not a second implementation of its conditions.
var core = new Core();
core.OnInitializeMelon();
var options = Drova_Modding_API.Access.OptionMenuAccess.Instance;
options.Open();
Check(options.Last.Labels.SequenceEqual(new[] { "Title", "Enabled", "Delay", "Help" }), "Menu controls built");
var values = Drova_Modding_API.Access.ConfigAccessor.Values;
Check((int)values[Core.DelayKey] == 500, "Fresh menu uses 500 ms default");
values["EnableDialogueAudio"] = true;
(Il2CppNodeCanvas.DialogueTrees.UI.DS_DialogueUGUI ui, Il2CppNodeCanvas.DialogueTrees.SubtitlesRequestInfo info) Line()
{
    var clip = new UnityEngine.AudioClip();
    var ui = new Il2CppNodeCanvas.DialogueTrees.UI.DS_DialogueUGUI();
    ui.DLGTree!.current = new Il2CppNodeCanvas.DialogueTrees.DS_StatementNode();
    ui.localSource!.clip = clip;
    var info = new Il2CppNodeCanvas.DialogueTrees.SubtitlesRequestInfo();
    info.statement.audio = clip;
    Core.Subtitle(ui, info);
    return (ui, info);
}
bool Continue(Il2CppNodeCanvas.DialogueTrees.UI.DS_DialogueUGUI ui)
{
    bool result = ui.InputHandler!.CheckShouldSkipDialog();
    Core.AfterContinueCheck(ui.InputHandler, ref result);
    return result;
}
void Play(Il2CppNodeCanvas.DialogueTrees.UI.DS_DialogueUGUI ui)
{
    ui.localSource!.isPlaying = true;
    for (int i = 0; i < 50; i++) { ui.localSource.time = i * 0.02f; core.OnUpdate(); }
    Check(!Continue(ui), "Native wait stays blocked while playing");
    ui.localSource.isPlaying = false;
    ui.localSource.time = 0;
}
void Finish() { for (int i = 0; i < 35; i++) core.OnUpdate(); }
var line = Line(); Play(line.ui); Finish();
Check(Continue(line.ui), "Native wait receives continue after audio end");
line.ui.DLGTree!.current = new Il2CppNodeCanvas.DialogueTrees.Node(); Finish();
Check(!Continue(line.ui), "No auto input leaks to the next node");
line = Line(); Play(line.ui); Core.Cancel(line.ui); Finish();
Check(!Continue(line.ui), "Choice/end cancellation prevents stale input");
line = Line(); Play(line.ui); line.ui.DLGTree!.current = new Il2CppNodeCanvas.DialogueTrees.Node(); Finish();
Check(!Continue(line.ui), "Node transition prevents stale input");
line = Line(); Play(line.ui); options.IsMenuOpen = true; Finish();
Check(!Continue(line.ui), "Open options block auto advance");
options.IsMenuOpen = false; Finish();
Check(Continue(line.ui), "Closing options resumes delay");
line = Line(); Play(line.ui); values[Core.EnabledKey] = false; Finish();
Check(!Continue(line.ui), "Toggle off works immediately");
values[Core.EnabledKey] = true;
line = Line(); Play(line.ui); core.OnSceneWasLoaded(1, "OtherScene"); Finish();
Check(!Continue(line.ui), "Scene change prevents stale input");
line = Line(); line.info.statement.audio = null; Core.Subtitle(line.ui, line.info); Play(line.ui); Finish();
Check(!Continue(line.ui), "Unvoiced text stays manual");
line = Line(); line.ui.DLGTree!.current = new Il2CppNodeCanvas.DialogueTrees.Node(); Core.Subtitle(line.ui, line.info); Play(line.ui); Finish();
Check(!Continue(line.ui), "Choice node never auto advances");
values["EnableDialogueAudio"] = false;
line = Line(); Play(line.ui); Finish();
Check(!Continue(line.ui), "Disabled audio remains manual");
values["EnableDialogueAudio"] = true;
line = Line(); Play(line.ui); line.ui.localSource!.clip = new UnityEngine.AudioClip(); Finish();
Check(!Continue(line.ui), "Unrelated clip cannot advance");
line = Line(); Play(line.ui); Finish();
Check(!line.ui.InputHandler!._inputDown, "Regression: no reliance on obsolete text animation flag");
Check(Continue(line.ui), "Actual native polling hook returns true");
bool unrelated = false;
Core.AfterContinueCheck(new Il2CppDrova.GUI.Dialogue.GUI_DialogueInputHandler(), ref unrelated);
Check(!unrelated, "Other input handler unaffected");
options.IsMenuOpen = true;
Check(!Continue(line.ui), "Even an already-ready line waits while options open");
options.IsMenuOpen = false;
values[Core.EnabledKey] = false;
Check(!Continue(line.ui), "Ready flag respects toggle before next update");
values[Core.EnabledKey] = true;
line.ui.DLGTree!.current = new Il2CppNodeCanvas.DialogueTrees.Node();
Check(!Continue(line.ui), "Changed node rejected directly at native polling");
line = Line(); Play(line.ui);
bool manual = true;
Core.AfterContinueCheck(line.ui.InputHandler!, ref manual);
Check(manual, "Physical manual input preserved");
Finish();
Check(!Continue(line.ui), "Manual input clears delayed action");
values.Remove(Core.DelayKey);
line = Line(); Play(line.ui);
for (int i=0; i<24; i++) core.OnUpdate();
Check(!Continue(line.ui), "Missing config still waits at least 500 ms");
for (int i=0; i<4; i++) core.OnUpdate();
Check(Continue(line.ui), "Missing config uses 500 ms fallback");
values[Core.DelayKey] = 900;
options.Open();
Check((int)values[Core.DelayKey] == 900, "Existing personal delay survives menu rebuild");
values[Core.DelayKey]=500;
line=Line();Play(line.ui);Finish();line.ui.localSource!.isPlaying=true;
Check(!Continue(line.ui),"Restarted playback between update and native poll cannot be skipped");
line=Line();Play(line.ui);Finish();line.ui.localSource!.clip=new UnityEngine.AudioClip();
Check(!Continue(line.ui),"Replaced clip between update and native poll cancels ready action");
line=Line();Play(line.ui);Finish();line.ui.isActiveAndEnabled=false;
Check(!Continue(line.ui),"Inactive UI cannot consume ready automatic continuation");
line=Line();Play(line.ui);Finish();values["EnableDialogueAudio"]=false;
Check(!Continue(line.ui),"Ready auto action cancelled when dialogue audio is disabled before next frame");
values["EnableDialogueAudio"]=true;Finish();Check(!Continue(line.ui),"Enabling audio cannot revive a cancelled auto action");
line=Line();var actorSource=new UnityEngine.AudioSource{clip=line.info.statement.audio};
line.info.actor=new(){transform=new(){Source=actorSource}};Core.Subtitle(line.ui,line.info);
line.ui.localSource!.clip=null;actorSource.isPlaying=true;
for(int i=0;i<50;i++){actorSource.time=i*0.02f;core.OnUpdate();}
Check(!Continue(line.ui),"Actor-source audio cannot advance while speaking");
actorSource.isPlaying=false;actorSource.time=0;Finish();
Check(Continue(line.ui),"Actual actor source advances despite unused UI fallback");
line=Line();Play(line.ui);values["EnableDialogueAudio"]=false;Finish();
Check(!Continue(line.ui),"Disabling speech while playing cancels automatic continuation");values["EnableDialogueAudio"]=true;
core.OnDeinitializeMelon();
Console.WriteLine($"Actual mod bridge and menu fixtures passed. Total assertions: {passed}. Native game rendering and input consumption require user ingame test.");
