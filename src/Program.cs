using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;


namespace WorldTime;
public class Row {
 public string Id {get;set;} = Guid.NewGuid().ToString("N");
 public string City {get;set;} = "伦敦";
 public string Name {get;set;} = "";
 public string Zone {get;set;} = "GMT Standard Time";
 public string Code {get;set;} = "GB";
 public string Group {get;set;} = Guid.NewGuid().ToString("N");
 public int Order {get;set;}
 public double X {get;set;} = 120;
 public double Y {get;set;} = 160;
 public double Width {get;set;} = 620;
 public CityRoutine? Routine {get;set;}
 public string Monitor {get;set;}="";
 public double PositionScale {get;set;}=1;
}
public class State {
 public int Schema {get;set;} = 1;
 public string Layer {get;set;} = "normal";
 public List<Row> Rows {get;set;} = new();
 public List<DockedGroup> Docked {get;set;} = new();
 public List<ScreenArea> Displays {get;set;}=new();
 public string Language {get;set;} = "auto";
 public bool Twelve {get;set;}
 public bool Life {get;set;} = true;
 public bool Advice {get;set;} = true;
 public bool Snap {get;set;} = true;
 public bool Weekend {get;set;} = true;
 public int Start {get;set;} = 9;
 public int End {get;set;} = 18;
 public int Lunch {get;set;} = 12;
 public double Opacity {get;set;} = .35;
 public int AppearanceVersion {get;set;}
 public double Font {get;set;} = 13;
}
public static class Layout {
 public const double Step=54;
 public static List<Row> Group(State s, Row r) => s.Rows.Where(x=>x.Group==r.Group).OrderBy(x=>x.Order).ToList();
 public static List<Row> DragSelection(State s,Row r,bool header){
  var group=Group(s,r);
  if(group.Count==0)return new();
  if(group[0].Id==r.Id)return group;
  return header?group.SkipWhile(x=>x.Id!=r.Id).ToList():new();
 }
 public static void SplitForDrag(State s,List<Row> moving){
  if(moving.Count==0)return;
  var group=Group(s,moving[0]);
  if(group.Count==moving.Count)return;
  string id=Guid.NewGuid().ToString("N");
  for(int i=0;i<moving.Count;i++){moving[i].Group=id;moving[i].Order=i;}
  int order=0;foreach(var row in group.Except(moving))row.Order=order++;
 }
 public static void Arrange(List<Row> rows, double x, double y, double width) {
  for(int i=0;i<rows.Count;i++){rows[i].Order=i; rows[i].X=x; rows[i].Y=y+i*Step;rows[i].Width=width;}
 }
 public static void Detach(State s,Row r) {
  var old=Group(s,r); old.Remove(r);
  if(old.Count>0) Arrange(old,old[0].X,old.Min(x=>x.Y),old[0].Width);
  r.Group=Guid.NewGuid().ToString("N");r.Order=0;
 }
 public record Candidate(Row Target,bool Before,double X,double Y,double Score);
 public static Candidate? Find(State s,List<Row> moving) {
  if(!s.Snap || moving.Count==0)return null;
  var first=moving[0]; Candidate? best=null;
  foreach(var g in s.Rows.Except(moving).GroupBy(r=>r.Group)) {
   var rows=g.OrderBy(r=>r.Order).ToList();var a=rows[0];var z=rows[^1];
   if(Math.Abs(first.PositionScale-a.PositionScale)>.001||first.Monitor.Length>0&&a.Monitor.Length>0&&first.Monitor!=a.Monitor)continue;
   foreach(bool before in new[]{true,false}) {
    double y=before?a.Y-moving.Count*Step:z.Y+Step;
    double dx=Math.Abs(first.X-a.X),dy=Math.Abs(first.Y-y);
    if(dx<=28 && dy<=22 && (best==null || dx+dy<best.Score))best=new(a,before,a.X,y,dx+dy);
   }
  }
  return best;
 }
 public static void Merge(State s,List<Row> moving,Candidate c) {
  var target=Group(s,c.Target);var all=c.Before?moving.Concat(target).ToList():target.Concat(moving).ToList();
  foreach(var r in all){r.Group=c.Target.Group;r.Monitor=c.Target.Monitor;r.PositionScale=c.Target.PositionScale;}
  Arrange(all,c.X,c.Before?c.Y:target[0].Y,c.Target.Width);
 }
}
public sealed class Store : IDisposable {
 public readonly string DirectoryPath;
 readonly FileStream lease;
 public string FilePath => Path.Combine(DirectoryPath,"worldtime.json");
 public Store(string dir){DirectoryPath=Path.GetFullPath(dir);Directory.CreateDirectory(DirectoryPath);lease=new FileStream(Path.Combine(DirectoryPath,"instance.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}
 public State Load(){
  if(!File.Exists(FilePath))return new State();
  var s=JsonSerializer.Deserialize<State>(File.ReadAllText(FilePath)) ?? throw new InvalidDataException("Empty state");
  Validate(s);return s;
 }
 public static void Validate(State s){
  if(s.Rows==null||s.Displays==null||s.Displays.Any(d=>d.Right<=d.Left||d.Bottom<=d.Top||!double.IsFinite(d.Scale)||d.Scale<.5||d.Scale>8))throw new InvalidDataException("Invalid display layout");
  if(s.Docked==null||s.Docked.Any(d=>string.IsNullOrEmpty(d.Group)||!DockGeometry.Edges.Contains(d.Edge)||!double.IsFinite(d.Offset)||d.Offset<0||d.Offset>1)||s.Docked.Select(d=>d.Group).Distinct().Count()!=s.Docked.Count)throw new InvalidDataException("Invalid folded layout");
  if(Array.IndexOf(L10n.Languages,s.Language)<0 || (s.Layer!="normal" && s.Layer!="top" && s.Layer!="bottom") || s.Schema!=1 || s.Start<0 || s.End>24 || s.Start>=s.End || (s.End-s.Start==1 && s.Lunch==s.Start) || s.Lunch<0 || s.Lunch>23 || !double.IsFinite(s.Opacity) || s.Opacity<.15 || s.Opacity>1 || !double.IsFinite(s.Font) || s.Font<11 || s.Font>18)throw new InvalidDataException("Unsupported or invalid settings");
  if(s.Rows.Select(r=>r.Id).Distinct().Count()!=s.Rows.Count)throw new InvalidDataException("Duplicate IDs");
  foreach(var r in s.Rows){if(!double.IsFinite(r.X)||!double.IsFinite(r.Y)||!double.IsFinite(r.Width)||r.Width<480||r.Width>900||string.IsNullOrEmpty(r.Group)||r.Name==null||r.Monitor==null||r.Name.Length>80||!double.IsFinite(r.PositionScale)||r.PositionScale<.5||r.PositionScale>8)throw new InvalidDataException("Invalid layout");r.Routine?.Validate();Catalog.Local(r.Zone,DateTimeOffset.UtcNow);}
 }
 public void Save(State s){Validate(s);string tmp=FilePath+".tmp";using(var f=new FileStream(tmp,FileMode.Create,FileAccess.Write,FileShare.None)){JsonSerializer.Serialize(f,s,new JsonSerializerOptions{WriteIndented=true});f.Flush(true);}if(File.Exists(FilePath))File.Replace(tmp,FilePath,FilePath+".bak");else File.Move(tmp,FilePath);}
 public void Dispose()=>lease.Dispose();
}
public static class Program {
 [STAThread] public static void Main(string[] args){if(args.Contains("--layered-child-test")){LayeredChildProbe.Run(args);return;}if(args.Contains("--acceptance-probe")){AcceptanceProbe.Run(args);return;}if(args.Contains("--ui-test")){UiTests.Run(args);return;}if(args.Contains("--layer-test")){LayerTests.Run(args);return;}if(args.Contains("--self-test")){Tests.Run(args);return;}Store? store=null;try{string dir=args.Contains("--data-dir")?args[Array.IndexOf(args,"--data-dir")+1]:Path.Combine(AppContext.BaseDirectory,"data");store=new Store(dir);var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};var host=new Host(store);if(args.Contains("--diagnostic-report"))ClockDiagnostics.Start(host,args[Array.IndexOf(args,"--diagnostic-report")+1]);if(args.Contains("--recover"))app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(host.RecoverClocks));app.Run();}catch(Exception e){MessageBox.Show(L10n.Text("auto","WorldTime could not start. Existing data was preserved.")+"\n"+e.Message,"WorldTime");store?.Dispose();}}
}
