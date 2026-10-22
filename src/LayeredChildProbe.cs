using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace WorldTime;

// Native render-capability probe. Both invisible windows belong to this
// process; no Explorer window, user layout or external application is changed.
public static class LayeredChildProbe {
 [StructLayout(LayoutKind.Sequential)]struct PointI {public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]struct SizeI {public int X,Y;}
 [StructLayout(LayoutKind.Sequential)]struct BitmapInfo {public uint Size;public int Width,Height;public ushort Planes,Bits;public uint Compression,ImageSize;public int X,Y;public uint Used,Important;}
 [StructLayout(LayoutKind.Sequential)]struct Blend {public byte Operation,Flags,Alpha,Format;}
 [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]static extern IntPtr CreateWindowEx(uint ex,string name,string title,uint style,int x,int y,int w,int h,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr data);
 [DllImport("user32.dll")]static extern bool DestroyWindow(IntPtr h);
 [DllImport("gdi32.dll")]static extern IntPtr CreateCompatibleDC(IntPtr dc);
 [DllImport("gdi32.dll")]static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapInfo info,uint usage,out IntPtr bits,IntPtr section,uint offset);
 [DllImport("gdi32.dll")]static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
 [DllImport("gdi32.dll")]static extern bool DeleteObject(IntPtr obj);
 [DllImport("gdi32.dll")]static extern bool DeleteDC(IntPtr dc);
 [DllImport("user32.dll",SetLastError=true)]static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr destination,ref PointI position,ref SizeI size,IntPtr source,ref PointI origin,uint key,ref Blend blend,uint flags);
 public static void Run(string[] args){
  string report=Path.GetFullPath(args[Array.IndexOf(args,"--report")+1]);
  IntPtr parent=IntPtr.Zero,child=IntPtr.Zero,dc=IntPtr.Zero,bitmap=IntPtr.Zero,old=IntPtr.Zero;
  bool ok=false;int error=0;
  try{
   parent=CreateWindowEx(0,"STATIC","WorldTime invisible probe parent",0x80000000,0,0,2,2,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
   child=CreateWindowEx(0x80000,"STATIC","WorldTime invisible layered child",0x40000000,0,0,2,2,parent,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);
   error=Marshal.GetLastWin32Error();
   if(parent!=IntPtr.Zero&&child!=IntPtr.Zero){
    dc=CreateCompatibleDC(IntPtr.Zero);var info=new BitmapInfo{Size=40,Width=2,Height=-2,Planes=1,Bits=32};
    bitmap=CreateDIBSection(dc,ref info,0,out var bits,IntPtr.Zero,0);old=SelectObject(dc,bitmap);
    for(int i=0;i<4;i++)Marshal.WriteInt32(bits,i*4,unchecked((int)0x80403020));
    var p=new PointI();var size=new SizeI{X=2,Y=2};var blend=new Blend{Alpha=255,Format=1};
    ok=UpdateLayeredWindow(child,IntPtr.Zero,ref p,ref size,dc,ref p,0,ref blend,2);error=ok?0:Marshal.GetLastWin32Error();
   }
   File.WriteAllText(report,JsonSerializer.Serialize(new{Executable=Environment.ProcessPath,ParentCreated=parent!=IntPtr.Zero,ChildCreated=child!=IntPtr.Zero,UpdateLayeredChildSucceeded=ok,Win32Error=error},new JsonSerializerOptions{WriteIndented=true}));
   Environment.ExitCode=ok?0:1;
  }finally{
   if(child!=IntPtr.Zero)DestroyWindow(child);if(parent!=IntPtr.Zero)DestroyWindow(parent);
   if(old!=IntPtr.Zero)SelectObject(dc,old);if(bitmap!=IntPtr.Zero)DeleteObject(bitmap);if(dc!=IntPtr.Zero)DeleteDC(dc);
  }
 }
}
