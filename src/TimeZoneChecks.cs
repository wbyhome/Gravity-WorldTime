using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace WorldTime;
public static class TimeZoneChecks {
 public sealed class Fake : ISystemTimeZone {
  public string Zone="China Standard Time";public int Sets;public bool Denied,Ignore,Elevated;
  public Task<string> Read()=>Task.FromResult(Zone);
  public Task Set(string zone,bool administrator){Sets++;Elevated=administrator;if(Denied)throw new IOException("Denied");if(!Ignore)Zone=zone;return Task.CompletedTask;}
 }
 public static void Run(Action<bool,string> check){
  void Fail(Action a,string name){bool failed=false;try{a();}catch{failed=true;}check(failed,name);}
  var read=WindowsTimeZone.Command();check(read.ArgumentList.Count==1&&read.ArgumentList[0]=="/g"&&!read.UseShellExecute,"Read command is read-only /g with no shell");
  var write=WindowsTimeZone.Command("Eastern Standard Time");check(write.ArgumentList.Count==2&&write.ArgumentList[1]=="Eastern Standard Time"&&!write.UseShellExecute,"Zone with spaces is one process argument");
  var admin=WindowsTimeZone.Command("Eastern Standard Time",true);check(admin.Verb=="runas"&&admin.UseShellExecute&&admin.Arguments=="/s \"Eastern Standard Time\"","Elevation uses explicit runas and quoted validated zone");
  Fail(()=>WindowsTimeZone.Command("Eastern Standard Time\" & whoami"),"Reject command injection or unknown zone");
  check(WindowsTimeZone.Validate("Pacific Standard Time_dstoff").EndsWith("_dstoff"),"Restore preserves disabled-DST suffix");
  var s=new State();var f=new Fake();int saves=0;var change=new TimeZoneChange(s,()=>{saves++;return true;},f);
  void Apply(string target,bool restore=false,bool elevated=false)=>change.Apply(f.Zone,target,restore,elevated).GetAwaiter().GetResult();
  Apply("Eastern Standard Time");check(f.Sets==1&&f.Zone=="Eastern Standard Time"&&s.OriginalSystemTimeZone=="China Standard Time"&&saves==2,"Save original before applying then verify and persist");
  Apply("Pacific Standard Time");check(s.OriginalSystemTimeZone=="China Standard Time","Repeated city switches preserve first original zone");
  Apply("China Standard Time",true);check(s.OriginalSystemTimeZone==""&&f.Zone=="China Standard Time","Verified restore clears recovery record");
  Apply("China Standard Time");check(f.Sets==3&&s.OriginalSystemTimeZone=="","Same zone avoids system write and unnecessary backup");
  Fail(()=>change.Apply("UTC","Eastern Standard Time",false).GetAwaiter().GetResult(),"Stale preview refuses to overwrite changed Windows zone");
  check(f.Sets==3,"Stale preview performs no system mutation");
  var deniedState=new State();var denied=new Fake{Denied=true};var blocked=new TimeZoneChange(deniedState,()=>true,denied);
  Fail(()=>blocked.Apply(denied.Zone,"UTC",false).GetAwaiter().GetResult(),"Permission failure is surfaced");
  check(denied.Zone=="China Standard Time"&&deniedState.OriginalSystemTimeZone==denied.Zone,"Failure keeps recovery record and does not claim changed zone");
  denied.Denied=false;blocked.Apply(denied.Zone,"UTC",false,true).GetAwaiter().GetResult();check(denied.Elevated&&denied.Zone=="UTC","Explicit administrator retry uses elevated route");
  var noSave=new Fake();Fail(()=>new TimeZoneChange(new State(),()=>false,noSave).Apply(noSave.Zone,"UTC",false).GetAwaiter().GetResult(),"Failed pre-change save blocks operation");check(noSave.Sets==0,"Never set Windows zone without saved recovery information");
  var ignored=new Fake{Ignore=true};Fail(()=>new TimeZoneChange(new State(),()=>true,ignored).Apply(ignored.Zone,"UTC",false).GetAwaiter().GetResult(),"Readback mismatch is not reported as success");
  var old=new State{OriginalSystemTimeZone="Pacific Standard Time_dstoff"};var restore=new Fake();new TimeZoneChange(old,()=>true,restore).Apply(restore.Zone,old.OriginalSystemTimeZone,true).GetAwaiter().GetResult();check(restore.Zone=="Pacific Standard Time_dstoff","Restore applies original DST-disabled setting exactly");
  var saved=JsonSerializer.Deserialize<State>(JsonSerializer.Serialize(deniedState))!;check(saved.OriginalSystemTimeZone=="China Standard Time","Original zone survives saved-state serialization");
  check(JsonSerializer.Deserialize<State>("{}")!.OriginalSystemTimeZone=="","Legacy saved state has no pending restore");
  var failedRestore=new State{OriginalSystemTimeZone="China Standard Time"};var restored=new Fake{Zone="UTC"};Fail(()=>new TimeZoneChange(failedRestore,()=>false,restored).Apply("UTC","China Standard Time",true).GetAwaiter().GetResult(),"Final recovery-save failure is surfaced");check(failedRestore.OriginalSystemTimeZone=="China Standard Time","Failed restore persistence retains in-memory recovery record");
 }
}
