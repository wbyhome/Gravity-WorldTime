using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WorldTime;

// A native per-pixel-alpha desktop child. WPF renders the visual into a bitmap,
// without depending on WPF's top-level HWND compositor after SetParent.
public sealed class DesktopSurface : IDisposable {
 readonly Bar bar;readonly WindowProc callback;
 public IntPtr Handle {get;private set;}
 public bool PaintSucceeded {get;private set;}
 public int PaintError {get;private set;}
 public int OpaquePixels {get;private set;}
 public int PaintCount {get;private set;}
 bool visible,dragging;int x,y,width,height;double scale=1;
 static readonly Dictionary<IntPtr,DesktopSurface> surfaces=new();
 static readonly WindowProc route=Route;static ushort atom;
 const uint Child=0x40000000,Layered=0x80000,Tool=0x80;
 public DesktopSurface(Bar bar,IntPtr parent){
  this.bar=bar;callback=route;
  if(atom==0){var wc=new ClassInfo{Size=(uint)Marshal.SizeOf<ClassInfo>(),Procedure=callback,Instance=GetModuleHandle(null),Name="Gravity.WorldTime.DesktopSurface"};atom=RegisterClassEx(ref wc);if(atom==0)throw new Win32Exception(Marshal.GetLastWin32Error());}
  Handle=CreateWindowEx(Layered|Tool,"Gravity.WorldTime.DesktopSurface",bar.Title,Child,0,0,1,1,parent,IntPtr.Zero,GetModuleHandle(null),IntPtr.Zero);
  if(Handle==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());surfaces.Add(Handle,this);
 }
 public void Move(int x,int y,int width,int height,double scale){this.x=x;this.y=y;this.width=width;this.height=height;this.scale=scale;Present();}
 public void Show(bool value){visible=value;if(Handle!=IntPtr.Zero){ShowWindow(Handle,value?4:0);if(value)Present();}}
 public void Present(){
  if(!visible||Handle==IntPtr.Zero||width<=0||height<=0||bar.Content is not FrameworkElement visual)return;
  IntPtr dc=IntPtr.Zero,bitmap=IntPtr.Zero,old=IntPtr.Zero;
  try{
   visual.Measure(new Size(width/scale,height/scale));visual.Arrange(new Rect(0,0,width/scale,height/scale));visual.UpdateLayout();
   var image=new RenderTargetBitmap(width,height,96*scale,96*scale,PixelFormats.Pbgra32);image.Render(visual);
   var pixels=new byte[width*height*4];image.CopyPixels(pixels,width*4,0);int opaque=0;for(int i=3;i<pixels.Length;i+=4)if(pixels[i]>=245)opaque++;OpaquePixels=opaque;
   dc=CreateCompatibleDC(IntPtr.Zero);var info=new BitmapInfo{Size=40,Width=width,Height=-height,Planes=1,Bits=32};
   bitmap=CreateDIBSection(dc,ref info,0,out var bits,IntPtr.Zero,0);if(bitmap==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
   old=SelectObject(dc,bitmap);Marshal.Copy(pixels,0,bits,pixels.Length);
   var point=new PointI{X=x,Y=y};var origin=new PointI();var size=new SizeI{X=width,Y=height};var blend=new Blend{Alpha=255,Format=1};
   PaintSucceeded=UpdateLayeredWindow(Handle,IntPtr.Zero,ref point,ref size,dc,ref origin,0,ref blend,2);PaintError=PaintSucceeded?0:Marshal.GetLastWin32Error();PaintCount++;
   if(PaintSucceeded)SetWindowPos(Handle,IntPtr.Zero,0,0,0,0,0x213);
  }finally{if(old!=IntPtr.Zero)SelectObject(dc,old);if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);if(dc!=IntPtr.Zero)DeleteDC(dc);}
 }
 static IntPtr Route(IntPtr hwnd,uint message,IntPtr wp,IntPtr lp){
  if(surfaces.TryGetValue(hwnd,out var surface)){
   try{return surface.Input(message,wp,lp);}catch(Exception ex){WindowLayer.Diagnostics?.Invoke("Desktop surface input: "+ex.Message);}
  }
  return DefWindowProc(hwnd,message,wp,lp);
 }
 IntPtr Input(uint message,IntPtr wp,IntPtr lp){
  if(message==0x21)return new IntPtr(3); // MA_NOACTIVATE
  if(message==0x201){
   GetCursorPos(out var point);double local=(short)(lp.ToInt64()&0xffff)/scale;
   if(local>width/scale-36){bar.FoldFromSurface();return IntPtr.Zero;}
   dragging=bar.BeginDrag(new Point(point.X,point.Y),local<=154);if(dragging)SetCapture(Handle);return IntPtr.Zero;
  }
  if(message==0x200){
   bar.SurfaceHover(true);var track=new TrackInfo{Size=(uint)Marshal.SizeOf<TrackInfo>(),Flags=2,Window=Handle};TrackMouseEvent(ref track);
   if(dragging){GetCursorPos(out var point);bar.ContinueDrag(new Point(point.X,point.Y));}else Present();return IntPtr.Zero;
  }
  if(message==0x2a3){bar.SurfaceHover(false);Present();return IntPtr.Zero;}
  if(message==0x202||message==0x215){
   if(dragging){dragging=false;bar.CompleteDrag((GetKeyState(0x12)&0x8000)!=0);if(GetCapture()==Handle)ReleaseCapture();}return IntPtr.Zero;
  }
  if(message==0x205){
   GetCursorPos(out var point);bar.SurfaceMenu(new Point(point.X/scale,point.Y/scale));return IntPtr.Zero;
  }
  return DefWindowProc(Handle,message,wp,lp);
 }
 public void Dispose(){if(Handle!=IntPtr.Zero){surfaces.Remove(Handle);DestroyWindow(Handle);Handle=IntPtr.Zero;}}
 delegate IntPtr WindowProc(IntPtr h,uint m,IntPtr w,IntPtr l);
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct ClassInfo{public uint Size,Style;public WindowProc Procedure;public int ClassExtra,WindowExtra;public IntPtr Instance,Icon,Cursor,Background;public string? Menu;public string Name;public IntPtr SmallIcon;}
 [StructLayout(LayoutKind.Sequential)]struct PointI{public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]struct SizeI{public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]struct BitmapInfo{public uint Size;public int Width,Height;public ushort Planes,Bits;public uint Compression,ImageSize;public int X,Y;public uint Used,Important;}
 [StructLayout(LayoutKind.Sequential)]struct Blend{public byte Operation,Flags,Alpha,Format;}
 [StructLayout(LayoutKind.Sequential)]struct TrackInfo{public uint Size,Flags;public IntPtr Window;public uint Hover;}
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr GetModuleHandle(string? name);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern ushort RegisterClassEx(ref ClassInfo info);
 [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr CreateWindowEx(uint ex,string name,string title,uint style,int x,int y,int w,int h,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr data);
 [DllImport("user32.dll")]static extern IntPtr DefWindowProc(IntPtr h,uint m,IntPtr w,IntPtr l);
 [DllImport("user32.dll")]static extern bool DestroyWindow(IntPtr h);
 [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int command);
 [DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int width,int height,uint flags);
 [DllImport("user32.dll")]static extern IntPtr SetCapture(IntPtr h);
 [DllImport("user32.dll")]static extern bool ReleaseCapture();
 [DllImport("user32.dll")]static extern IntPtr GetCapture();
 [DllImport("user32.dll")]static extern bool GetCursorPos(out PointI point);
 [DllImport("user32.dll")]static extern short GetKeyState(int key);
 [DllImport("user32.dll")]static extern bool TrackMouseEvent(ref TrackInfo info);
 [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapInfo info,uint usage,out IntPtr bits,IntPtr section,uint offset);
 [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
 [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
 [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr destination,ref PointI position,ref SizeI size,IntPtr source,ref PointI origin,uint key,ref Blend blend,uint flags);
}
