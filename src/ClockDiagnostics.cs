using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Threading;

namespace WorldTime;

// Bounded, read-only observation of our own clock rectangles. No screenshots,
// keystrokes, external window titles or user file contents are recorded.
public static class ClockDiagnostics {
 [StructLayout(LayoutKind.Sequential)]struct PointI{public int X,Y;}
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(PointI point);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hwnd);
 [DllImport("user32.dll")]static extern bool IsIconic(IntPtr hwnd);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr hwnd,StringBuilder name,int size);
 public static void Start(Host host,string report){
  report=Path.GetFullPath(report);string previous="";int count=0;
  var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};
  timer.Tick+=(_,_)=>{
   if(host.Exiting||++count>360){timer.Stop();return;}
   try{
    var state=new{host.S.Layer,host.S.Opacity,Rows=host.Bars.Values.Select(b=>{
     var bounds=DockScreen.Bounds(b);var hit=WindowFromPoint(new PointI{X=(int)bounds.Left+35,Y=(int)bounds.Top+25});var name=new StringBuilder(100);GetClassName(hit,name,100);
     return new{b.R.Id,Handle=b.Layer.NativeHandle.ToInt64(),b.Layer.IsDesktopHosted,Visible=IsWindowVisible(b.Layer.NativeHandle),Minimized=IsIconic(b.Layer.NativeHandle),HitOwnClock=hit==b.Layer.NativeHandle,OccluderClass=name.ToString(),Bounds=new{bounds.X,bounds.Y,bounds.Width,bounds.Height},NativePaint=b.Layer.Surface?.PaintSucceeded,PaintError=b.Layer.Surface?.PaintError,OpaqueTextPixels=b.Layer.Surface?.OpaquePixels};}).ToArray()};
    string json=JsonSerializer.Serialize(state,new JsonSerializerOptions{WriteIndented=true});
    if(json!=previous){File.WriteAllText(report,json);File.AppendAllText(report+".journal",JsonSerializer.Serialize(new{Utc=DateTimeOffset.UtcNow,State=state})+Environment.NewLine);previous=json;}
   }catch(Exception ex){WindowLayer.Diagnostics?.Invoke("Clock observer: "+ex.Message);}
  };
  timer.Start();
 }
}
