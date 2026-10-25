using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
namespace WorldTime;
// Explicit, bounded export of this process's actual native-upload source frames.
// These are not OS screenshots or a capture of the user's desktop.
public static class RulerMotionRecording {
 public static void Start(Host host,string directory){
  directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);int count=0,ticks=0,lastPaint=-1;var frames=new List<object>();var timer=new DispatcherTimer(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(500)};
  timer.Tick+=(_,_)=>{if(host.Exiting||++ticks>40||count>=18){timer.Stop();return;}var ruler=host.Ruler;var surface=ruler?.Desktop.Surface;if(surface?.Shown!=true||surface.PaintError!=0||surface.PaintCount==lastPaint||ruler?.LastMotionBitmap is not { } image)return;
   try{string name=$"frame-{count:000}.png";var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var stream=File.Create(Path.Combine(directory,name)))encoder.Save(stream);lastPaint=surface.PaintCount;frames.Add(new{Frame=count,File=name,Utc=DateTimeOffset.UtcNow,NativePaint=lastPaint,NativeHandle=surface.Handle.ToInt64(),ruler.MotionFrames,Chosen=ruler.MotionMode.ToString(),Effective=ruler.EffectiveMotionMode.ToString(),ruler.MotionRunning});count++;File.WriteAllText(Path.Combine(directory,"frames.json"),JsonSerializer.Serialize(new{Source="Actual native-upload source frames, not OS screenshots",Version=Assembly.GetExecutingAssembly().GetName().Version?.ToString(),Frames=frames},new JsonSerializerOptions{WriteIndented=true}));}
   catch{timer.Stop();}
  };timer.Start();
 }
}
