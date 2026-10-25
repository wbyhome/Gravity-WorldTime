using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace WorldTime;
// New-profile evidence only. It does not read or restore older version profiles.
public static class RulerViewAudit {
 static readonly Lazy<string> assemblyHash=new(()=>Hash(Assembly.GetExecutingAssembly().Location));
 public static string Hash(string path)=>File.Exists(path)?Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))):"";
 public static string Record(string path,string before,string reason){
  try{string journal=Path.Combine(Path.GetDirectoryName(path)!,"ruler-view-writes.jsonl");if(File.Exists(journal)&&new FileInfo(journal).Length>4*1024*1024)return "View audit reached its size limit; existing journal retained.";
   File.AppendAllText(journal,JsonSerializer.Serialize(new{Utc=DateTimeOffset.UtcNow,ProcessId=Environment.ProcessId,AssemblySHA256=assemblyHash.Value,Path=Path.GetFullPath(path),Reason=reason,BeforeSHA256=before,AfterSHA256=Hash(path)})+Environment.NewLine);return "";
  }catch(Exception e){return e.Message;}
 }
}
