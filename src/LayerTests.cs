using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace WorldTime;

// Exercises real HWND ordering using two disposable windows in a separate
// process. It never adjusts existing application or Explorer windows.
public static class LayerTests {
 [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hwnd,uint command);
 [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")] static extern IntPtr GetStyle(IntPtr hwnd,int index);
 [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd,int command);
 [DllImport("user32.dll")] static extern bool IsIconic(IntPtr hwnd);
 static bool Above(IntPtr a,IntPtr b){for(var w=GetWindow(b,3);w!=IntPtr.Zero;w=GetWindow(w,3))if(w==a)return true;return false;}
 public static void Run(string[] args){
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
  var target=new Window{Title="WorldTime isolated layer probe",WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,ShowInTaskbar=false,Width=290,Height=90,Left=100,Top=420,Content=new TextBlock{Text="WorldTime layer test (temporary)"}};
  var other=new Window{Title="WorldTime reference probe",Width=290,Height=90,Left=110,Top=430,Content=new TextBlock{Text="WorldTime normal reference (temporary)"}};
  var layer=new WindowLayer(target,"normal");target.Show();other.Show();
  var a=new WindowInteropHelper(target).Handle;var b=new WindowInteropHelper(other).Handle;
  var lines=new List<string>();int step=0;
  void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);lines.Add("PASS "+name);}
  var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(180)};
  timer.Tick+=(_,_)=>{try{
   switch(step++){
    case 0:Check(layer.IsDesktopHosted,"Normal mode is an actual child of the Explorer desktop band");Check((GetStyle(a,-16).ToInt64()&0x40000000L)!=0,"Desktop clock uses WS_CHILD");layer.Set("top");break;
    case 1:Check((GetStyle(a,-20).ToInt64()&8)!=0,"Top mode sets native TOPMOST");Check(Above(a,b),"Top mode above normal reference");layer.Set("normal");break;
    case 2:Check((GetStyle(a,-20).ToInt64()&8)==0,"Normal mode removes TOPMOST");Check(layer.IsDesktopHosted,"Returning from top mode reattaches to desktop");layer.Set("bottom");break;
    case 3:Check(layer.IsDesktopHosted,"Legacy bottom setting now uses actual desktop hosting");target.Activate();break;
    case 4:Check(layer.IsDesktopHosted,"Desktop parent survives activation attempt");target.Left+=12;break;
    case 5:Check(layer.IsDesktopHosted,"Desktop parent survives moving");layer.Set("top");break;
    case 6:lines.Add($"INFO top-after-bottom style={GetStyle(a,-20).ToInt64():X} above={Above(a,b)} wpfTop={target.Topmost}");Check((GetStyle(a,-20).ToInt64()&8)!=0&&Above(a,b),"Bottom to top releases bottom hook");layer.Set("normal");target.Activate();break;
    case 7:
     Check(layer.IsDesktopHosted,"Normal mode returns to desktop rather than floating Z-order");
     var state=new State{Layer="bottom"};Check(JsonSerializer.Deserialize<State>(JsonSerializer.Serialize(state))!.Layer=="bottom","Layer setting roundtrip");
     bool rejected=false;try{Store.Validate(new State{Layer="unsupported"});}catch(InvalidDataException){rejected=true;}Check(rejected,"Reject unsupported layer setting");
     layer.Set("top");ShowWindow(a,6);break;
    case 8:
     Check(IsIconic(a),"Explicit minimization of our own floating probe still works");layer.Set("top");layer.RestoreNoActivate();break;
    case 9:
     Check(!IsIconic(a),"Recovery restores minimized clock");
     Check((GetStyle(a,-20).ToInt64()&8)!=0&&Above(a,b),"Recovery pins above normal reference");
     lines.Add("TOTAL 16 PASS — native parenting/layer/minimize checks; physical Win+D is a separate acceptance probe");Finish(0);break;
   }
  }catch(Exception e){lines.Add(e.ToString());Finish(1);}};
  void Finish(int result){timer.Stop();target.Close();other.Close();string path=args.Contains("--report")?args[Array.IndexOf(args,"--report")+1]:"layer-results.txt";File.WriteAllLines(path,lines);Environment.ExitCode=result;app.Shutdown();}
  timer.Start();app.Run();
 }
}
