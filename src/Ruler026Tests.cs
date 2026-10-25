using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WorldTime;
public static class Ruler026Tests {
 public static void Run(string[] args){
  string report=Path.GetFullPath(args.Contains("--report")?args[Array.IndexOf(args,"--report")+1]:"ruler026-test.txt"),media=Path.Combine(Path.GetDirectoryName(report)!,"media"),temp=Path.Combine(Path.GetTempPath(),"WorldTime-026-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(media);
  var lines=new List<string>();int count=0,exit=0;Host? host=null;var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
  void Check(bool value,string label){if(!value)throw new Exception("FAIL "+label);lines.Add("PASS "+label);count++;}
  try{
   var state=new State{Language="zh-Hans",AppearanceVersion=2};foreach(string name in new[]{"纽约","伦敦","东京","悉尼"}){var c=Catalog.All.First(c=>c.Zh==name);state.Rows.Add(new Row{Id=c.En,Name=c.En,City=c.Zh,Code=c.Code,Zone=c.Zone});}using(var seed=new Store(temp))seed.Save(state);host=new Host(new Store(temp),true);
   app.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>{
    try{
     host.ShowRuler();var w=host.Ruler!;var at=DateTimeOffset.Parse("2026-10-24T00:15:00Z");var viewer=TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");string original=JsonSerializer.Serialize(host.S);
     void Save(BitmapSource image,string name){var enc=new PngBitmapEncoder();enc.Frames.Add(BitmapFrame.Create(image));using(var output=File.Create(Path.Combine(media,name+".png")))enc.Save(output);}
     void Picture(string name){w.Refresh(at,viewer,true);Save(w.RenderNative(1),name);}
     byte Alpha(BitmapSource image,double x,double y,double dpi=1){var pixel=new byte[4];image.CopyPixels(new Int32Rect((int)Math.Floor(x*dpi),(int)Math.Floor(y*dpi),1,1),pixel,4,0);return pixel[3];}
     IEnumerable<DependencyObject> Descendants(DependencyObject d){for(int i=0;i<VisualTreeHelper.GetChildrenCount(d);i++){var c=VisualTreeHelper.GetChild(d,i);yield return c;foreach(var child in Descendants(c))yield return child;}}
     void Composite(string name,Color background){var image=w.RenderNative(1);var draw=new DrawingVisual();using(var dc=draw.RenderOpen()){dc.DrawRectangle(new SolidColorBrush(background),null,new Rect(0,0,image.PixelWidth,image.PixelHeight));dc.DrawImage(image,new Rect(0,0,image.PixelWidth,image.PixelHeight));}var result=new RenderTargetBitmap(image.PixelWidth,image.PixelHeight,96,96,PixelFormats.Pbgra32);result.Render(draw);Save(result,name);}
     void Zoom(string name,Rect area){var draw=new DrawingVisual();using(var dc=draw.RenderOpen()){dc.PushTransform(new ScaleTransform(4,4));dc.DrawRectangle(new VisualBrush(w.Visual){ViewboxUnits=BrushMappingMode.Absolute,Viewbox=area,Stretch=Stretch.Fill},null,new Rect(0,0,area.Width,area.Height));}var bitmap=new RenderTargetBitmap((int)Math.Ceiling(area.Width*4),(int)Math.Ceiling(area.Height*4),96,96,PixelFormats.Pbgra32);bitmap.Render(draw);Save(bitmap,name);}
     Picture("026-standard");
     Check(w.Placements.All(p=>Math.Abs(p.X-p.Person.Fraction*w.TrackWidth)<.0001),"All minute anchors retained after endpoint changes");
     Check(w.DateGroups.Sum(g=>g.Members.Count)==w.Placements.Count&&w.DateRails.Count==2,"Two-date membership and all contacts remain intact");
     Check(!Descendants(w.Visual).OfType<Button>().Any(b=>AutomationProperties.GetName(b)==host.T("Pin on top")||AutomationProperties.GetName(b)==host.T("Ruler on desktop")),"No permanent pin button remains in ruler visual tree");
     var body=w.BodyBounds;var grip=new Point(w.DragBand.X+w.DragBand.Width/2,w.DragBand.Y+20);var image=w.RenderNative(1);
     Check(Enumerable.Range((int)body.Left+1,(int)body.Width-2).All(x=>Alpha(image,x,body.Y+20)>0),"Body has one continuous painted middle row, without the former gap");
     Check(Alpha(image,body.Left,body.Top)==0&&Alpha(image,body.Right-1,body.Top)==0&&Alpha(image,body.Left,body.Bottom-1)==0&&Alpha(image,body.Right-1,body.Bottom-1)==0,"All four outer rounded corners have zero alpha");
     Check(w.NativeCanDrag(grip)&&w.NativeCanDrag(new Point(grip.X,body.Y+7)),"Integrated left end has a full-height usable grip interior");
     Check(!w.NativeCanDrag(body.TopLeft)&&!w.NativeCanDrag(new Point(body.Right-5,body.Y+20))&&!w.NativeCanDrag(new Point(w.CoreOrigin,body.Y+20)),"Rounded corner, right cap and timeline cannot start a drag");
     w.NativePointerMoved(grip);Check(w.HandleHovered,"Entering left end strengthens grip");Save(w.RenderNative(1),"026-handle-hover");
     Zoom("026-left-hover-4x",new Rect(body.X-2,body.Y-2,70,44));
     int transitions=w.HandleHighlightChangeCount,builds=w.RenderCount,windows=app.Windows.Count;string[] paths={"worldtime.json","ruler-view.json","ruler-view-writes.jsonl"};var before=paths.Select(p=>RulerViewAudit.Hash(Path.Combine(temp,p))).ToArray();var timer=Stopwatch.StartNew();for(int i=0;i<10000;i++)w.NativePointerMoved(grip);timer.Stop();
     Check(w.HandleHighlightChangeCount==transitions&&w.RenderCount==builds&&app.Windows.Count==windows,"10000 stationary grip events create no scene rebuild, highlight toggle or new window");lines.Add("INFO 10000 grip events ms: "+timer.Elapsed.TotalMilliseconds.ToString("F2"));
     Check(paths.Select(p=>RulerViewAudit.Hash(Path.Combine(temp,p))).SequenceEqual(before),"Grip hover writes no contacts, view settings or audit files");
     w.NativePointerLeft();Check(!w.HandleHovered,"Leaving left end restores quiet grip");Save(w.RenderNative(1),"026-handle-default");Zoom("026-left-default-4x",new Rect(body.X-2,body.Y-2,70,44));Zoom("026-right-4x",new Rect(body.Right-68,body.Y-2,70,44));
     w.NativeClick(grip,new Point(0,0));Check(!w.ActualPinned&&w.DetailWindow==null,"Clicking former pin area no longer changes mode or opens a contact");
     var rail=w.DateRails.Last();w.NativePointerMoved(new Point(rail.LabelBounds.X+rail.LabelBounds.Width/2,w.DateTop+rail.LabelBounds.Y+8));Check(w.HoveredDate==rail.Group.Date&&!w.HandleHovered&&w.DateHighlightedIds.Count==rail.Group.Members.Count,"Date hover retains exact members independently of grip hover");Save(w.RenderNative(1),"026-date-hover");w.NativePointerLeft();
     foreach(double opacity in new[]{.65,.30,0d,1d}){w.SetBackgroundOpacity(opacity,false);Picture("026-opacity-"+(int)(opacity*100));var bmp=w.RenderNative(1);Check(Alpha(bmp,body.Left,body.Top)==0&&Alpha(bmp,body.Right-1,body.Bottom-1)==0,"Rounded corners remain transparent at background opacity "+opacity);if(opacity>0)Check(Alpha(bmp,grip.X,body.Y+20)>0,"Grip interior remains painted at background opacity "+opacity);}
     w.SetBackgroundOpacity(.65,false);Picture("026-standard");Composite("026-light",Color.FromRgb(239,244,249));Composite("026-dark",Color.FromRgb(24,36,54));
     foreach(double dpi in new[]{1d,1.25,1.5,2d}){var bmp=w.RenderNative(dpi);Check(Alpha(bmp,body.Left,body.Top,dpi)==0&&Alpha(bmp,grip.X,grip.Y,dpi)>0,"Rendered corner and grip alpha align at simulated scale "+dpi);Save(bmp,"026-scale-"+dpi.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture));}
     foreach(string lang in new[]{"en","zh-Hans","zh-Hant","ja"}){host.S.Language=lang;Picture("026-"+lang);Check(!host.T("Drag the left end to move. Right-click to switch desktop or always-on-top mode.").Contains("Click the pin"),"Updated handle instructions: "+lang);}
     host.S.Language="zh-Hans";Picture("026-standard");
     // Real own-window mode transitions, not Explorer acceptance or OS input injection.
     var parent=new Window{Title="WorldTime 0.2.6 isolated menu test",Width=900,Height=400,Left=30,Top=30,Content=new Canvas()};parent.Show();w.Desktop.HostProvider=()=>new WindowInteropHelper(parent).Handle;w.SetPinned(false);
     MenuItem PinItem(){w.OpenNativeMenu(new Point(w.Desktop.Position.X+80,w.Desktop.Position.Y+w.ScaleTop+20));return w.NativeMenu!.Items.OfType<MenuItem>().Single(m=>Equals(m.Header,host.T("Pin on top")));}
     var pin=PinItem();Check(pin.IsCheckable&&!pin.IsChecked&&w.Desktop.DesktopHosted,"Menu initially reports desktop mode with unchecked always-on-top");pin.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Check(w.ActualPinned&&w.Desktop.Surface?.Pinned==true,"Always-on-top menu action retains native popup transition");pin=PinItem();Check(pin.IsChecked,"Reopened menu reflects pinned state");pin.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));Check(!w.ActualPinned&&w.Desktop.DesktopHosted,"Selecting checked menu item returns to desktop child");
     Check(JsonSerializer.Serialize(host.S)==original,"Visual and mode controls preserve complete contact and routine state");w.Desktop.Dispose();parent.Close();
    }catch(Exception e){exit=1;lines.Add(e.ToString());}finally{host.Exiting=true;host.Tray.Dispose();host.Disk.Dispose();app.Shutdown();}
   }));app.Run();
  }catch(Exception e){exit=1;lines.Add(e.ToString());host?.Tray.Dispose();host?.Disk.Dispose();}
  finally{if(Directory.Exists(temp)&&Path.GetFullPath(temp).StartsWith(Path.Combine(Path.GetTempPath(),"WorldTime-026-"),StringComparison.OrdinalIgnoreCase))try{Directory.Delete(temp,true);}catch{}lines.Add($"TOTAL {count} PASS; real-control rendering/programmatic own-window checks, not physical Explorer acceptance");File.WriteAllLines(report,lines);Environment.ExitCode=exit;}
 }
}
