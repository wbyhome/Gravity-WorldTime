using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WorldTime;
// Separate from the classic Bar/DesktopSurface input path. Own HWNDs only.
public sealed class RulerNativeSurface : IDisposable {
 static readonly Dictionary<IntPtr,RulerNativeSurface> windows=new();
 static readonly Proc route=Route;static ushort atom;
 readonly uint parentPid;readonly IntPtr parent;readonly bool pinned;
 readonly DispatcherTimer dragTimer=new(){Interval=TimeSpan.FromMilliseconds(16)};
 byte[] pixels=Array.Empty<byte>();int width,height;double dpi=1,downDpi;bool pressed,dragging,resizing,moved;Point down;Rect start;
 public IntPtr Handle {get;private set;}
 public bool Alive=>Handle!=IntPtr.Zero&&IsWindow(Handle)&&Owner(Handle)==(uint)Environment.ProcessId;
 public bool Hosted=>Alive&&(pinned?GetParent(Handle)==IntPtr.Zero:IsWindow(parent)&&Owner(parent)==parentPid&&GetParent(Handle)==parent);
 public bool Pinned=>pinned;
 public bool Shown=>Hosted&&IsWindowVisible(Handle);
 public bool Painted {get;private set;}
 public int PaintCount {get;private set;}
 public int PaintError {get;private set;}
 public int ClickCount {get;private set;}public int HoverCount {get;private set;}public int MenuCount {get;private set;}public int DragCount {get;private set;}
 public int ResizeCount {get;private set;}
 public int VisibilityCalls {get;private set;}public int ZOrderCalls {get;private set;}
 public static int ActiveCount=>windows.Count;
 public bool Interacting=>pressed;public bool RadialResize {get;set;}
 public string LastInputError {get;private set;}="";
 public Rect Bounds {get{if(!Alive||!GetWindowRect(Handle,out var r))return Rect.Empty;return new(r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top);}}
 public Func<Point,bool>? CanDrag;
 public Func<Point,bool>? CanResize;public Action<double,bool>? WidthChanged;
 public Action<Point,Point>? Clicked,Hovered;
 public Action<Point>? ContextRequested,PointerMoved;
 public Action? PointerLeft;
 public Action<Rect,bool>? PositionChanged;
 public RulerNativeSurface(IntPtr parent,bool pinned=false){
  if(!pinned&&(parent==IntPtr.Zero||!IsWindow(parent)))throw new InvalidOperationException("Desktop host unavailable");this.parent=pinned?IntPtr.Zero:parent;this.pinned=pinned;parentPid=pinned?0:Owner(parent);
  if(atom==0){var c=new ClassInfo{Size=(uint)Marshal.SizeOf<ClassInfo>(),Procedure=route,Instance=GetModuleHandle(null),Cursor=LoadCursor(IntPtr.Zero,new IntPtr(32512)),Name="Gravity.WorldTime.RulerSurface"};atom=RegisterClassEx(ref c);if(atom==0)throw new Win32Exception(Marshal.GetLastWin32Error());}
  Handle=CreateWindowEx((uint)(0x80000|0x80|0x08000000|(pinned?8:0)),"Gravity.WorldTime.RulerSurface","Gravity WorldTime 0.3.1 Surface",pinned?0x80000000:0x40000000,0,0,1,1,this.parent,IntPtr.Zero,GetModuleHandle(null),IntPtr.Zero);
  if(Handle==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());windows.Add(Handle,this);dragTimer.Tick+=(_,_)=>{if(!Alive||!pressed||!dragging&&!resizing){dragTimer.Stop();return;}if((GetAsyncKeyState(1)&0x8000)==0){FinishDrag();return;}ContinueDrag();};
 }
 public void Present(BitmapSource image,Point position,double scale){
  if(!Hosted)throw new InvalidOperationException("Desktop host changed");if(!double.IsFinite(scale)||scale<.5||scale>8)throw new ArgumentOutOfRangeException(nameof(scale));
  bool wasPainted=Painted;dpi=scale;width=image.PixelWidth;height=image.PixelHeight;pixels=new byte[checked(width*height*4)];image.CopyPixels(pixels,width*4,0);
  IntPtr dc=IntPtr.Zero,bitmap=IntPtr.Zero,old=IntPtr.Zero;
  try{dc=CreateCompatibleDC(IntPtr.Zero);if(dc==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());var info=new BitmapInfo{Size=40,Width=width,Height=-height,Planes=1,Bits=32};bitmap=CreateDIBSection(dc,ref info,0,out var bits,IntPtr.Zero,0);if(bitmap==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());old=SelectObject(dc,bitmap);Marshal.Copy(pixels,0,bits,pixels.Length);
   var location=new PointI{X=(int)Math.Round(position.X),Y=(int)Math.Round(position.Y)};if(!pinned&&!ScreenToClient(parent,ref location))throw new Win32Exception(Marshal.GetLastWin32Error());var zero=new PointI();var size=new SizeI{X=width,Y=height};var blend=new Blend{Alpha=255,Format=1};Painted=UpdateLayeredWindow(Handle,IntPtr.Zero,ref location,ref size,dc,ref zero,0,ref blend,2);PaintError=Painted?0:Marshal.GetLastWin32Error();PaintCount++;if(!Painted)throw new Win32Exception(PaintError);
   // Layer placement belongs to first presentation, never to the frame clock.
   if(!wasPainted){SetWindowPos(Handle,IntPtr.Zero,0,0,0,0,pinned?0x217u:0x213u);ZOrderCalls++;ShowWindow(Handle,4);VisibilityCalls++;}
  }finally{if(old!=IntPtr.Zero)SelectObject(dc,old);if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);if(dc!=IntPtr.Zero)DeleteDC(dc);}
 }
 public void Show(bool show){if(!show)CancelInput(true);if(Alive){ShowWindow(Handle,show?4:0);VisibilityCalls++;}}
 public byte AlphaAt(int x,int y)=>x<0||y<0||x>=width||y>=height?(byte)0:pixels[(y*width+x)*4+3];
 public static IntPtr HitAt(Point point)=>WindowFromPoint(new PointI{X=(int)Math.Round(point.X),Y=(int)Math.Round(point.Y)});
 public static Point Cursor(){GetCursorPos(out var p);return new(p.X,p.Y);}
 public void MoveTo(Point position){if(!Hosted)return;var p=new PointI{X=(int)Math.Round(position.X),Y=(int)Math.Round(position.Y)};if((pinned||ScreenToClient(parent,ref p))&&!SetWindowPos(Handle,IntPtr.Zero,p.X,p.Y,0,0,0x215))throw new Win32Exception(Marshal.GetLastWin32Error());}
 static IntPtr Route(IntPtr hwnd,uint m,IntPtr w,IntPtr l){
  if(windows.TryGetValue(hwnd,out var surface)){
   if(m==0x82){windows.Remove(hwnd);surface.Handle=IntPtr.Zero;surface.Painted=false;surface.pressed=surface.dragging=false;surface.dragTimer.Stop();return DefWindowProc(hwnd,m,w,l);}
   try{return surface.Input(m,w,l);}catch(Exception e){surface.LastInputError=e.Message;WindowLayer.Diagnostics?.Invoke("Ruler native input: "+e.Message);}
  }return DefWindowProc(hwnd,m,w,l);
 }
 IntPtr Input(uint m,IntPtr w,IntPtr l){
  if(m==0x21)return new IntPtr(3); // MA_NOACTIVATE
  var local=new Point((short)(l.ToInt64()&0xffff)/dpi,(short)((l.ToInt64()>>16)&0xffff)/dpi);
  if(m==0x201){pressed=true;resizing=CanResize?.Invoke(local)==true;dragging=!resizing&&CanDrag?.Invoke(local)==true;moved=false;down=Cursor();downDpi=dpi;start=Bounds;SetCapture(Handle);if(dragging||resizing)dragTimer.Start();PointerLeft?.Invoke();return IntPtr.Zero;}
  if(m==0x200){
   if(pressed&&(dragging||resizing))ContinueDrag();
   else {PointerMoved?.Invoke(local);var tracking=new TrackInfo{Size=(uint)Marshal.SizeOf<TrackInfo>(),Flags=3,Window=Handle,Hover=400};TrackMouseEvent(ref tracking);}return IntPtr.Zero;
  }
  if(m==0x2a1){HoverCount++;Hovered?.Invoke(local,Cursor());return IntPtr.Zero;}
  if(m==0x2a3){PointerLeft?.Invoke();return IntPtr.Zero;}
  if(m==0x202){if(pressed&&(dragging||resizing))FinishDrag();else{bool click=pressed&&(Cursor()-down).Length<=6*dpi;CancelInput();if(click){ClickCount++;Clicked?.Invoke(local,Cursor());}}return IntPtr.Zero;}
  if(m==0x215||m==0x1f){CancelInput(true);return IntPtr.Zero;}
  if(m==0x205){MenuCount++;PointerLeft?.Invoke();ContextRequested?.Invoke(Cursor());return IntPtr.Zero;}
  return DefWindowProc(Handle,m,w,l);
 }
 double ResizeWidth(Point cursor)=>RadialResize?RingLayout.RadialDiameter(start.Width/downDpi,new Point(start.X+start.Width/2,start.Y+start.Height/2),down,cursor,downDpi):(start.Width+cursor.X-down.X)/downDpi;
 void ContinueDrag(){var cursor=Cursor();double dx=cursor.X-down.X,dy=cursor.Y-down.Y;if(Math.Abs(dx)+Math.Abs(dy)>3)moved=true;if(moved){if(resizing)WidthChanged?.Invoke(ResizeWidth(cursor),false);else{MoveTo(new Point(start.X+dx,start.Y+dy));PositionChanged?.Invoke(Bounds,false);}}}
 void FinishDrag(){bool finished=pressed&&(dragging||resizing),wasMoved=moved,wasResize=resizing;pressed=dragging=resizing=false;dragTimer.Stop();if(GetCapture()==Handle)ReleaseCapture();if(finished){if(wasResize){if(wasMoved){ResizeCount++;WidthChanged?.Invoke(ResizeWidth(Cursor()),true);}}else{if(wasMoved)DragCount++;PositionChanged?.Invoke(Bounds,true);}}}
 void CancelInput(bool finishResize=false){bool commit=finishResize&&pressed&&resizing&&moved&&Alive;double currentWidth=commit?Bounds.Width/dpi:0;pressed=dragging=resizing=false;dragTimer.Stop();if(Handle!=IntPtr.Zero&&GetCapture()==Handle)ReleaseCapture();if(commit)WidthChanged?.Invoke(currentWidth,true);PointerLeft?.Invoke();}
 public void Dispose(){CancelInput();if(Handle==IntPtr.Zero)return;var h=Handle;bool own=Alive;if(GetCapture()==h)ReleaseCapture();windows.Remove(h);Handle=IntPtr.Zero;Painted=false;pixels=Array.Empty<byte>();if(own)DestroyWindow(h);}
 static uint Owner(IntPtr h){GetWindowThreadProcessId(h,out uint pid);return pid;}
 delegate IntPtr Proc(IntPtr h,uint m,IntPtr w,IntPtr l);
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct ClassInfo{public uint Size,Style;public Proc Procedure;public int ClassExtra,WindowExtra;public IntPtr Instance,Icon,Cursor,Background;public string? Menu;public string Name;public IntPtr SmallIcon;}
 [StructLayout(LayoutKind.Sequential)]struct PointI{public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]struct RectI{public int Left,Top,Right,Bottom;}
 [StructLayout(LayoutKind.Sequential)]struct SizeI{public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]struct BitmapInfo{public uint Size;public int Width,Height;public ushort Planes,Bits;public uint Compression,ImageSize;public int X,Y;public uint Used,Important;}
 [StructLayout(LayoutKind.Sequential)]struct Blend{public byte Operation,Flags,Alpha,Format;}
 [StructLayout(LayoutKind.Sequential)]struct TrackInfo{public uint Size,Flags;public IntPtr Window;public uint Hover;}
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string? name);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern ushort RegisterClassEx(ref ClassInfo c);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr CreateWindowEx(uint ex,string cls,string title,uint style,int x,int y,int width,int height,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr data);
 [DllImport("user32.dll")]static extern IntPtr DefWindowProc(IntPtr h,uint m,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")]static extern IntPtr GetParent(IntPtr h);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr h,out RectI rect);
 [DllImport("user32.dll",SetLastError=true)]static extern bool ScreenToClient(IntPtr h,ref PointI p);
 [DllImport("user32.dll")]static extern bool DestroyWindow(IntPtr h);
 [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int command);
 [DllImport("user32.dll",SetLastError=true)]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
 [DllImport("user32.dll")]static extern IntPtr SetCapture(IntPtr h);
 [DllImport("user32.dll")]static extern IntPtr GetCapture();
 [DllImport("user32.dll")]static extern bool ReleaseCapture();
 [DllImport("user32.dll")]static extern bool GetCursorPos(out PointI p);
 [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")]static extern bool TrackMouseEvent(ref TrackInfo info);
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(PointI point);
 [DllImport("user32.dll")]static extern IntPtr LoadCursor(IntPtr instance,IntPtr name);
 [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapInfo info,uint usage,out IntPtr bits,IntPtr section,uint offset);
 [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
 [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
 [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr h,IntPtr dest,ref PointI pos,ref SizeI size,IntPtr dc,ref PointI origin,uint key,ref Blend blend,uint flags);
}
