using DrovaVoices;
using Drova_Modding_API.Access;
using Il2CppNodeCanvas.DialogueTrees;
using Il2CppNodeCanvas.DialogueTrees.UI;
using UnityEngine;

int count=0;
void Check(bool condition,string message){count++;if(!condition)throw new Exception(message);}
var harmony=new HarmonyLib.Harmony();PlaybackGuard.Install(harmony);Check(harmony.Count==11,"All lifetime patches installed");
AudioClip Own(){var c=new AudioClip();PlaybackGuard.RegisterClip(c);return c;}
(DS_DialogueUGUI ui,SubtitlesRequestInfo info,DS_DialogueUGUI._HandleAudio_d__50 handle,AudioSource source) Line(AudioClip? clip=null,AudioSource? source=null,DS_DialogueUGUI? ui=null)
{
    ui??=new();source??=new();clip??=Own();var info=new SubtitlesRequestInfo();info.statement.audio=clip;
    Check(PlaybackGuard.Begin(ui,info),"New request accepted");
    var h=new DS_DialogueUGUI._HandleAudio_d__50{__4__this=ui,audio=clip,actor=new(){transform=new(){Source=source}}};
    // Native IL2CPP inlines the iterator factory: no HandleAudio() managed call.
    PlaybackGuard.AfterSubtitle(new(){__4__this=ui,info=info,__2__current=h},true);return(ui,info,h,source);
}
DS_DialogueUGUI._PlayAudio_d__51 Play(DS_DialogueUGUI._HandleAudio_d__50 h,AudioSource source)
{
    bool result=true;Check(PlaybackGuard.BeforeHandle(h,ref result),"Current native playback allowed");
    // Exact native sequence: assign clip; Play(); yield the PlayAudio waiter.
    source.clip=h.audio;source.Play();var w=new DS_DialogueUGUI._PlayAudio_d__51{__4__this=h.__4__this,audio=h.audio,playSource=source};
    h.__2__current=w;h.__1__state=1;PlaybackGuard.AfterHandle(h,true);return w;
}
bool Allowed(DS_DialogueUGUI._HandleAudio_d__50 h){bool result=true;return PlaybackGuard.BeforeHandle(h,ref result)&&result;}
bool WaitAllowed(DS_DialogueUGUI._PlayAudio_d__51 w){bool result=true;return PlaybackGuard.BeforeWait(w,ref result)&&result;}
bool StartAllowed(DS_DialogueUGUI ui,SubtitlesRequestInfo info){bool result=true;return PlaybackGuard.BeforeStart(new(){__4__this=ui,info=info},ref result)&&result;}
bool TextAllowed(DS_DialogueUGUI ui,SubtitlesRequestInfo info){bool result=true;return PlaybackGuard.BeforeSubtitle(new(){__4__this=ui,info=info},ref result)&&result;}

// Reproduce the previously unguarded engine behaviour first.
var vulnerable=new AudioSource{clip=Own()};vulnerable.Play();vulnerable.isPlaying=false;vulnerable.Reactivate();Check(vulnerable.Starts==2,"Positive control reproduces stale clip replay");
for(int i=0;i<100;i++)
{
    var l=Line();var wait=Play(l.handle,l.source);
    Check(!l.source.playOnAwake&&!l.source.loop,"Borrowed speech source is one-shot without activation playback");
    Check(WaitAllowed(wait),"Current timer remains active");
    l.source.isPlaying=false;PlaybackGuard.Tick();
    Check(l.source.clip==l.info.statement.audio,"Completed clip retained for auto-dialog end detection");
    l.source.Reactivate();Check(l.source.Starts==1,"Completed clip cannot replay on reactivation");
    Check(!PlaybackGuard.Begin(l.ui,l.info),"Same active request is idempotent");
    Check(l.source.Stops==0,"Duplicate event does not truncate active line");
    PlaybackGuard.End(l.ui);Check(l.source.clip==null&&!l.source.isPlaying,"Finish clears own stale clip");
    Check(l.source.playOnAwake,"Borrowed source flags restored after clip removed");
    l.source.Reactivate();Check(l.source.Starts==1,"No speech after finish while walking");
    Check(!Allowed(l.handle)&&!WaitAllowed(wait),"Late handle and wait rejected after finish");
    Check(!StartAllowed(l.ui,l.info)&&!TextAllowed(l.ui,l.info),"Late root routines rejected after finish");
    var next=Line(l.info.statement.audio,l.source,l.ui);Play(next.handle,next.source);
    Check(next.source.Starts==2,"Intentional same dialogue may be spoken again");
    Check(!Allowed(l.handle)&&!WaitAllowed(wait)&&!StartAllowed(l.ui,l.info),"Old generation rejected even with same clip, UI and source");
    PlaybackGuard.ResetAll();
}
var active=Line();var oldWait=Play(active.handle,active.source);var current=Line(source:active.source,ui:active.ui);Play(current.handle,current.source);
Check(!WaitAllowed(oldWait)&&!Allowed(active.handle),"Old timer cannot stop a new line");
Check(current.source.clip==current.info.statement.audio&&current.source.isPlaying,"New line survives obsolete callback");
PlaybackGuard.End(current.ui);
var delayed=Line();PlaybackGuard.End(delayed.ui);Check(!Allowed(delayed.handle),"Audio awaiting first tick cannot start after close");
var foreign=new AudioClip();var takeover=Line();Play(takeover.handle,takeover.source);takeover.source.clip=foreign;takeover.source.Play();int stops=takeover.source.Stops;
PlaybackGuard.End(takeover.ui);Check(takeover.source.isPlaying&&takeover.source.clip==foreign&&takeover.source.Stops==stops,"Foreign sound takeover untouched");
var plain=Line(foreign);bool r=true;Check(PlaybackGuard.BeforeHandle(plain.handle,ref r),"Non-mod audio remains native");PlaybackGuard.End(plain.ui);
var muted=Line();Play(muted.handle,muted.source);ConfigAccessor.Enabled=false;PlaybackGuard.Tick();Check(!muted.source.isPlaying&&muted.source.clip==null,"Disable immediately stops owned speech");
Check(StartAllowed(muted.ui,muted.info)&&TextAllowed(muted.ui,muted.info),"Disabling audio does not cancel text or its completion callback");
ConfigAccessor.Enabled=true;Check(!Allowed(muted.handle),"Re-enabling cannot revive old audio");PlaybackGuard.End(muted.ui);
ConfigAccessor.Enabled=false;var initiallyMuted=Line();Check(!Allowed(initiallyMuted.handle)&&TextAllowed(initiallyMuted.ui,initiallyMuted.info),"Audio-off new dialog remains readable");ConfigAccessor.Enabled=true;PlaybackGuard.End(initiallyMuted.ui);
var fallback=Line();fallback.handle.actor=null;fallback.ui.localSource=fallback.source;Play(fallback.handle,fallback.source);Check(!fallback.source.playOnAwake,"UI fallback protected too");fallback.ui.isActiveAndEnabled=false;PlaybackGuard.Tick();Check(fallback.source.clip==null&&!Allowed(fallback.handle),"Disabled UI expires speech");
var shared=new AudioSource{loop=true};var a=Line(source:shared);var aw=Play(a.handle,shared);var b=Line(source:shared);Play(b.handle,shared);PlaybackGuard.End(a.ui);Check(shared.isPlaying&&shared.clip==b.info.statement.audio&&!WaitAllowed(aw),"Two UI owners cannot stop each other's new speech");PlaybackGuard.End(b.ui);Check(shared.loop&&shared.playOnAwake,"Original shared flags restored");
var x=Line();Play(x.handle,x.source);var y=Line();Play(y.handle,y.source);PlaybackGuard.ResetAll();Check(x.source.clip==null&&y.source.clip==null,"Scene/save teardown clears every owned source");
var destroyed=Line();Play(destroyed.handle,destroyed.source);destroyed.source.Alive=false;destroyed.ui.Alive=false;PlaybackGuard.Tick();Check(!Allowed(destroyed.handle),"Destroyed actor/UI cannot revive speech");
var reusedSource=new AudioSource{Pointer=destroyed.source.Pointer};var reused=Line(source:reusedSource);Play(reused.handle,reused.source);Check(reused.source.isPlaying,"Destroyed source ownership removed before native pointer reuse");PlaybackGuard.End(reused.ui);
var deadClip=Own();deadClip.Alive=false;var unrelatedClip=new AudioClip{Pointer=deadClip.Pointer};var nativeHandle=new DS_DialogueUGUI._HandleAudio_d__50{audio=unrelatedClip};bool native=true;
Check(PlaybackGuard.BeforeHandle(nativeHandle,ref native),"Destroyed clip pointer reuse cannot misclassify a foreign clip");
Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{assertions=count,repeated_lifetimes=100,native_game_started=false,scope="Actual guard code, simulated native coroutine/source events; final ingame behaviour requires user test"}));
