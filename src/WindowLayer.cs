using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace WorldTime;

// Attach only our small HWNDs to the existing Explorer desktop band. Never
// subclass, inject, resize, disable, or reorder Explorer/Gravity windows.
public sealed class WindowLayer {
 public static Action<string>? Diagnostics;
 readonly Window window;
 HwndSource? source;
 string mode;
 IntPtr handle,desktop;
 bool binding;
 DesktopSurface? surface;
 const long Child=0x40000000L,Popup=0x80000000L;
 const uint NoSize=1,NoMove=2,NoZOrder=4,NoActivate=0x10,FrameChanged=0x20,NoOwnerZOrder=0x200;
 public IntPtr NativeHandle=>surface?.Handle??handle;
 public DesktopSurface? Surface=>surface;
 public bool IsDesktopHosted=>NativeHandle!=IntPtr.Zero&&IsWindow(NativeHandle)&&desktop!=IntPtr.Zero&&GetParent(NativeHandle)==desktop;
 public string? BindingError {get;private set;}
 public WindowLayer(Window window,string initial){
  this.window=window;mode=initial;
  window.SourceInitialized+=(_,_)=>{
   handle=new WindowInteropHelper(window).Handle;
   source=HwndSource.FromHwnd(handle);source?.AddHook(Hook);
   long ex=GetWindowLongPtr(handle,-20).ToInt64();SetStyle(-20,(ex|0x80L)&~0x40000L);
  };
  window.Loaded+=(_,_)=>Set(mode);
  window.IsVisibleChanged+=(_,_)=>surface?.Show(window.IsVisible);
  window.Closed+=(_,_)=>{surface?.Dispose();surface=null;source?.RemoveHook(Hook);source=null;handle=IntPtr.Zero;desktop=IntPtr.Zero;};
 }
 public void Set(string value){
  Diagnostics?.Invoke("Set "+value+" hwnd="+handle);
  if(value is not ("normal" or "top" or "bottom"))throw new ArgumentException("Unknown layer");
  mode=value;
  if(handle==IntPtr.Zero||!IsWindow(handle))return;
  binding=true;
  try{
   if(mode=="top"){
    Detach();window.Topmost=true;
    Position(new IntPtr(-1),0,0,0,0,NoMove|NoSize|NoActivate|NoOwnerZOrder);BindingError=null;
   }else{window.Topmost=false;Attach();}
  }finally{binding=false;Diagnostics?.Invoke("Set complete parent="+GetParent(handle));}
 }
 void Attach(){
  if(IsDesktopHosted){BindingError=null;return;}
  IntPtr parent=FindDesktopHost();
  if(parent==IntPtr.Zero){BindingError="Explorer desktop host is not ready.";return;}
  if(window is Bar bar){
   // Keep WPF's compositor away from the Explorer window tree. Its HWND is
   // message-only; a separately created native child presents the bitmap.
   SetStyle(-16,(GetWindowLongPtr(handle,-16).ToInt64()&~Popup)|Child);
   SetParent(handle,new IntPtr(-3));
   surface?.Dispose();surface=new DesktopSurface(bar,parent);desktop=parent;
   surface.Show(window.IsVisible);Place(bar.R.X,bar.R.Y,bar.R.Width,50);BindingError=null;return;
  }
  if(!GetWindowRect(handle,out var bounds))throw new Win32Exception(Marshal.GetLastWin32Error());
  long old=GetWindowLongPtr(handle,-16).ToInt64();SetStyle(-16,(old&~Popup)|Child);
  Diagnostics?.Invoke("Attaching to "+parent);SetLastError(0);SetParent(handle,parent);Diagnostics?.Invoke("SetParent returned");
  if(GetParent(handle)!=parent){SetStyle(-16,old);BindingError="Desktop attachment was rejected.";return;}
  desktop=parent;
  var point=new NativePoint{X=bounds.Left,Y=bounds.Top};
  if(!ScreenToClient(desktop,ref point))throw new Win32Exception(Marshal.GetLastWin32Error());
  Position(IntPtr.Zero,point.X,point.Y,bounds.Right-bounds.Left,bounds.Bottom-bounds.Top,NoActivate|NoOwnerZOrder|FrameChanged);BindingError=null;
 }
 void Detach(){
  if(surface!=null){surface.Dispose();surface=null;desktop=IntPtr.Zero;SetParent(handle,IntPtr.Zero);SetStyle(-16,(GetWindowLongPtr(handle,-16).ToInt64()&~Child)|Popup);SetStyle(-8,0);return;}
  if(!IsDesktopHosted){desktop=IntPtr.Zero;return;}
  GetWindowRect(handle,out var bounds);SetLastError(0);SetParent(handle,IntPtr.Zero);
  SetStyle(-16,(GetWindowLongPtr(handle,-16).ToInt64()&~Child)|Popup);
  SetStyle(-8,0);
  if(GetParent(handle)!=IntPtr.Zero)throw new InvalidOperationException("Could not detach the clock from desktop mode.");
  desktop=IntPtr.Zero;
  Position(new IntPtr(-2),bounds.Left,bounds.Top,bounds.Right-bounds.Left,bounds.Bottom-bounds.Top,NoActivate|NoOwnerZOrder|FrameChanged);
 }
 public void Maintain(){if(mode!="top"&&!IsDesktopHosted&&handle!=IntPtr.Zero&&IsWindow(handle))Set(mode);}
 public void Invalidate(){if(surface!=null)window.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render,new Action(()=>surface?.Present()));}
 public void Place(double x,double y,double width,double height){
  if(!IsDesktopHosted)return;
  var dpi=VisualTreeHelper.GetDpi(window);
  if(surface!=null){surface.Move((int)Math.Round(x*dpi.DpiScaleX),(int)Math.Round(y*dpi.DpiScaleY),(int)Math.Round(width*dpi.DpiScaleX),(int)Math.Round(height*dpi.DpiScaleY),dpi.DpiScaleX);return;}
  var point=new NativePoint{X=(int)Math.Round(x*dpi.DpiScaleX),Y=(int)Math.Round(y*dpi.DpiScaleY)};
  if(!ScreenToClient(desktop,ref point))return;
  Position(IntPtr.Zero,point.X,point.Y,(int)Math.Round(width*dpi.DpiScaleX),(int)Math.Round(height*dpi.DpiScaleY),NoActivate|NoOwnerZOrder);
 }
 public void RestoreNoActivate(){if(handle==IntPtr.Zero)return;if(surface!=null){surface.Show(true);return;}ShowWindow(handle,4);Set(mode);}
 void SetStyle(int index,long value){
  SetLastError(0);var old=SetWindowLongPtr(handle,index,new IntPtr(value));int error=Marshal.GetLastWin32Error();
  // A synchronous WPF style/owner callback can leave LastError set even
  // after an idempotent zero->zero change. Verify the actual postcondition.
  if(old==IntPtr.Zero&&error!=0&&(!IsWindow(handle)||GetWindowLongPtr(handle,index).ToInt64()!=value))throw new Win32Exception(error);
 }
 void Position(IntPtr after,int x,int y,int width,int height,uint flags){if(!SetWindowPos(handle,after,x,y,width,height,flags))throw new Win32Exception(Marshal.GetLastWin32Error());}
 IntPtr Hook(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled){
  if(message==0x46&&!binding&&lParam!=IntPtr.Zero){
   var pos=Marshal.PtrToStructure<PositionData>(lParam);
   if(mode=="top"||(surface==null&&IsDesktopHosted)){pos.After=mode=="top"?new IntPtr(-1):IntPtr.Zero;pos.Flags=(pos.Flags&~NoZOrder)|NoActivate|NoOwnerZOrder;Marshal.StructureToPtr(pos,lParam,false);}
  }
  return IntPtr.Zero;
 }
 public static IntPtr FindDesktopHost(){
  IntPtr found=IntPtr.Zero;
  EnumWindows((hwnd,_)=>{if(FindWindowEx(hwnd,IntPtr.Zero,"SHELLDLL_DefView",null)!=IntPtr.Zero){found=hwnd;return false;}return true;},IntPtr.Zero);
  return found;
 }
 [StructLayout(LayoutKind.Sequential)]struct NativePoint{public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]struct NativeRect{public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct PositionData{public IntPtr Window,After;public int X,Y,Width,Height;public uint Flags;}
 delegate bool WindowCallback(IntPtr hwnd,IntPtr data);
 [DllImport("user32.dll")]static extern bool EnumWindows(WindowCallback callback,IntPtr data);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr FindWindowEx(IntPtr parent,IntPtr after,string className,string? title);
 [DllImport("user32.dll")]static extern IntPtr GetParent(IntPtr hwnd);
 [DllImport("user32.dll")]static extern bool IsWindow(IntPtr hwnd);
 [DllImport("user32.dll",SetLastError=true)]static extern bool GetWindowRect(IntPtr hwnd,out NativeRect rect);
 [DllImport("user32.dll",SetLastError=true)]static extern bool ScreenToClient(IntPtr hwnd,ref NativePoint point);
 [DllImport("user32.dll",SetLastError=true)]static extern IntPtr SetParent(IntPtr child,IntPtr parent);
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW",SetLastError=true)]static extern IntPtr GetWindowLongPtr(IntPtr hwnd,int index);
 [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)]static extern IntPtr SetWindowLongPtr(IntPtr hwnd,int index,IntPtr value);
 [DllImport("kernel32.dll")]static extern void SetLastError(uint error);
 [DllImport("user32.dll",SetLastError=true)]static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
 [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr hwnd,int command);
}
