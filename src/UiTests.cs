using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Windows.Threading;

namespace WorldTime;
// Integration tests instantiate our own WPF app and isolated data only.
// They do not send input to other apps or claim physical tray-click evidence.
public static class UiTests {
 public static void Run(string[] args){
  string report=args.Contains("--report")?args[Array.IndexOf(args,"--report")+1]:"ui-results.txt";
  string images=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(report)??".",Path.GetFileNameWithoutExtension(report)+"-images"));Directory.CreateDirectory(images);
  string temp=Path.Combine(Path.GetTempPath(),"WorldTime-ui-"+Guid.NewGuid().ToString("N"));
  var lines=new List<string>();int count=0,exit=0;Host? host=null;
  void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);lines.Add("PASS "+name);count++;}
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
  int trace=0;WindowLayer.Diagnostics=s=>{if(trace++<100)File.AppendAllText(report+".trace",s+Environment.NewLine);};
  try{
   // Seed an actually saved folded layout before constructing the application.
   var resumed=new State{AppearanceVersion=1};int order=0;
   foreach(string name in new[]{"伦敦","纽约","东京"}){var c=Catalog.All.First(c=>c.Zh==name);resumed.Rows.Add(new Row{City=c.Zh,Zone=c.Zone,Code=c.Code,Group="resume",Order=order,Y=160+order++*Layout.Step});}
   resumed.Docked.Add(new DockedGroup{Group="resume",Edge="right",Monitor="missing-test-monitor",Offset=.4});
   using(var seed=new Store(temp))seed.Save(resumed);
   File.AppendAllText(report+".trace","Creating host\n");host=new Host(new Store(temp));File.AppendAllText(report+".trace","Host ready\n");
   app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>{
    try{
     Check(!host.Manager.IsVisible,"Manager hidden at startup");Check(host.Tray.Visible,"NotifyIcon requested visible");Check(!host.Manager.ShowInTaskbar&&host.Bars.Values.All(b=>!b.ShowInTaskbar),"All app windows excluded from taskbar");
     Check(host.EdgeTabs.Count==1&&host.Bars.Values.All(b=>!b.IsVisible),"Saved folded group restores as one edge tab at startup");
     var initialTab=host.EdgeTabs.Values.First();Check(DockScreen.Bounds(initialTab).IntersectsWith(new Rect(DockScreen.Find("").Left,DockScreen.Find("").Top,DockScreen.Find("").Right-DockScreen.Find("").Left,DockScreen.Find("").Bottom-DockScreen.Find("").Top)),"Missing monitor falls back to an available screen");
     host.ExpandAll();Check(host.Bars.Count==3&&host.Bars.Values.All(b=>b.IsVisible),"Expansion restores three desktop clocks");
     string layout=JsonSerializer.Serialize(host.S.Rows);
     foreach(string code in new[]{"en","zh-Hans","zh-Hant","ja"}){
      host.S.Language=code;host.Sync();host.Save();host.Manager.Reveal();host.Manager.UpdateLayout();
      Check(host.Manager.IsVisible&&!host.Manager.ShowInTaskbar,"Manager available without taskbar entry: "+code);
      Check(host.Bars.Values.All(b=>b.PeriodText==L10n.Text(code,DayPart.Key(Catalog.Local(b.R.Zone,DateTimeOffset.UtcNow).Hour))),"Visible local-period badges: "+code);
      foreach(double font in new[]{11d,18d})foreach(double width in new[]{480d,620d}){
       host.S.Font=font;foreach(var row in host.S.Rows)row.Width=width;host.Sync();foreach(var bar in host.Bars.Values)bar.UpdateLayout();
       Check(host.Bars.Values.All(b=>b.HintFits),$"Hint fits without ellipsis: {code}, {font}px, {width} DIP");
      }
      host.S.Font=13;foreach(var row in host.S.Rows)row.Width=620;host.Sync();foreach(var bar in host.Bars.Values)bar.UpdateLayout();
      foreach(var pair in L10n.Resources)Check(!string.IsNullOrWhiteSpace(L10n.Text(code,pair.Key)),"Resource "+code+": "+pair.Key);
      Check(host.Bars.Values.All(b=>b.ContactText.Length>0),"Contact advice is visible on its own line: "+code);
      Render(host.Manager,Path.Combine(images,"manager-"+code+".png"));
      Render(host.Bars.Values.First(),Path.Combine(images,"clock-"+code+".png"));
      host.Manager.Close();Check(!host.Manager.IsVisible&&host.Bars.Values.All(b=>b.IsVisible),"Close panel hides manager and preserves clocks: "+code);
     }
     host.S.Life=false;host.S.Advice=false;host.Sync();Check(host.Bars.Values.All(b=>b.PeriodText.Length>0),"Period badge survives hidden hints");
     host.S.Language="zh-Hans";host.Sync();
     var fakeZone=new TimeZoneChecks.Fake();
     var zoneDialog=new TimeZoneDialog(host,"Eastern Standard Time",system:fakeZone);zoneDialog.Show();zoneDialog.UpdateLayout();
     Check(fakeZone.Sets==0&&zoneDialog.Summary.Text.Contains("China Standard Time")&&zoneDialog.Summary.Text.Contains("Eastern Standard Time"),"Time-zone preview shows both zones without changing Windows");
     Render(zoneDialog,Path.Combine(images,"computer-time-zone-zh-Hans.png"));zoneDialog.Close();
     Check(fakeZone.Sets==0&&host.S.OriginalSystemTimeZone=="","Closing time-zone preview cancels without settings changes");
     zoneDialog=new TimeZoneDialog(host,"Eastern Standard Time",system:fakeZone);zoneDialog.Show();
     Check(zoneDialog.ApplyChanges().GetAwaiter().GetResult()&&fakeZone.Zone=="Eastern Standard Time"&&host.Disk.Load().OriginalSystemTimeZone=="China Standard Time","Confirmation applies fake zone and persists recovery state");zoneDialog.Close();
     zoneDialog=new TimeZoneDialog(host,"China Standard Time",true,fakeZone);zoneDialog.Show();
     Check(zoneDialog.ApplyChanges().GetAwaiter().GetResult()&&fakeZone.Zone=="China Standard Time"&&host.S.OriginalSystemTimeZone=="","Restore dialog uses saved zone and clears verified recovery state");zoneDialog.Close();
     Check(JsonSerializer.Serialize(host.S.Rows)==layout,"Language switches preserve exact layout");
     foreach(string edge in DockGeometry.Edges){
      host.CollapseGroup(host.S.Rows[0],edge);var tab=host.EdgeTabs.Values.Single();tab.UpdateLayout();var area=DockScreen.Find(tab.Dock.Monitor);var rect=DockScreen.Bounds(tab);
      Check(host.Bars.Values.All(b=>!b.IsVisible)&&!tab.ShowInTaskbar&&tab.IsVisible,"Collapse replaces group with taskbar-free tab: "+edge);
      Check(rect.Left>=area.Left-2&&rect.Right<=area.Right+2&&rect.Top>=area.Top-2&&rect.Bottom<=area.Bottom+2,"Tab stays inside screen work area: "+edge);
      Check(Math.Abs(edge=="left"?rect.Left-area.Left:edge=="right"?rect.Right-area.Right:edge=="top"?rect.Top-area.Top:rect.Bottom-area.Bottom)<2,"Tab touches requested edge: "+edge);
      Check(host.Disk.Load().Docked.Single().Edge==edge,"Folded edge saved to disk: "+edge);
      Render(tab,Path.Combine(images,"edge-"+edge+".png"));((Button)tab.Content).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
      Check(host.EdgeTabs.Count==0&&host.Bars.Values.All(b=>b.IsVisible)&&JsonSerializer.Serialize(host.S.Rows)==layout,"Expansion restores exact layout: "+edge);
     }
     var lastRow=host.S.Rows[^1];string oldGroup=lastRow.Group;int oldOrder=lastRow.Order;Layout.Detach(host.S,lastRow);
     host.CollapseGroup(host.S.Rows[0],"right");host.CollapseGroup(lastRow,"right");
     var tabRects=host.EdgeTabs.Values.Select(t=>DockScreen.Bounds(t)).ToArray();Check(tabRects.Length==2&&!tabRects[0].IntersectsWith(tabRects[1]),"Nearby folded groups get separate edge tabs");
     host.ExpandAll();lastRow.Group=oldGroup;lastRow.Order=oldOrder;host.Sync();
     Check(JsonSerializer.Serialize(host.S.Rows)==layout,"Multiple group fold/expand preserves layout");
     host.Manager.Reveal();host.Manager.WindowState=WindowState.Minimized;
     Check(!host.Manager.IsVisible&&host.Bars.Values.All(b=>b.IsVisible&&b.WindowState==WindowState.Normal),"Minimizing manager does not minimize clock bars");
     host.SetLayer("bottom");host.CollapseAll();host.Bars.Values.First().WindowState=WindowState.Minimized;host.RecoverClocks();
     Check(host.S.Layer=="top"&&host.Bars.Values.All(b=>b.WindowState==WindowState.Normal),"Tray recovery action restores clocks and selects top mode");
     Check(host.EdgeTabs.Count==0&&host.S.Docked.Count==0,"Recovery expands folded groups too");
     Check(JsonSerializer.Serialize(host.S.Rows)==layout,"Recovery preserves saved layout");
     host.SetLayer("normal");Check(host.Bars.Values.All(b=>b.Layer.IsDesktopHosted),"Desktop mode attaches all clocks to the real Explorer desktop host");
     Check(host.Bars.Values.All(b=>b.Layer.Surface is {PaintSucceeded:true,OpaquePixels:>20}),"Desktop surfaces upload actual alpha bitmaps with readable opaque text pixels");
     for(int cycle=0;cycle<3;cycle++){
      var oldBars=host.Bars.Values.ToList();
      host.CollapseAll();host.SetLayer("top");
      Check(oldBars.All(b=>!host.Bars.Values.Contains(b)),"Layer switch creates fresh WPF surfaces: "+cycle);
      Check(host.Bars.Values.All(b=>!b.IsVisible)&&host.S.Docked.Count==1,"Layer switch retains folding: "+cycle);
      host.ExpandAll();
      Check(host.Bars.Values.All(b=>b.IsVisible&&!b.Layer.IsDesktopHosted&&b.Topmost),"Expanded fresh floating clocks visible and topmost: "+cycle);
      host.SetLayer("normal");
      Check(host.Bars.Values.All(b=>b.IsVisible&&b.Layer.IsDesktopHosted),"Fresh desktop clocks after repeated fold/layer/expand cycle: "+cycle);
      Check(host.Bars.Values.All(b=>b.Layer.Surface is {PaintSucceeded:true,OpaquePixels:>20}),"Rebuilt desktop surfaces successfully upload real clock images: "+cycle);
      Check(JsonSerializer.Serialize(host.S.Rows)==layout,"Layer reconstruction preserves exact rows: "+cycle);
     }
     void RestoreLayout(){
      var saved=JsonSerializer.Deserialize<List<Row>>(layout)!;
      foreach(var r in host.S.Rows){var old=saved.Single(x=>x.Id==r.Id);r.Group=old.Group;r.Order=old.Order;r.X=old.X;r.Y=old.Y;r.Width=old.Width;}
      host.S.Docked.Clear();host.Sync();host.Save();
     }
     var rows=host.S.Rows;var first=host.Bars[rows[0].Id];var middle=host.Bars[rows[1].Id];var lastBar=host.Bars[rows[2].Id];
     var firstDpi=System.Windows.Media.VisualTreeHelper.GetDpi(first);
     Check(first.BeginDrag(new Point(0,0),false),"First row body begins whole-stack move");first.ContinueDrag(new Point(70*firstDpi.DpiScaleX,18*firstDpi.DpiScaleY));first.CompleteDrag(true);
     Check(rows.All(r=>Math.Abs(r.X-190)<.5)&&rows.Select(r=>r.Group).Distinct().Count()==1,"First row moves every clock without changing membership");RestoreLayout();
     Check(!middle.BeginDrag(new Point(0,0),false)&&JsonSerializer.Serialize(rows)==layout,"Lower body does not drag or detach anything");
     Check(middle.BeginDrag(new Point(0,0),true),"Middle header begins suffix move");middle.ContinueDrag(new Point(100*firstDpi.DpiScaleX,20*firstDpi.DpiScaleY));middle.CompleteDrag(true);
     Check(rows[0].X==120&&rows[0].Y==160&&rows[0].Group=="resume","Middle-header drag leaves upper clock unchanged");
     Check(rows[1].Group==rows[2].Group&&rows[1].Group!=rows[0].Group&&rows[1].Order==0&&rows[2].Order==1&&Math.Abs(rows[2].Y-rows[1].Y-Layout.Step)<.1,"Middle header moves itself and the entire suffix as one ordered group");
     Check(host.Disk.Load().Rows[1].Group==rows[1].Group,"Suffix split persists to disk");RestoreLayout();
     lastBar.BeginDrag(new Point(0,0),true);lastBar.ContinueDrag(new Point(100*firstDpi.DpiScaleX,0));lastBar.CompleteDrag(true);
     Check(rows[0].Group==rows[1].Group&&rows[2].Group!=rows[1].Group,"Last header detaches only its own clock");RestoreLayout();
     middle.BeginDrag(new Point(0,0),true);middle.ContinueDrag(new Point(1,1));middle.CompleteDrag();Check(JsonSerializer.Serialize(rows)==layout,"Click or tiny motion does not split a stack");
     foreach(string edge in DockGeometry.Edges){
      RestoreLayout();var area=DockScreen.For(first);var bounds=DockScreen.Bounds(first);foreach(var r in rows.Skip(1))bounds.Union(DockScreen.Bounds(host.Bars[r.Id]));
      double dx=edge=="left"?area.Left+2-bounds.Left:edge=="right"?area.Right-bounds.Right-2:0;
      double dy=edge=="top"?area.Top+2-bounds.Top:edge=="bottom"?area.Bottom-bounds.Bottom-2:0;
      first.BeginDrag(new Point(0,0),true);first.ContinueDrag(new Point(dx,dy));first.CompleteDrag();
      Check(host.S.Docked.Single().Edge==edge&&host.Bars.Values.All(b=>!b.IsVisible),"Releasing at "+edge+" edge automatically folds, with no button click");
      Check(host.Disk.Load().Docked.Single().Edge==edge,"Automatic edge folding persists: "+edge);
      host.ExpandAll();Check(host.Bars.Values.All(b=>b.IsVisible)&&host.S.Docked.Count==0,"Expanded group stays open until a new drag: "+edge);
     }
     RestoreLayout();Check(JsonSerializer.Serialize(rows)==layout,"Interaction tests restore the isolated baseline exactly");
     Check(L10n.Resolve("auto","en-US")=="en"&&L10n.Resolve("auto","de-DE")=="en","English base and unsupported-system fallback");
     Check(L10n.Resolve("auto","zh-CN")=="zh-Hans"&&L10n.Resolve("auto","zh-TW")=="zh-Hant"&&L10n.Resolve("auto","zh-HK")=="zh-Hant"&&L10n.Resolve("auto","ja-JP")=="ja","Supported system language selection");
     Check(L10n.Resolve("en","zh-CN")=="en","Manual English overrides system language");
     foreach(var (hour,expected) in new[]{(0,"Wee hours"),(4,"Wee hours"),(5,"Morning"),(11,"Morning"),(12,"Afternoon"),(16,"Afternoon"),(17,"Evening"),(18,"Evening"),(19,"Night"),(22,"Night"),(23,"Late night")})Check(DayPart.Key(hour)==expected,"Period boundary "+hour);
     Check(L10n.Resources.All(p=>p.Value.Length==3&&p.Value.All(v=>v.Length>0)),"All resources cover the three non-English translations");
     Check(JsonSerializer.Deserialize<State>(JsonSerializer.Serialize(new State{Language="ja"}))!.Language=="ja","Manual language persists");
     bool rejected=false;try{Store.Validate(new State{Language="unsupported"});}catch(InvalidDataException){rejected=true;}Check(rejected,"Reject unsupported saved language");
     host.S.Language="zh-Hans";host.Sync();string beforeRoutine=JsonSerializer.Serialize(host.S.Rows);var routineRow=host.S.Rows[0];
     var editor=new RoutineEditor(host,routineRow);editor.Show();editor.Inherit.IsChecked=false;editor.Wake.Text="17:00";editor.Sleep.Text="08:00";editor.Start.Text="22:00";editor.End.Text="06:00";editor.BreakEnabled.IsChecked=false;editor.DaysOff=65;editor.UpdateLayout();
     Render(editor,Path.Combine(images,"routine-editor-zh-Hans.png"));
     Check(editor.SaveChanges()&&routineRow.Routine is {Start:1320,End:360,Wake:1020,Sleep:480},"Routine editor saves an individual overnight routine");
     Check(host.S.Rows.Skip(1).All(r=>r.Routine==null)&&host.S.Start==9&&host.S.End==18,"Saving one city's routine preserves every other city and default hours");
     Check(host.Disk.Load().Rows.Single(r=>r.Id==routineRow.Id).Routine==routineRow.Routine,"Routine editor persists custom hours and rest days");
     var originalRoutine=routineRow.Routine;var canceled=new RoutineEditor(host,routineRow);canceled.Show();canceled.Wake.Text="01:00";canceled.Close();
     Check(ReferenceEquals(originalRoutine,routineRow.Routine),"Closing a routine editor without saving does not mutate the city");
     var invalidEditor=new RoutineEditor(host,routineRow);invalidEditor.Show();invalidEditor.Wake.Text="nonsense";
     Check(!invalidEditor.SaveChanges()&&invalidEditor.Error.Text.Length>0&&ReferenceEquals(originalRoutine,routineRow.Routine),"Invalid routine stays in the editor without changing saved data");
     invalidEditor.Inherit.IsChecked=true;Check(invalidEditor.SaveChanges()&&routineRow.Routine==null,"A custom city can return to inherited defaults");
     Check(JsonSerializer.Serialize(host.S.Rows)==beforeRoutine,"Routine UI tests retain the original city identities and geometry");
     var newYork=host.Bars.Values.Single(b=>b.R.Zone=="Eastern Standard Time");newYork.Refresh(DateTimeOffset.Parse("2026-01-01T00:30:00Z"));newYork.UpdateLayout();
     Check(newYork.DateText=="12/31","Date column shows the city calendar date without relative-day labels");
     var dateImage=new RenderTargetBitmap(620,50,96,96,PixelFormats.Pbgra32);dateImage.Render((Visual)newYork.Content);var dateEncoder=new PngBitmapEncoder();dateEncoder.Frames.Add(BitmapFrame.Create(dateImage));using(var output=File.Create(Path.Combine(images,"date-only-zh-Hans.png")))dateEncoder.Save(output);
     host.S.Twelve=true;newYork.Refresh(DateTimeOffset.Parse("2026-01-01T00:30:00Z"));Check(newYork.DateText=="12/31\nPM","Twelve-hour mode retains AM/PM without a relative-day label");host.S.Twelve=false;newYork.Refresh();
     Window? fakeA=null,fakeB=null;
     try{
      fakeA=new Window{Title="WorldTime isolated desktop host A",Left=0,Top=0,Width=1000,Height=700,WindowStyle=WindowStyle.None,ShowInTaskbar=false,ShowActivated=false};fakeA.Show();
      IntPtr parentA=new WindowInteropHelper(fakeA).Handle;WindowLayer.DesktopHostForTest=()=>parentA;host.SetLayer("normal");
      var oldSurfaces=host.Bars.Values.Select(b=>b.Layer.Surface!).ToList();var renderers=host.Bars.Values.ToDictionary(b=>b.R.Id,b=>new WindowInteropHelper(b).Handle);
      Check(oldSurfaces.All(s=>s.PaintSucceeded),"All clock bitmaps paint on an isolated substitute desktop host");
      fakeA.Close();Check(oldSurfaces.All(s=>s.Handle==IntPtr.Zero&&!s.PaintSucceeded),"Native parent destruction clears child handles and paint status");
      WindowLayer.DesktopHostForTest=()=>IntPtr.Zero;foreach(var bar in host.Bars.Values)bar.Layer.Maintain();
      Check(host.Bars.Values.All(b=>b.Layer.BindingError!=null)&&host.S.Layer=="normal","Missing desktop host defers retry without silently switching to top mode");
      fakeB=new Window{Title="WorldTime isolated desktop host B",Left=80,Top=60,Width=1000,Height=700,WindowStyle=WindowStyle.None,ShowInTaskbar=false,ShowActivated=false};fakeB.Show();
      IntPtr parentB=new WindowInteropHelper(fakeB).Handle;WindowLayer.DesktopHostForTest=()=>parentB;foreach(var bar in host.Bars.Values)bar.Layer.Maintain();
      Check(host.Bars.Values.All(b=>b.Layer.IsDesktopHosted&&b.Layer.Surface is {PaintSucceeded:true}&&new WindowInteropHelper(b).Handle==renderers[b.R.Id]),"Desktop host recreation restores native painting while preserving each WPF renderer");
      Check(host.Bars.Values.All(b=>Math.Abs(DockScreen.Bounds(b).Left-DisplayLayout.Physical(b.R).X)<1&&Math.Abs(DockScreen.Bounds(b).Top-DisplayLayout.Physical(b.R).Y)<1),"Recreated desktop host with a shifted origin preserves physical clock positions");
      host.CollapseAll();foreach(var bar in host.Bars.Values)bar.Layer.Maintain();Check(host.Bars.Values.All(b=>!b.Layer.Surface!.IsShown),"Maintenance never reopens intentionally folded clocks");host.ExpandAll();
     }finally{WindowLayer.DesktopHostForTest=null;if(fakeA?.IsVisible==true)fakeA.Close();if(fakeB?.IsVisible==true)fakeB.Close();foreach(var bar in host.Bars.Values)bar.Layer.Maintain();host.Sync();}
     Check(host.Bars.Values.All(b=>b.Layer.IsDesktopHosted&&b.Layer.Surface is {PaintSucceeded:true}),"Clock presentation returns to the real desktop after isolated recovery tests");
     Check(JsonSerializer.Serialize(host.S.Rows)==beforeRoutine,"Desktop recovery simulations preserve all city settings and positions");
     host.S.Language="zh-Hans";host.Sync();string beforePicker=JsonSerializer.Serialize(host.S.Rows);
     var picker=new CityPicker(host,null);picker.Show();picker.UpdateLayout();
     Check(picker.CountrySelector.Items.Count>=241&&picker.CityList.Items.Count>=400,"City picker offers country filtering and the expanded offline catalog");
     picker.SearchBox.Text="美国";picker.UpdateLayout();Render(picker,Path.Combine(images,"city-picker-zh-Hans.png"));
     picker.SearchBox.Text="韩国";Check(picker.CityList.Items.Count>0&&picker.CityList.Items.Cast<ListBoxItem>().All(i=>((City)i.Tag).Code=="KR"),"Country-name search returns the expected locations in the real picker");
     picker.ZoneSelector.SelectedItem=TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
     Check(picker.CityList.SelectedItem==null,"Selecting an explicit time zone clears the city selection");
     picker.SearchBox.Text="Vietnam";Check(picker.ZoneSelector.SelectedItem==null&&picker.CityList.SelectedItem!=null,"A new city search clears the previously selected explicit zone");
     picker.CountrySelector.SelectedItem=picker.CountrySelector.Items.Cast<CountryChoice>().Single(c=>c.Code=="KR");
     Check(picker.CityList.Items.Count==0&&!picker.AddButton.IsEnabled,"Country filter plus unmatched search disables adding a stale selection");
     picker.SearchBox.Text="韩国";Check(picker.AddSelected(),"Selected catalog city can be added through the actual picker");
     var added=host.S.Rows.Single(r=>!JsonSerializer.Deserialize<System.Collections.Generic.List<Row>>(beforePicker)!.Any(old=>old.Id==r.Id));
     Check(added.Code=="KR"&&added.Zone=="Korea Standard Time"&&host.Disk.Load().Rows.Any(r=>r.Id==added.Id),"New country selection persists the correct Windows time zone");
     host.Remove(added);Check(JsonSerializer.Serialize(host.S.Rows)==beforePicker,"Isolated picker test restores its original cities and layout");host.Manager.Close();
     host.S.Language="zh-Hans";host.S.Life=true;host.S.Advice=true;host.S.Font=13;host.S.Twelve=true;
     var demo=host.Bars.Values.First();demo.R.City="洛杉矶";demo.R.Zone="Pacific Standard Time";demo.R.Code="US";demo.R.Width=620;host.SetLayer("normal");demo=host.Bars[demo.R.Id];
     demo.Refresh(DateTimeOffset.Parse("2026-10-22T11:26:00Z"));demo.UpdateLayout();
     lines.Add("INFO 04:26 preview: "+demo.PeriodText+" | "+demo.HintText+" | "+demo.ContactText);
     Check(demo.PeriodText=="凌晨"&&(demo.HintText.Contains("睡")||demo.HintText.Contains("周公"))&&demo.ContactText.Contains("09:00"),"04:26 preview visibly combines sleep estimate, wee-hours badge and next local contact time");
     var preview=new RenderTargetBitmap(620,50,96,96,PixelFormats.Pbgra32);preview.Render((Visual)demo.Content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(preview));using(var output=File.Create(Path.Combine(images,"friend-0426-zh-Hans.png")))encoder.Save(output);
     lines.Add("TOTAL "+count+" PASS; programmatic WPF integration, not physical tray interaction");
    }catch(Exception e){lines.Add(e.ToString());exit=1;}
    finally{File.WriteAllLines(report,lines);host.Quit();}
   }));
   app.Run();
  }catch(Exception e){lines.Add(e.ToString());File.WriteAllLines(report,lines);exit=1;host?.Tray.Dispose();host?.Disk.Dispose();}
  finally{if(Directory.Exists(temp))Directory.Delete(temp,true);}
  Environment.ExitCode=exit;
 }
 static void Render(Window window,string path){
  void RefreshVisual(DependencyObject node){if(node is UIElement element)element.InvalidateVisual();for(int i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)RefreshVisual(VisualTreeHelper.GetChild(node,i));}
  RefreshVisual(window);window.UpdateLayout();window.Dispatcher.Invoke(()=>{},DispatcherPriority.Render);
  var bitmap=new RenderTargetBitmap((int)window.ActualWidth,(int)window.ActualHeight,96,96,PixelFormats.Pbgra32);bitmap.Render(window);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var stream=File.Create(path);encoder.Save(stream);
 }
}
