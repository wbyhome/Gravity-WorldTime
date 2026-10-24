using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WorldTime;

public interface ISystemTimeZone {
 Task<string> Read();
 Task Set(string zone,bool administrator);
}

// Only tzutil changes the zone. Never calls SetSystemTime or changes UTC/NTP.
public sealed class WindowsTimeZone : ISystemTimeZone {
 public static string Validate(string id){
  string plain=id.EndsWith("_dstoff",StringComparison.Ordinal)?id[..^7]:id;
  if(!TimeZoneInfo.GetSystemTimeZones().Any(z=>z.Id==plain))throw new InvalidDataException("Unknown Windows time zone.");
  return id;
 }
 public static ProcessStartInfo Command(string? zone=null,bool administrator=false){
  if(zone!=null)Validate(zone);
  var p=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"tzutil.exe")){
   UseShellExecute=administrator,CreateNoWindow=!administrator,RedirectStandardOutput=!administrator,RedirectStandardError=!administrator
  };
  if(administrator){p.Verb="runas";p.WindowStyle=ProcessWindowStyle.Hidden;p.Arguments="/s \""+zone+"\"";}
  else {p.ArgumentList.Add(zone==null?"/g":"/s");if(zone!=null)p.ArgumentList.Add(zone);}
  return p;
 }
 public async Task<string> Read(){
  using var p=Process.Start(Command())??throw new IOException("Could not read Windows time zone.");
  var output=p.StandardOutput.ReadToEndAsync();var error=p.StandardError.ReadToEndAsync();
  await p.WaitForExitAsync();if(p.ExitCode!=0)throw new IOException(await error);
  return Validate((await output).Trim());
 }
 public async Task Set(string zone,bool administrator){
  using var p=Process.Start(Command(zone,administrator))??throw new IOException("Could not change Windows time zone.");
  Task<string>? output=administrator?null:p.StandardOutput.ReadToEndAsync();
  Task<string>? error=administrator?null:p.StandardError.ReadToEndAsync();
  await p.WaitForExitAsync();if(output!=null)await output;
  if(p.ExitCode!=0)throw new IOException(error==null?"Windows rejected the time zone change.":await error);
 }
}

// Persist rollback information before invoking the system operation.
public sealed class TimeZoneChange {
 readonly State state;readonly Func<bool> save;readonly ISystemTimeZone system;
 public TimeZoneChange(State state,Func<bool> save,ISystemTimeZone system){this.state=state;this.save=save;this.system=system;}
 public async Task Apply(string expectedCurrent,string target,bool restore,bool administrator=false){
  WindowsTimeZone.Validate(target);
  if(restore&&(state.OriginalSystemTimeZone.Length==0||target!=state.OriginalSystemTimeZone))throw new InvalidOperationException("No saved time zone to restore.");
  string current=await system.Read();
  if(current!=expectedCurrent)throw new InvalidOperationException("Windows time zone changed. Reopen this dialog.");
  if(current!=target){
   if(state.OriginalSystemTimeZone.Length==0){state.OriginalSystemTimeZone=current;if(!save()){state.OriginalSystemTimeZone="";throw new IOException("Could not save the original time zone. Nothing changed.");}}
   await system.Set(target,administrator);
   if(await system.Read()!=target)throw new IOException("Windows did not retain the requested time zone.");
  }
  string original=state.OriginalSystemTimeZone;
  if(restore)state.OriginalSystemTimeZone="";
  if(!save()){state.OriginalSystemTimeZone=original;throw new IOException("Time zone applied, but recovery settings could not be saved.");}
 }
}
