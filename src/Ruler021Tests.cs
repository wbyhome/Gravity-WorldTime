using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WorldTime;
public static class Ruler021Tests {
 // Different USNO solar-coordinate + sidereal-hour-angle formulation, for cross-checks.
 static double Usno(DateTimeOffset at,double lat,double lon){
  double d=(at-DateTimeOffset.Parse("2000-01-01T12:00:00Z")).TotalDays,r=Math.PI/180;
  double g=(357.529+.98560028*d)*r,q=280.459+.98564736*d,l=(q+1.915*Math.Sin(g)+.020*Math.Sin(2*g))*r,e=(23.439-.00000036*d)*r;
  double ra=Math.Atan2(Math.Cos(e)*Math.Sin(l),Math.Cos(l)),decl=Math.Asin(Math.Sin(e)*Math.Sin(l));
  double ha=(280.46061837+360.98564736629*d+lon)*r-ra;
  return Math.Asin(Math.Sin(lat*r)*Math.Sin(decl)+Math.Cos(lat*r)*Math.Cos(decl)*Math.Cos(ha))/r;
 }
 public static void Run(string[] args){
  string report=Path.GetFullPath(args.Contains("--report")?args[Array.IndexOf(args,"--report")+1]:"ruler021-test.txt"),images=Path.Combine(Path.GetDirectoryName(report)!,Path.GetFileNameWithoutExtension(report)+"-images");Directory.CreateDirectory(images);
  string temp=Path.Combine(Path.GetTempPath(),"WorldTime-021-"+Guid.NewGuid().ToString("N"));var lines=new List<string>();int count=0,exit=0;Host? host=null;
  void Check(bool value,string name){if(!value)throw new Exception("FAIL "+name);lines.Add("PASS "+name);count++;}
  Row City(string zh){var c=Catalog.All.First(c=>c.Zh==zh);return new Row{City=c.Zh,Code=c.Code,Zone=c.Zone,Name=c.En};}
  void Models(){
   var points=new[]{(0d,0d),(40.714d,-74.006d),(-33.867d,151.217d),(78.217d,15.633d),(35.65d,139.75d),(-1.28d,36.82d)};double error=0;
   foreach(int month in new[]{1,3,6,9,12})foreach(int hour in new[]{0,6,12,18})foreach(var (lat,lon) in points){var at=new DateTimeOffset(2026,month,21,hour,0,0,TimeSpan.Zero);double a=NatureModel.Solar(at,lat,lon).Elevation,b=Usno(at,lat,lon);error=Math.Max(error,Math.Abs(a-b));Check(Math.Abs(a-b)<1.2,$"NOAA approximation vs independent USNO formulation {month}/{hour}/{lat}/{lon}");}
   lines.Add("INFO maximum solar elevation difference degrees: "+error.ToString("F4",System.Globalization.CultureInfo.InvariantCulture));
   var ny=City("纽约");Check(NatureModel.Resolve(ny) is {Latitude:>40 and <41,Longitude:<-73 and >-75},"New York coordinates resolve from exact catalog IANA identity");
   Check(NatureModel.Resolve(City("北京"))==null,"Beijing without a catalog IANA coordinate stays unknown, not borrowed from Shanghai");
   Check(NatureModel.Resolve(new Row{City="invented",Zone="Eastern Standard Time"})==null,"A Windows time-zone ID alone never supplies a geographic coordinate");
   Check(NatureModel.Resolve(new Row{City="纽约",Code="GB",Zone="Eastern Standard Time"})==null,"Conflicting country identity degrades instead of silently borrowing coordinates");
   var north=City("伦敦");var south=City("悉尼");var tropical=City("新加坡");
   RulerPerson Person(Row row,DateTimeOffset at)=>RulerModel.Snapshot(new State{Rows=new(){row}},at,TimeZoneInfo.Utc).Single(p=>!p.Self);
   foreach(var (month,season) in new[]{(1,"Calendar winter"),(4,"Calendar spring"),(7,"Calendar summer"),(10,"Calendar autumn")}){var at=new DateTimeOffset(2026,month,21,12,0,0,TimeSpan.Zero);Check(NatureModel.At(Person(north,at),at).Season==season,"Northern calendar season: "+season);}
   var jan=DateTimeOffset.Parse("2026-01-21T12:00:00Z");Check(NatureModel.At(Person(south,jan),jan).Season=="Calendar summer","Southern hemisphere calendar season is inverted");Check(NatureModel.At(Person(tropical,jan),jan).Season=="Local seasons vary","Low latitude does not invent four local seasons or wet/dry weather");
   var polar=City("朗伊尔城");DateTimeOffset june=DateTimeOffset.Parse("2026-06-21T12:00:00Z"),dec=DateTimeOffset.Parse("2026-12-21T12:00:00Z");Check(NatureModel.At(Person(polar,june),june).Light=="Polar daylight estimate","High-latitude summer supports polar-day estimate");Check(NatureModel.At(Person(polar,dec),dec).Light=="Polar night estimate","High-latitude winter supports polar-night estimate");
   Check(NatureModel.Solar(june,35,139)==NatureModel.Solar(june.ToOffset(TimeSpan.FromHours(9)),35,139),"Solar state depends on the UTC instant, not the civil offset or DST");
   var wake=new Row{Id="wake",Zone="UTC",Routine=new CityRoutine{Wake=1430,Sleep=900,Start=60,End=300,BreakEnabled=false,DaysOff=0}};var early=RulerModel.Snapshot(new State{Language="en",Rows=new(){wake}},DateTimeOffset.Parse("2026-10-24T00:10:00Z"),TimeZoneInfo.Utc)[1];Check(early.Status=="May be waking up","Wake-relative hint wraps across midnight");
   var same=RulerModel.Snapshot(new State{Rows=new(){new Row{Id="@me",Zone="UTC"}}},jan,TimeZoneInfo.Utc);Check(same.Select(p=>p.Id).Distinct().Count()==2,"Virtual self ID does not collide with legacy contact IDs");
   var many=Enumerable.Range(0,64).Select(i=>same[1] with{Id="crowd-"+i}).ToList();var grouped=CompactRulerLayout.Arrange(many,300,100,2);Check(grouped.SelectMany(g=>g.Members).Count()==64&&grouped.All(g=>g.Lane<2),"64 same-hour people aggregate into bounded rows without being dropped");
   Check(grouped.All(g=>Math.Abs(g.X-g.Members[0].Fraction*300)<.001),"Representative avatar retains its own true-hour anchor");
   string source=Path.Combine(temp,"profiles");Directory.CreateDirectory(Path.Combine(source,"data"));File.WriteAllText(Path.Combine(source,"data","worldtime.json"),"{}");bool rejected=false;try{PreviewProfile.Resolve(source,Path.Combine(source,"data"));}catch(InvalidOperationException){rejected=true;}Check(rejected,"Preview CLI refuses the classic data directory");
   var unmarked=Path.Combine(source,"unmarked");Directory.CreateDirectory(unmarked);File.WriteAllText(Path.Combine(unmarked,"worldtime.json"),"{}");rejected=false;try{PreviewProfile.Resolve(source,unmarked);}catch(InvalidOperationException){rejected=true;}Check(rejected&&File.ReadAllText(Path.Combine(unmarked,"worldtime.json"))=="{}","Existing unmarked configuration is rejected without modification");
   Check(PreviewProfile.Resolve(source,null).EndsWith("ruler-preview-data"),"No explicit preview profile uses a separate safe default");
  }
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};try{
   Models();var state=new State{Language="zh-Hans",AppearanceVersion=2};foreach(string name in new[]{"纽约","伦敦","东京","悉尼"})state.Rows.Add(City(name));using(var seed=new Store(temp))seed.Save(state);host=new Host(new Store(temp),true);
   app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new System.Action(()=>{
    try{
     string original=JsonSerializer.Serialize(host.S);host.ShowRuler();var w=host.Ruler!;var viewer=TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");var at=DateTimeOffset.Parse("2026-10-24T00:15:00Z");
     void Picture(string name,DateTimeOffset instant){w.Refresh(instant,viewer,true);w.UpdateLayout();Render((FrameworkElement)w.Content,Path.Combine(images,name+".png"));}
     foreach(string language in new[]{"zh-Hans","en","zh-Hant","ja"}){host.S.Language=language;Picture("five-"+language,at);Check(((FrameworkElement)w.Content).ActualHeight<390&&w.Placements.Count==5,"Five-person compact-height visual in "+language);}
     host.S.Language="zh-Hans";w.SetCompact(true);w.UpdateLayout();Picture("compact",at);Check(((FrameworkElement)w.Content).ActualHeight<270&&w.Placements.Count==5,"Compact mode lowers height while retaining all contacts");lines.Add("INFO compact content DIP: "+((FrameworkElement)w.Content).ActualWidth+" x "+((FrameworkElement)w.Content).ActualHeight);w.SetCompact(false);w.UpdateLayout();
     Picture("before-midnight",DateTimeOffset.Parse("2026-10-23T15:59:00Z"));var before=w.Placements.Single(p=>p.Person.Self);Picture("after-midnight",DateTimeOffset.Parse("2026-10-23T16:00:00Z"));var after=w.Placements.Single(p=>p.Person.Self);Check(before.Person.Date!=after.Person.Date&&before.Person.Fraction>.99&&after.Person.Fraction==0,"23:59 to 00:00 changes date and moves anchor to the start without losing anyone");
     foreach(int month in new[]{1,4,7,10})Picture("seasons-"+month,new DateTimeOffset(2026,month,21,12,0,0,TimeSpan.Zero));
     var saved=host.S.Rows;host.S.Rows=Enumerable.Range(0,32).Select(i=>new Row{Id="same-"+i,Name="Friend "+i,City="纽约",Code="US",Zone="Eastern Standard Time"}).ToList();Picture("same-hour-32",at);Check(w.Placements.Count==33&&((FrameworkElement)w.Content).ActualHeight<390,"Crowded contacts do not turn the panel into an unbounded tall board");
     var cluster=w.Clusters.OrderByDescending(c=>c.Members.Count).First();w.PersonButtons[cluster.Members[0].Id].RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Check(w.PopupContent!=null,"Aggregated avatar opens a member list through WPF routed click");
     w.SelectPerson(cluster.Members[^1].Id);Check(w.SelectedId==cluster.Members[^1].Id,"Last member of a large aggregate remains selectable");
     if(w.PopupContent is FrameworkElement details){details.UpdateLayout();Render(details,Path.Combine(images,"contact-details.png"));}
     host.S.Rows=saved;w.FitToWorkArea(new Rect(0,0,430,650));w.UpdateLayout();Picture("narrow-work-area",at);Check(w.Width<=410&&w.Placements.All(p=>p.X>=0),"A synthetic 430-DIP work area fits without the old 640-DIP minimum");
     Render((FrameworkElement)w.Content,Path.Combine(images,"simulated-200-percent.png"),2);w.Width=820;w.UpdateLayout();Picture("nature-disabled",at);w.SetNature(false);Picture("nature-disabled",at);Check(!w.NatureEnabled,"Nature accents can be switched off");w.SetNature(true);
     w.Refresh(at,viewer,true);int renders=w.RenderCount;var watch=Stopwatch.StartNew();for(int i=0;i<500;i++)w.Refresh(at,viewer);watch.Stop();Check(w.RenderCount==renders,"500 unchanged snapshots produce no full redraw");lines.Add("INFO 500 cache hits elapsed ms: "+watch.Elapsed.TotalMilliseconds.ToString("F2"));
     host.S.Language="zh-Hans";Check(JsonSerializer.Serialize(host.S)==original,"Visual modes, seasons, aggregation and resizing preserve all saved state");
    }catch(Exception e){exit=1;lines.Add(e.ToString());}finally{host.Exiting=true;host.Tray.Dispose();host.Disk.Dispose();app.Shutdown();}
   }));app.Run();
  }catch(Exception e){exit=1;lines.Add(e.ToString());host?.Tray.Dispose();host?.Disk.Dispose();}
  finally{try{Directory.Delete(temp,true);}catch{}lines.Add($"TOTAL {count} PASS; synthetic time/nature/WPF checks, not weather or physical desktop acceptance");File.WriteAllLines(report,lines);}Environment.ExitCode=exit;
 }
 static void Render(FrameworkElement element,string path,double scale=1){element.UpdateLayout();var image=new RenderTargetBitmap(Math.Max(1,(int)Math.Ceiling(element.ActualWidth*scale)),Math.Max(1,(int)Math.Ceiling(element.ActualHeight*scale)),96*scale,96*scale,PixelFormats.Pbgra32);image.Render(element);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using var f=File.Create(path);encoder.Save(f);}
}
