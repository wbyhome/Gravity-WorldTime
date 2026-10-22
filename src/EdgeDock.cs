using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace WorldTime;
public class DockedGroup {
 public string Group {get;set;}="";
 public string Edge {get;set;}="right";
 public string Monitor {get;set;}="";
 public double Offset {get;set;}=.5;
}
public record ScreenArea(string Device,int Left,int Top,int Right,int Bottom,double Scale);
public static class DockGeometry {
 public static readonly string[] Edges={"left","right","top","bottom"};
 public static string? Contact(ScreenArea area,Rect clocks,double thresholdDip=14){
  if(clocks.Right<area.Left||clocks.Left>area.Right||clocks.Bottom<area.Top||clocks.Top>area.Bottom)return null;
  var distances=new[]{clocks.Left-area.Left,area.Right-clocks.Right,clocks.Top-area.Top,area.Bottom-clocks.Bottom};
  int best=Array.IndexOf(distances,distances.Min());
  return distances[best]<=thresholdDip*area.Scale?Edges[best]:null;
 }
 public static string Nearest(ScreenArea area,Rect clocks){
  var distances=new[]{Math.Abs(clocks.Left-area.Left),Math.Abs(area.Right-clocks.Right),Math.Abs(clocks.Top-area.Top),Math.Abs(area.Bottom-clocks.Bottom)};
  return Edges[Array.IndexOf(distances,distances.Min())];
 }
 public static double Anchor(ScreenArea area,Rect clocks,string edge){
  bool side=edge=="left"||edge=="right";
  double position=side?clocks.Top+clocks.Height/2:clocks.Left+clocks.Width/2;
  return Math.Clamp((position-(side?area.Top:area.Left))/Math.Max(1,side?area.Bottom-area.Top:area.Right-area.Left),0,1);
 }
 public static Rect Tab(ScreenArea area,string edge,double offset){
  bool side=edge=="left"||edge=="right";double w=(side?28:100)*area.Scale,h=(side?84:28)*area.Scale;
  double x=side?(edge=="left"?area.Left:area.Right-w):Math.Clamp(area.Left+(area.Right-area.Left)*offset-w/2,area.Left,Math.Max(area.Left,area.Right-w));
  double y=side?Math.Clamp(area.Top+(area.Bottom-area.Top)*offset-h/2,area.Top,Math.Max(area.Top,area.Bottom-h)):(edge=="top"?area.Top:area.Bottom-h);
  return new Rect(x,y,w,h);
 }
}
public static class DockScreen {
 [StructLayout(LayoutKind.Sequential)]struct NativeRect{public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct MonitorInfo{public int Size;public NativeRect Bounds,Work;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)]public string Device;}
 delegate bool MonitorCallback(IntPtr monitor,IntPtr dc,IntPtr rect,IntPtr data);
 [DllImport("user32.dll")]static extern IntPtr MonitorFromWindow(IntPtr hwnd,uint flags);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorInfo info);
 [DllImport("user32.dll")]static extern bool EnumDisplayMonitors(IntPtr dc,IntPtr clip,MonitorCallback callback,IntPtr data);
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr hwnd,out NativeRect rect);
 [DllImport("shcore.dll")]static extern int GetDpiForMonitor(IntPtr monitor,int type,out uint x,out uint y);
 [DllImport("user32.dll",SetLastError=true)]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
 static ScreenArea Read(IntPtr monitor){var info=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>(),Device=""};if(!GetMonitorInfo(monitor,ref info))throw new InvalidOperationException("Cannot locate screen work area");double scale=1;if(GetDpiForMonitor(monitor,0,out uint x,out _)==0)scale=x/96d;return new(info.Device,info.Work.Left,info.Work.Top,info.Work.Right,info.Work.Bottom,scale);}
 static IntPtr Handle(Window window)=>window is Bar bar?bar.Layer.NativeHandle:new WindowInteropHelper(window).Handle;
 public static ScreenArea For(Window window)=>Read(MonitorFromWindow(Handle(window),2));
 public static ScreenArea Find(string device){ScreenArea? found=null;EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,(m,_,_,_)=>{var area=Read(m);if(area.Device==device)found=area;return true;},IntPtr.Zero);return found??Read(MonitorFromWindow(IntPtr.Zero,1));}
 public static Rect Bounds(Window window){if(!GetWindowRect(Handle(window),out var r))throw new InvalidOperationException("Cannot read clock bounds");return new Rect(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top);}
 public static void Place(Window window,Rect bounds){if(!SetWindowPos(new WindowInteropHelper(window).Handle,IntPtr.Zero,(int)Math.Round(bounds.X),(int)Math.Round(bounds.Y),(int)Math.Round(bounds.Width),(int)Math.Round(bounds.Height),0x14))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
}
public sealed class EdgeTab : Window {
 readonly Host host;public readonly DockedGroup Dock;
 readonly TextBlock label=new(){Foreground=Brushes.White,FontWeight=FontWeights.SemiBold,TextAlignment=TextAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
 public EdgeTab(Host host,DockedGroup dock){this.host=host;Dock=dock;Title="WorldTime edge tab";ShowInTaskbar=false;ShowActivated=false;Topmost=true;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;
  var button=new Button{Content=label,Padding=new Thickness(0),Background=new SolidColorBrush(Color.FromArgb(240,32,52,72)),BorderBrush=Brushes.SlateGray,BorderThickness=new Thickness(1)};Content=button;button.Click+=(_,_)=>host.ExpandGroup(Dock.Group);
  ContextMenuOpening+=(_,_)=>{var menu=new ContextMenu();void Item(string key,Action action){var item=new MenuItem{Header=host.T(key)};item.Click+=(_,_)=>action();menu.Items.Add(item);}Item("Expand clocks",()=>host.ExpandGroup(Dock.Group));foreach(string edge in DockGeometry.Edges){string captured=edge;Item("Dock "+captured,()=>{Dock.Edge=captured;host.Sync();host.Save();});}Item("Manage cities",()=>host.Manager.Reveal());ContextMenu=menu;};ContextMenu=new ContextMenu();
  Loaded+=(_,_)=>Refresh();
 }
 public Rect DesiredBounds=>DockGeometry.Tab(DockScreen.Find(Dock.Monitor),Dock.Edge,Dock.Offset);
 public void Refresh(){
  var rows=host.S.Rows.Where(r=>r.Group==Dock.Group).OrderBy(r=>r.Order).ToList();
  string arrow=Dock.Edge=="left"?"›":Dock.Edge=="right"?"‹":Dock.Edge=="top"?"⌄":"⌃";
  label.Text=Dock.Edge is "left" or "right"?$"◷\n{rows.Count}\n{arrow}":$"◷  {rows.Count}  {arrow}";
  ToolTip=host.T("Click to expand clocks")+"\n"+string.Join(" · ",rows.Select(r=>L10n.CityName(r,host.S.Language)));
  if(IsLoaded)DockScreen.Place(this,DesiredBounds);
 }
}
