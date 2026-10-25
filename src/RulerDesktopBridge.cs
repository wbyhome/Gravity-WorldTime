using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace WorldTime;
public sealed class RulerViewPreferences {
 public double X {get;set;}=120;public double Y {get;set;}=200;public double Width {get;set;}=720;
 public bool Pinned {get;set;}public bool Dates {get;set;}=true;public double BackgroundOpacity {get;set;}=.65;
 public bool Compact {get;set;}public bool Nature {get;set;}=true;public bool Desktop {get;set;}=true;
 public int Motion {get;set;}public bool FollowSystemMotion {get;set;}=true;public string SelfName {get;set;}="";
 public static bool Valid(RulerViewPreferences p)=>double.IsFinite(p.X)&&double.IsFinite(p.Y)&&Math.Abs(p.X)<100000&&Math.Abs(p.Y)<100000&&double.IsFinite(p.Width)&&p.Width>=200&&p.Width<=1100&&double.IsFinite(p.BackgroundOpacity)&&p.BackgroundOpacity>=0&&p.BackgroundOpacity<=1&&(!p.Pinned||p.Desktop)&&p.Motion is >=0 and <=2&&p.SelfName!=null&&p.SelfName.Length<=80&&!System.Linq.Enumerable.Any(p.SelfName,char.IsControl);
 public static RulerViewPreferences Load(string path,bool desktop){if(!File.Exists(path))return new(){Desktop=desktop};var p=JsonSerializer.Deserialize<RulerViewPreferences>(File.ReadAllText(path));return p!=null&&Valid(p)?p:throw new InvalidDataException("Invalid ruler view preferences; file preserved");}
 public void Save(string path){if(!Valid(this))throw new InvalidDataException("Invalid ruler view preferences");var temporary=path+".tmp";File.WriteAllText(temporary,JsonSerializer.Serialize(this,new JsonSerializerOptions{WriteIndented=true}));if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);}
}
// Desktop and explicitly pinned HWNDs are separate roles; no shell mutation or focus polling.
public sealed class RulerDesktopBridge : IDisposable {
 readonly RulerWindow ruler;readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromSeconds(2)};
 internal Func<IntPtr> HostProvider=WindowLayer.FindDesktopHost;
 IntPtr parent;DateTimeOffset retryAt;int failures;bool disposed;
 public RulerNativeSurface? Surface {get;private set;}
 public bool Visible {get;private set;}
 public bool Requested {get;private set;}
 public bool Pinned {get;private set;}
 public bool DesktopHosted=>Hosted&&!Pinned;
 public bool Hosted=>Surface?.Hosted==true&&Surface.Painted;
 public string BindingError {get;private set;}="";
 public int BindAttempts {get;private set;}
 public double Scale {get;private set;}=1;
 public Point Position {get;private set;}
 public RulerDesktopBridge(RulerWindow ruler,Point position){this.ruler=ruler;Position=position;timer.Tick+=(_,_)=>Maintain();}
 public void Show(bool desktop,bool pinned=false){Visible=true;Requested=desktop;Pinned=pinned;failures=0;retryAt=DateTimeOffset.MinValue;if(!desktop){timer.Stop();Drop();ruler.ShowFloating(false);return;}timer.Start();if(Surface?.Hosted==true&&Surface.Painted)Surface.Show(true);if(!TryBind())Fallback();}
 public void Hide(){Visible=false;timer.Stop();Surface?.Show(false);}
 public void Refresh(){if(Visible&&Hosted)Paint();}
 public void SetPosition(Point position){Position=position;Surface?.MoveTo(position);}
 public void Retry(){if(!Visible)return;failures=0;retryAt=DateTimeOffset.MinValue;if(Requested){timer.Start();if(!TryBind())Fallback();}}
 public void Maintain(){
  if(disposed||!Visible||!Requested)return;
  if(Hosted){if(Pinned)return;var current=HostProvider();if(current==IntPtr.Zero||current==parent)return;}
  if(failures>=4||DateTimeOffset.UtcNow<retryAt)return;
  if(!TryBind())Fallback();
 }
 bool TryBind(){
  BindAttempts++;RulerNativeSurface? replacement=null;
  try{
   var next=Pinned?IntPtr.Zero:HostProvider();if(!Pinned&&next==IntPtr.Zero)throw new InvalidOperationException("Explorer desktop host is not ready");
   var area=DockScreen.AtPoint(Position);Scale=area.Scale;ruler.FitNativeWidth((area.Right-area.Left)/Scale);var image=ruler.RenderNative(Scale);
   Position=Clamp(Position,image.PixelWidth,image.PixelHeight,area);replacement=new RulerNativeSurface(next,Pinned);
   replacement.CanResize=ruler.NativeCanResize;replacement.WidthChanged=(width,finished)=>ruler.SetRulerWidth(width,finished);replacement.CanDrag=ruler.NativeCanDrag;replacement.Clicked=ruler.NativeClick;replacement.ContextRequested=ruler.OpenNativeMenu;replacement.Hovered=ruler.NativeHover;replacement.PointerMoved=ruler.NativePointerMoved;replacement.PointerLeft=ruler.NativePointerLeft;
   replacement.PositionChanged=(bounds,finished)=>{Position=bounds.TopLeft;if(finished){var newArea=DockScreen.AtPoint(Position);Scale=newArea.Scale;ruler.FitNativeWidth((newArea.Right-newArea.Left)/Scale);Paint();ruler.SaveView("drag-release");}};
   replacement.Present(image,Position,Scale);var old=Surface;Surface=replacement;replacement=null;parent=next;old?.Dispose();BindingError="";failures=0;ruler.HideFloating();ruler.NativeModeChanged();return true;
  }catch(Exception e){replacement?.Dispose();BindingError=e.Message;failures++;retryAt=DateTimeOffset.UtcNow.AddSeconds(new[]{2,5,15,30}[Math.Min(failures-1,3)]);return false;}
 }
 void Fallback(){if(Hosted&&Surface?.Shown==true&&Surface.Pinned==Pinned){ruler.HideFloating();return;}Drop();ruler.ShowFloating(false);}
 void Paint(){try{var image=ruler.RenderNative(Scale);var area=DockScreen.AtPoint(Position);Position=Clamp(Position,image.PixelWidth,image.PixelHeight,area);Surface?.Present(image,Position,Scale);}catch(Exception e){BindingError=e.Message;Drop();retryAt=DateTimeOffset.UtcNow.AddSeconds(2);if(Visible)ruler.ShowFloating(false);}}
 public static Point Clamp(Point p,double width,double height,ScreenArea area)=>new(Math.Clamp(p.X,area.Left,Math.Max(area.Left,area.Right-width)),Math.Clamp(p.Y,area.Top,Math.Max(area.Top,area.Bottom-height)));
 void Drop(){Surface?.Dispose();Surface=null;parent=IntPtr.Zero;ruler.NativeModeChanged();}
 public void Dispose(){disposed=true;Visible=false;timer.Stop();Drop();}
}
