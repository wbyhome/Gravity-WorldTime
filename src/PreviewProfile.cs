using System;
using System.IO;
namespace WorldTime;
public static class PreviewProfile {
 public const string Marker=".worldtime-ruler-preview";
 public static string Resolve(string baseDirectory,string? requested){
  string root=Path.GetFullPath(baseDirectory),target=Path.GetFullPath(requested??Path.Combine(root,"ruler-preview-data"));
  if(string.Equals(target.TrimEnd(Path.DirectorySeparatorChar),Path.Combine(root,"data").TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("The classic data directory cannot be used for a ruler preview.");
  if(File.Exists(Path.Combine(target,"worldtime.json"))&&!File.Exists(Path.Combine(target,Marker)))throw new InvalidOperationException("Existing data needs a separately copied and marked preview profile. Original configuration was not modified.");
  Directory.CreateDirectory(target);if(!File.Exists(Path.Combine(target,Marker)))File.WriteAllText(Path.Combine(target,Marker),"WorldTime isolated ruler preview profile\n");return target;
 }
}
