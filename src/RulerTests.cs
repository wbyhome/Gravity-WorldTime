using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WorldTime;
public static class RulerTests {
 public static State Example(){
  var s=new State{Language="zh-Hans",AppearanceVersion=2};
  var rows=new[]{("Tom","纽约","Eastern Standard Time","US"),("Anna","伦敦","GMT Standard Time","GB"),("Yuki","东京","Tokyo Standard Time","JP"),("Lin","北京","China Standard Time","CN"),("Nora","洛杉矶","Pacific Standard Time","US"),("Maya","加德满都","Nepal Standard Time","NP")};
  foreach(var (name,city,zone,code) in rows)s.Rows.Add(new Row{Id="example-"+name,Name=name,City=city,Zone=zone,Code=code,Group="example",Order=s.Rows.Count,X=140,Y=150+s.Rows.Count*54});
  return s;
 }
 public static void Run(string[] args){
  string report=Path.GetFullPath(args.Contains("--report")?args[Array.IndexOf(args,"--report")+1]:"ruler-tests.txt"),images=Path.Combine(Path.GetDirectoryName(report)!,Path.GetFileNameWithoutExtension(report)+"-images");Directory.CreateDirectory(images);
  string temp=Path.Combine(Path.GetTempPath(),"WorldTime-ruler-"+Guid.NewGuid().ToString("N"));var lines=new List<string>();int count=0;Host? host=null;
  void Check(bool yes,string message){if(!yes)throw new Exception("FAIL "+message);lines.Add("PASS "+message);count++;}
  void Model(){
   var s=Example();var utc=DateTimeOffset.Parse("2026-10-24T00:15:00Z");var china=TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
   string before=JsonSerializer.Serialize(s);var people=RulerModel.Snapshot(s,utc,china);
   Check(people.Count==7&&people.Count(p=>p.Self)==1,"Self is virtual and all six saved contacts are present");
   foreach(var p in people){Check(Math.Abs(p.Fraction-(p.Local.TimeOfDay.TotalMinutes-p.Local.Second/60d)/1440d)<.000001,"Hour placement is local minute / 1440: "+p.Id);Check(p.Fraction>=0&&p.Fraction<1,"Midnight-bound fraction: "+p.Id);}
   Check(people.First(p=>p.Id=="example-Tom").Local.Day==23&&people.First(p=>p.Self).Local.Day==24,"Same instant displays different absolute dates");
   Check(people.First(p=>p.Id=="example-Maya").Difference.TotalMinutes==-135,"Quarter-hour offset is preserved relative to self");
   Check(RulerModel.Difference(people.First(p=>p.Id=="example-Maya"),"en").Contains("02:15"),"Difference description preserves direction and minutes");
   var view=RulerModel.Arrange(people,850);foreach(var p in view)Check(Math.Abs(p.X-p.Person.Fraction*850)<.0001,"Collision handling never changes true-hour anchor: "+p.Person.Id);
   var crowd=Enumerable.Range(0,32).Select(i=>people[1] with{Id="crowd-"+i}).ToList();var placement=RulerModel.Arrange(crowd,550,156);
   Check(placement.Count==32&&placement.Select(p=>p.Lane).Distinct().Count()==32,"32 contacts at the same time remain reachable in separate scroll lanes");
   foreach(var lane in view.GroupBy(p=>p.Lane)){var sorted=lane.OrderBy(p=>p.CardLeft).ToList();Check(sorted.Zip(sorted.Skip(1),(a,b)=>a.CardLeft+170+10<=b.CardLeft).All(x=>x),"Cards do not overlap in a lane");}
   Check(view.All(p=>p.CardLeft>=0&&p.CardLeft+170<=850),"End-of-day cards stay inside the track");
   var dateline=new State();dateline.Rows.Add(new Row{Zone="Line Islands Standard Time"});var extreme=RulerModel.Snapshot(dateline,DateTimeOffset.Parse("2026-10-23T11:15:00Z"),TimeZoneInfo.CreateCustomTimeZone("viewer-12",TimeSpan.FromHours(-12),"viewer-12","viewer-12"));
   Check(extreme[1].Difference.TotalHours==26&&extreme[1].Local.Date.Subtract(extreme[0].Local.Date).Days==2,"Date-line extreme keeps exact date and 26-hour offset without dropping contact");
   var dst=new State();dst.Rows.Add(new Row{Zone="Eastern Standard Time"});var a=RulerModel.Snapshot(dst,DateTimeOffset.Parse("2026-03-08T06:30:00Z"),TimeZoneInfo.Utc)[1];var b=RulerModel.Snapshot(dst,DateTimeOffset.Parse("2026-03-08T07:30:00Z"),TimeZoneInfo.Utc)[1];
   Check(a.Local.Hour==1&&b.Local.Hour==3&&a.Difference!=b.Difference,"DST moves actual local-hour anchor and refreshes offset");
   s.Rows[0].Routine=new CityRoutine{Wake=17*60,Sleep=8*60,Start=22*60,End=6*60,BreakEnabled=false};
   var night=RulerModel.Snapshot(s,DateTimeOffset.Parse("2026-10-24T08:26:00Z"),china).Single(p=>p.Id==s.Rows[0].Id);
   Check(night.Contactable&&night.Status==L10n.Text(s.Language,"Usually convenient"),"Friday night shift stays contactable on Saturday at 04:26");
   Check(!night.Status.Contains("正在"),"Ruler uses tentative status rather than monitoring claims");
   s.Rows[0].Routine=null;Check(JsonSerializer.Serialize(s)==before,"Model and layouts do not mutate saved records");
  }
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};int exit=0;
  try{
   Model();using(var seed=new Store(temp))seed.Save(Example());host=new Host(new Store(temp),true);
   app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>{
    try{
     string original=JsonSerializer.Serialize(host.S.Rows);host.ShowRuler();var ruler=host.Ruler!;
     Check(ruler.IsVisible&&!ruler.ShowInTaskbar&&host.Bars.Values.All(b=>!b.IsVisible),"Ruler-only preview opens without taskbar entry or native clock windows");
     var instant=DateTimeOffset.Parse("2026-10-24T00:15:00Z");var viewer=TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
     foreach(string language in new[]{"en","zh-Hans","zh-Hant","ja"}){
      host.S.Language=language;ruler.Refresh(instant,viewer,true);ruler.UpdateLayout();Render(ruler,Path.Combine(images,"ruler-"+language+".png"));
      Check(ruler.Placements.Count==7&&ruler.Placements.All(p=>p.X>=0),"Four-language ruler preserves people: "+language);
     }
     host.S.Language="zh-Hans";ruler.Refresh(instant,viewer,true);ruler.PersonButtons[host.S.Rows[0].Id].RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));Check(ruler.PopupContent!=null,"Avatar click opens individual detail or aggregate member list");ruler.SelectPerson(host.S.Rows[0].Id);ruler.UpdateLayout();Render(ruler,Path.Combine(images,"ruler-detail-zh-Hans.png"));
     Check(ruler.SelectedId==host.S.Rows[0].Id&&ruler.PersonButtons[host.S.Rows[0].Id].ToolTip.ToString()!.Contains("2026-10-23"),"Explicit member selection selects contact and hover content includes its actual date");
     ruler.Width=720;ruler.UpdateLayout();ruler.Refresh(instant,viewer,true);ruler.UpdateLayout();Render(ruler,Path.Combine(images,"ruler-compact-zh-Hans.png"));
     Check(ruler.Placements.Count==7&&ruler.Placements.All(p=>p.CardLeft>=0),"Compact width retains all contact anchors");
     Check(JsonSerializer.Serialize(host.S.Rows)==original,"Opening, selecting, resizing and translating leaves city identities and layout unchanged");
     var row=host.S.Rows[0];ruler.RenameContact(row,"Friend test");Check(host.Disk.Load().Rows[0].Name=="Friend test"&&row.Group=="example","Contact rename persists without regrouping");
     var added=new Row{Id="added-test",City="东京",Zone="Tokyo Standard Time",Code="JP"};host.AddRow(added,null);Check(ruler.Placements.Any(p=>p.Person.Id==added.Id),"Existing city-add workflow updates the ruler");
     host.Remove(added);Check(!ruler.Placements.Any(p=>p.Person.Id==added.Id),"Removing a contact removes its ruler marker");
     ruler.Hide();host.ShowRuler();Check(ruler.IsVisible&&ReferenceEquals(ruler,host.Ruler),"Tray/manager reopen reuses the hidden ruler");
     var expected=JsonSerializer.Serialize(host.S.Docked);host.RecoverClocks();Check(JsonSerializer.Serialize(host.S.Docked)==expected,"Preview recovery does not clear saved classic edge groups");
     host.S.Docked.Add(new DockedGroup{Group=host.S.Rows[0].Group,Edge="right",Monitor="test",Offset=.5});expected=JsonSerializer.Serialize(host.S.Docked);
     host.CollapseAll();host.CollapseGroup(host.S.Rows[0]);host.ExpandAll();host.ExpandGroup(host.S.Rows[0].Group);Check(JsonSerializer.Serialize(host.S.Docked)==expected,"Classic fold/expand entry points are safe no-ops in preview-only mode");
     host.S.OriginalSystemTimeZone="China Standard Time";host.SetComputerTimeZone(host.S.Rows[0]);host.RestoreComputerTimeZone();Check(host.S.OriginalSystemTimeZone=="China Standard Time","Preview-only mode does not open a system-zone mutation path");
     ruler.Close();Check(!ruler.IsVisible,"Closing the preview hides it for tray recovery");
    }catch(Exception e){exit=1;lines.Add(e.ToString());}
    finally{host.Exiting=true;host.Tray.Dispose();host.Disk.Dispose();app.Shutdown();}
   }));app.Run();
  }catch(Exception e){exit=1;lines.Add(e.ToString());host?.Tray.Dispose();host?.Disk.Dispose();}
  finally{try{Directory.Delete(temp,true);}catch{}Directory.CreateDirectory(Path.GetDirectoryName(report)!);lines.Add($"TOTAL {count} PASS; synthetic model/WPF evidence, not native desktop or physical input acceptance");File.WriteAllLines(report,lines);}
  Environment.ExitCode=exit;
 }
 static void Render(Window window,string path){window.UpdateLayout();var visual=(FrameworkElement)window.Content;var image=new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth),(int)Math.Ceiling(visual.ActualHeight),96,96,PixelFormats.Pbgra32);image.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var file=File.Create(path);encoder.Save(file);}
}
