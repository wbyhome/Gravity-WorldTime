using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace WorldTime;
// Own synthetic parent windows; never changes Explorer or generates OS input.
public static class RulerNativeProbe {
 public static void Run(string[] args,bool interactive){
  string report=Path.GetFullPath(args.Contains("--report")?args[Array.IndexOf(args,"--report")+1]:"ruler-native-test.json");Directory.CreateDirectory(Path.GetDirectoryName(report)!);
  string temp=Path.Combine(Path.GetTempPath(),"WorldTime-023-"+Guid.NewGuid().ToString("N"));var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Host? host=null;Window? parent=null;var lines=new List<string>();int checks=0,exit=0,step=0;
  void Check(bool value,string label){if(!value)throw new Exception("FAIL "+label);checks++;lines.Add("PASS "+label);}
  try{
   var state=new State{AppearanceVersion=2,Language="zh-Hans"};foreach(string name in new[]{"纽约","伦敦","东京","悉尼"}){var c=Catalog.All.First(c=>c.Zh==name);state.Rows.Add(new Row{Id=c.En,City=c.Zh,Code=c.Code,Zone=c.Zone,Name=c.En});}using(var seed=new Store(temp))seed.Save(state);host=new Host(new Store(temp),true,true);
   var panel=new StackPanel{Background=new SolidColorBrush(Color.FromRgb(224,233,242))};var controls=new WrapPanel{Margin=new Thickness(10)};panel.Children.Add(controls);var status=new TextBlock{Margin=new Thickness(10,210,10,10),TextWrapping=TextWrapping.Wrap};panel.Children.Add(status);
   parent=new Window{Title="WorldTime 0.2.7 · native input check (synthetic host)",Left=80,Top=80,Width=1000,Height=800,Content=panel,ShowInTaskbar=true};parent.Show();var ruler=host.EnsureRuler();ruler.TransientOwner=parent;ruler.Desktop.HostProvider=()=>parent==null?IntPtr.Zero:new WindowInteropHelper(parent).Handle;ruler.Desktop.SetPosition(parent.PointToScreen(new Point(20,60)));ruler.Reveal();
   void Button(string label,Action action){var b=new Button{Content=label,Padding=new Thickness(8,4,8,4),Margin=new Thickness(0,0,8,0)};b.Click+=(_,_)=>action();controls.Children.Add(b);}
   Button("Hide ruler",ruler.Conceal);Button("Show ruler",ruler.Reveal);Button("Reset position",()=>ruler.Desktop.SetPosition(parent.PointToScreen(new Point(20,60))));Button("Finish",()=>host.Quit());if(interactive){Button("Pin / unpin",()=>ruler.SetPinned(!ruler.ActualPinned));bool dark=false;Button("Light / dark",()=>{dark=!dark;panel.Background=dark?new SolidColorBrush(Color.FromRgb(31,43,60)):new SolidColorBrush(Color.FromRgb(224,233,242));});}
   var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(interactive?500:200)};timer.Tick+=(_,_)=>{
    try{
     var surface=ruler.Desktop.Surface;var bounds=surface?.Bounds??Rect.Empty;var data=new{Interactive=interactive,SyntheticHost=true,Profile=temp,PhysicalWinDAccepted=false,Visible=ruler.RulerVisible,Hosted=ruler.Desktop.Hosted,ruler.Desktop.BindingError,ruler.Desktop.BindAttempts,ruler.HoveredDate,ruler.HighlightChangeCount,DateRails=ruler.PersonDates.Select(label=>new{label.Date,X=label.Bounds.X+label.Bounds.Width/2,Y=label.Bounds.Y+label.Bounds.Height/2}),Handle=surface?.Handle.ToInt64(),Bounds=bounds.IsEmpty?null:new{bounds.X,bounds.Y,bounds.Width,bounds.Height},surface?.Painted,surface?.PaintCount,surface?.PaintError,surface?.ClickCount,surface?.HoverCount,surface?.MenuCount,surface?.DragCount,surface?.LastInputError,Selected=ruler.SelectedId,DetailVisible=ruler.DetailWindow?.IsVisible==true};File.WriteAllText(report,JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));status.Text=$"Synthetic native host; desktop/Win+D acceptance is separate.\nHosted {ruler.Desktop.Hosted}; paint {surface?.PaintCount}; mouse click {surface?.ClickCount}; hover {surface?.HoverCount}; menu {surface?.MenuCount}; drag {surface?.DragCount}";
     if(interactive){if(++step>1200)host.Quit();return;}
     switch(step++){
      case 0:
       Check(surface?.Painted==true&&surface.Hosted,"Native child painted inside our synthetic parent");Check(!ruler.IsVisible,"Desktop mode does not show the WPF carrier window");
       Check(Math.Abs(bounds.X-ruler.Desktop.Position.X)<2&&Math.Abs(bounds.Y-ruler.Desktop.Position.Y)<2,"Native child actual screen origin matches requested screen pixels");
       var band=ruler.DragBand;int x=(int)((band.X+12)*ruler.Desktop.Scale),y=(int)((band.Y+8)*ruler.Desktop.Scale);Check(surface!.AlphaAt(x,y)>0&&RulerNativeSurface.HitAt(new Point(bounds.X+x,bounds.Y+y))==surface.Handle,"Opaque ruler pixels hit our native child");
       var image=ruler.RenderNative(ruler.Desktop.Scale);int blankX=2,blankY=3;Check(surface.AlphaAt(blankX,blankY)==0&&RulerNativeSurface.HitAt(new Point(bounds.X+blankX,bounds.Y+blankY))!=surface.Handle,"Alpha-zero blank area passes through native hit testing");var label=ruler.PersonDates[0];var datePoint=new Point(label.Bounds.X+label.Bounds.Width/2,ruler.DateTop+label.Bounds.Y+5);ruler.NativePointerMoved(datePoint);ruler.NativeHover(datePoint,new Point(bounds.X+datePoint.X,bounds.Y+datePoint.Y));var tip=app.Windows.OfType<RulerTipWindow>().Single();Check(tip.Passive,"Read-only tooltip has native no-activate and mouse-transparent styles");var tipPoint=tip.PointToScreen(new Point(5,5));Check(RulerNativeSurface.HitAt(tipPoint)!=new WindowInteropHelper(tip).Handle,"Opaque tooltip pixels do not intercept native hit testing");ruler.NativePointerLeft();ruler.SelectPerson(state.Rows[0].Id);break;
      case 1:
       var details=ruler.DetailWindow;ruler.Refresh(DateTimeOffset.UtcNow.AddMinutes(1),force:true);Check(ReferenceEquals(details,ruler.DetailWindow)&&details?.IsVisible==true,"Native minute update keeps details alive");ruler.Conceal();break;
      case 2:Check(!ruler.RulerVisible&&surface?.Shown!=true,"Conceal hides native surface");ruler.Reveal();break;
      case 3:Check(ruler.Desktop.Hosted&&ruler.Desktop.Surface?.Shown==true,"Reveal restores native rendering");ruler.Desktop.HostProvider=()=>IntPtr.Zero;var existingHandle=ruler.Desktop.Surface!.Handle;ruler.Reveal();Check(ruler.Desktop.Surface?.Handle==existingHandle&&!ruler.IsVisible,"Failed rebind keeps one healthy old native renderer, not a duplicate floating layer");ruler.Desktop.Surface!.Dispose();ruler.Desktop.Retry();break;
      case 4:Check(!ruler.Desktop.Hosted&&ruler.IsVisible&&!ruler.Topmost,"Unavailable desktop safely falls back to non-topmost transparent ruler");int attempts=ruler.Desktop.BindAttempts;for(int i=0;i<100;i++)ruler.Desktop.Maintain();Check(ruler.Desktop.BindAttempts==attempts,"Repeated maintain calls respect retry backoff");ruler.Desktop.HostProvider=()=>new WindowInteropHelper(parent).Handle;ruler.Desktop.Retry();break;
      case 5:Check(ruler.Desktop.Hosted&&!ruler.IsVisible,"Explicit retry returns to one native renderer");var old=parent;parent=new Window{Title="WorldTime isolated replacement host",Left=100,Top=140,Width=840,Height=400,Content=new Canvas()};parent.Show();ruler.TransientOwner=parent;old.Close();Check(!ruler.Desktop.Surface!.Alive,"Destroying own parent destroys native child without stale ownership");ruler.Desktop.Maintain();break;
      case 6:Check(ruler.Desktop.Hosted&&ruler.Desktop.Surface?.Painted==true,"Native renderer recovers to replacement own parent");ruler.SetDesktop(false);break;
      case 7:Check(ruler.IsVisible&&ruler.Desktop.Surface==null&&!ruler.Topmost,"Floating mode destroys native renderer and keeps a non-topmost ruler");ruler.SetPinned(true);break;
      case 8:Check(ruler.ActualPinned&&ruler.Desktop.Surface?.Pinned==true&&!ruler.IsVisible,"Pinned mode uses an independent native popup without WPF shell");timer.Stop();app.Dispatcher.BeginInvoke(new Action(()=>{try{Check(ruler.ModalBusy&&!ruler.NativeCanDrag(new Point(ruler.DragBand.X+19,ruler.DragBand.Y+20)),"Modal dialog blocks native handle drag");ruler.OpenNativeMenu(new Point(bounds.X+30,bounds.Y+ruler.ScaleTop+20));Check(ruler.NativeMenu==null&&ruler.ActualPinned,"Modal dialog blocks native display-mode menu");}catch(Exception e){exit=1;lines.Add(e.ToString());}finally{foreach(var dialog in app.Windows.OfType<RulerAppearanceDialog>().ToArray())dialog.Close();}}));ruler.OpenAppearance();if(exit!=0)throw new Exception("Modal native input checks failed");Check(!ruler.ModalBusy&&ruler.NativeCanDrag(new Point(ruler.DragBand.X+19,ruler.DragBand.Y+20)),"Closing modal restores native handle input");timer.Start();ruler.SetPinned(false);break;
      case 9:Check(ruler.Desktop.DesktopHosted&&!ruler.ActualPinned,"Unpinning returns to actual native desktop parenting");timer.Stop();lines.Add($"TOTAL {checks} PASS; native own-window tests, no physical input or Explorer restart/Win+D certification");File.WriteAllLines(report+".txt",lines);host.Quit();break;
     }
    }catch(Exception e){exit=1;lines.Add(e.ToString());File.WriteAllLines(report+".txt",lines);timer.Stop();host.Exiting=true;ruler.Desktop.Dispose();host.Tray.Dispose();host.Disk.Dispose();app.Shutdown();}
   };timer.Start();app.Run();
  }catch(Exception e){exit=1;lines.Add(e.ToString());File.WriteAllLines(report+".txt",lines);host?.Tray.Dispose();host?.Disk.Dispose();}
  finally{if(Directory.Exists(temp)&&temp.StartsWith(Path.Combine(Path.GetTempPath(),"WorldTime-023-"),StringComparison.OrdinalIgnoreCase))try{Directory.Delete(temp,true);}catch{}Environment.ExitCode=exit;}
 }
}
