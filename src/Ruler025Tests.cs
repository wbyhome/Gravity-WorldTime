using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WorldTime;
public static class Ruler025Tests {
 public static void Run(string[] args){
  string report=Path.GetFullPath(args.Contains("--report")?args[Array.IndexOf(args,"--report")+1]:"ruler025-test.txt"),media=Path.Combine(Path.GetDirectoryName(report)!,"media"),temp=Path.Combine(Path.GetTempPath(),"WorldTime-025-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(media);
  var lines=new List<string>();int count=0,exit=0;Host? host=null;var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
  void Check(bool value,string name){if(!value)throw new Exception("FAIL "+name);lines.Add("PASS "+name);count++;}
  try{
   var state=new State{Language="zh-Hans",AppearanceVersion=2};foreach(string name in new[]{"纽约","伦敦","东京","悉尼"}){var city=Catalog.All.First(c=>c.Zh==name);state.Rows.Add(new Row{Id=city.En,Name=city.En,City=city.Zh,Code=city.Code,Zone=city.Zone});}using(var disk=new Store(temp))disk.Save(state);host=new Host(new Store(temp),true);
   app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>{
    try{
     host.ShowRuler();var w=host.Ruler!;var at=DateTimeOffset.Parse("2026-10-24T00:15:00Z");var viewer=TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");var saved=host.S.Rows;string json=JsonSerializer.Serialize(host.S);
     void Picture(string name,DateTimeOffset? time=null){w.Refresh(time??at,viewer,true);Capture(w,name);}
     void Capture(RulerWindow window,string name){var image=window.RenderNative(1);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var output=File.Create(Path.Combine(media,name+".png")))encoder.Save(output);}
     void Anchors(){Check(w.Placements.All(p=>Math.Abs(p.X-p.Person.Fraction*w.TrackWidth)<.0001),"Minute anchors unchanged by visual layout");Check(w.DateGroups.Sum(g=>g.Members.Count)==w.Placements.Count,"Each person retains exactly one actual date");Check(w.Clusters.All(c=>Math.Abs(c.X-c.Members[0].Fraction*w.TrackWidth)<.0001),"Portrait centres remain at actual local hours");}
     Point Center(RulerDateRail rail)=>new(rail.LabelBounds.Left+rail.LabelBounds.Width/2,w.DateTop+rail.LabelBounds.Top+rail.LabelBounds.Height/2);
     Picture("025-standard");Anchors();Check(w.DateRails.Count==2,"Default fixed-time example contains two dates");
     foreach(var rail in w.DateRails){double expected=Math.Clamp(w.CoreOrigin+(rail.Group.Start+rail.Group.End)/2,rail.LabelBounds.Width/2+3,w.Visual.ActualWidth-rail.LabelBounds.Width/2-3);Check(Math.Abs(expected-(rail.LabelBounds.Left+rail.LabelBounds.Width/2))<1,"Date label follows member extents, not a fixed left column");w.NativePointerMoved(Center(rail));Check(w.HoveredDate==rail.Group.Date&&w.DateHighlightedIds.OrderBy(x=>x).SequenceEqual(rail.Group.Members.Select(p=>p.Id).OrderBy(x=>x)),"Date hover selects only members of that real date");Capture(w,"025-hover-"+rail.Group.Date);}
     var target=w.DateRails.Last();w.NativePointerMoved(Center(target));int changes=w.HighlightChangeCount,builds=w.RenderCount;string stateHash=RulerViewAudit.Hash(Path.Combine(temp,"worldtime.json")),viewHash=RulerViewAudit.Hash(Path.Combine(temp,"ruler-view.json")),auditHash=RulerViewAudit.Hash(Path.Combine(temp,"ruler-view-writes.jsonl"));var watch=Stopwatch.StartNew();for(int i=0;i<10000;i++)w.NativePointerMoved(Center(target));watch.Stop();Check(w.HighlightChangeCount==changes&&w.RenderCount==builds,"10000 same-date pointer events do not rebuild scene or toggle highlight");lines.Add("INFO 10000 same-date pointer events ms: "+watch.Elapsed.TotalMilliseconds.ToString("F2"));Check(stateHash==RulerViewAudit.Hash(Path.Combine(temp,"worldtime.json"))&&viewHash==RulerViewAudit.Hash(Path.Combine(temp,"ruler-view.json"))&&auditHash==RulerViewAudit.Hash(Path.Combine(temp,"ruler-view-writes.jsonl")),"Hover changes neither contacts, view settings nor audit files");w.NativePointerLeft();Check(w.HoveredDate==""&&w.DateHighlightedIds.Count==0,"Leaving ruler clears date membership highlight");
     foreach(var rail in w.DateRails){var p=rail.Group.Members[0];w.NativePointerMoved(new Point(w.CoreOrigin+p.Fraction*w.TrackWidth,114+rail.Y));Check(w.HoveredDate==rail.Group.Date,"Date member dot can be hovered independently");}
     w.NativePointerLeft();Check(!w.NativeCanDrag(new Point(w.CoreOrigin,w.DateTop+4)),"Date layer remains separate from the integrated drag end");Check(!w.NativeCanDrag(Center(target)),"Date rail cannot start a ruler drag");Check(w.Clusters.Any(c=>c.Members[0].Self),"Self remains a visible primary portrait under aggregation");
     foreach(string language in new[]{"en","zh-Hans","zh-Hant","ja"}){host.S.Language=language;Picture("025-"+language);Check(w.DateRails.All(r=>RulerDateLayout.Caption(r.Group,language).Length>0),"Localized actual-date captions: "+language);}
     host.S.Language="zh-Hans";
     host.S.Rows=new(){new Row{Id="east",Name="UTC+14",Zone="Line Islands Standard Time"},new Row{Id="same-hour-west",Name="UTC-10",Zone="Hawaiian Standard Time"}};Picture("025-same-hour-different-date",DateTimeOffset.Parse("2026-10-24T10:30:00Z"));var mixed=w.Clusters.Single(c=>c.Members.Any(p=>p.Id=="east"));Check(mixed.Members.Select(p=>p.Date).Distinct().Count()==2,"Same-hour aggregate can contain different actual dates");var ownDay=w.DateRails.Single(r=>r.Group.Members.Any(p=>p.Self));w.NativePointerMoved(Center(ownDay));Capture(w,"025-partial-date-highlight");Check(w.DateHighlightedIds.Contains("same-hour-west")&&!w.DateHighlightedIds.Contains("east"),"Partial aggregate highlights true-date members without changing identity");w.NativePointerLeft();
     host.S.Rows=new(){new Row{Id="east",Name="UTC+14",Zone="Line Islands Standard Time"},new Row{Id="west",Name="UTC-12",Zone="Dateline Standard Time"}};Picture("025-three-dates",DateTimeOffset.Parse("2026-10-24T10:30:00Z"));Check(w.DateRails.Count==3,"Three simultaneous actual dates keep independent rows");Anchors();
     host.S.Rows=new(){new Row{Id="london",Name="Anna",Zone="GMT Standard Time"},new Row{Id="newyork",Name="Tom",Zone="Eastern Standard Time"}};Picture("025-same-date-three",DateTimeOffset.Parse("2026-10-24T12:00:00Z"));Check(w.DateRails.Count==1&&w.DateGroups[0].Members.Count==3,"Three same-date people share one membership rail");w.NativePointerMoved(Center(w.DateRails[0]));Capture(w,"025-same-date-three-hover");w.NativePointerLeft();
     host.S.Rows.RemoveAt(1);Picture("025-same-date-two",DateTimeOffset.Parse("2026-10-24T12:00:00Z"));Check(w.DateRails.Count==1&&w.DateGroups[0].Members.Count==2,"Two same-date people share one membership rail");
     host.S.Rows=new(){new Row{Id="early",Name="Anna",Zone="Pacific Standard Time"},new Row{Id="late",Name="Tom",Zone="Tokyo Standard Time"}};Picture("025-separated-members",DateTimeOffset.Parse("2026-10-24T12:00:00Z"));Check(w.DateGroups.Count==1&&w.DateGroups[0].End-w.DateGroups[0].Start>w.TrackWidth/2,"Widely separated same-date members retain real extents, with dashed relationship drawing");
     host.S.Rows=saved;Picture("025-before-midnight",DateTimeOffset.Parse("2026-10-23T15:59:00Z"));var oldDate=w.DateRails.Single(r=>r.Group.Members.Any(p=>p.Self));w.NativePointerMoved(Center(oldDate));Picture("025-after-midnight",DateTimeOffset.Parse("2026-10-23T16:00:00Z"));Anchors();Check(w.DateHighlightedIds.All(id=>w.Placements.Single(p=>p.Person.Id==id).Person.Date==w.HoveredDate),"Midnight refresh never keeps stale membership identities");w.NativePointerLeft();
     foreach(double opacity in new[]{1d,.5,.1,0}){w.SetBackgroundOpacity(opacity,false);Picture("025-opacity-"+(int)(opacity*100));}w.SetBackgroundOpacity(.65,false);
     w.SetCompact(true);Picture("025-compact");Check(w.Visual.ActualHeight<175,"Compact mode remains a short ruler with date captions");w.SetCompact(false);w.FitToWorkArea(new Rect(0,0,430,650));Picture("025-narrow");Anchors();Check(w.DateRails.All(r=>r.LabelBounds.Left>=0&&r.LabelBounds.Right<=w.Visual.ActualWidth+1),"Date labels remain inside a narrow work area");
     w.SaveView("025-test-save");string audit=File.ReadAllText(Path.Combine(temp,"ruler-view-writes.jsonl"));Check(audit.Contains("025-test-save")&&audit.Contains("AssemblySHA256")&&audit.Contains("BeforeSHA256"),"New-profile audit records explicit save reason, process, path and hashes");
     host.S.Language="zh-Hans";host.S.Rows=saved;Check(JsonSerializer.Serialize(host.S)==json,"All visual and hover scenarios preserve saved contact and routine state");
    }catch(Exception e){exit=1;lines.Add(e.ToString());}finally{host.Exiting=true;host.Tray.Dispose();host.Disk.Dispose();app.Shutdown();}
   }));app.Run();
  }catch(Exception e){exit=1;lines.Add(e.ToString());host?.Tray.Dispose();host?.Disk.Dispose();}
  finally{if(Directory.Exists(temp))try{Directory.Delete(temp,true);}catch{}lines.Add($"TOTAL {count} PASS; model/real-control rendering and programmatic events, not physical desktop acceptance");File.WriteAllLines(report,lines);Environment.ExitCode=exit;}
 }
}
