using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace WorldTime;

// Manual-input acceptance harness: only synthetic cities and our own windows.
// It observes hit testing after real user/automation input; it does not send
// Win+D, simulate keyboard/mouse input, or change other applications.
public static class AcceptanceProbe {
 [StructLayout(LayoutKind.Sequential)]struct PointI{public int X,Y;}
 [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(PointI point);
 [DllImport("user32.dll")]static extern bool IsIconic(IntPtr hwnd);
 [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hwnd);
 public static void Run(string[] args){
  string report=Path.GetFullPath(args.Contains("--report")?args[Array.IndexOf(args,"--report")+1]:"acceptance.json");
  string temp=Path.Combine(Path.GetTempPath(),"WorldTime-acceptance-"+Guid.NewGuid().ToString("N"));
  var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};Host? host=null;
  try{
   var seed=new State{AppearanceVersion=1,Language="zh-Hans"};
   int i=0;foreach(var city in Catalog.All.Where(c=>new[]{"伦敦","纽约","东京"}.Contains(c.Zh))){seed.Rows.Add(new Row{City=city.Zh,Zone=city.Zone,Code=city.Code,Group="acceptance-stack",Order=i,X=200,Y=390+i++*Layout.Step});}
   using(var disk=new Store(temp))disk.Save(seed);
   host=new Host(new Store(temp));
   var panel=new StackPanel{Margin=new Thickness(14)};
   var controller=new Window{Title="WorldTime acceptance controller — isolated",Width=490,Height=190,Left=70,Top=80,Content=panel};
   panel.Children.Add(new TextBlock{Text="Temporary synthetic clocks · no real layout loaded",TextWrapping=TextWrapping.Wrap});
   var status=new TextBlock{Text="Desktop mode: waiting for Win+D",Margin=new Thickness(0,10,0,10)};panel.Children.Add(status);
   var buttons=new WrapPanel();panel.Children.Add(buttons);
   bool finished=false;var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(250)};int ticks=0;
   void Finish(){if(finished)return;finished=true;timer.Stop();host.Quit();}
   void Button(string text,Action action){var b=new Button{Content=text,Padding=new Thickness(8,5,8,5),Margin=new Thickness(0,0,8,0)};b.Click+=(_,_)=>action();buttons.Children.Add(b);}
   Button("Reset stack",()=>{host.S.Docked.Clear();int j=0;foreach(var r in host.S.Rows){r.Group="acceptance-stack";r.Order=j;r.X=200;r.Y=390+j++*Layout.Step;}host.Sync();host.Save();});
   Button("Desktop mode",()=>host.SetLayer("normal"));Button("Top mode",()=>host.SetLayer("top"));Button("Finish",Finish);
   controller.Closed+=(_,_)=>Finish();controller.Show();string previous="";
   timer.Tick+=(_,_)=>{
    var bars=host.Bars.Values.Select(b=>{var bounds=DockScreen.Bounds(b);IntPtr hit=WindowFromPoint(new PointI{X=(int)(bounds.Left+Math.Min(40,bounds.Width/2)),Y=(int)(bounds.Top+bounds.Height/2)});return new{b.R.City,b.R.Group,b.R.Order,b.R.X,b.R.Y,b.R.Width,Handle=b.Layer.NativeHandle.ToInt64(),DesktopHosted=b.Layer.IsDesktopHosted,Visible=IsWindowVisible(b.Layer.NativeHandle),Minimized=IsIconic(b.Layer.NativeHandle),Hit=hit.ToInt64(),HitOwnClock=hit==b.Layer.NativeHandle,Bounds=new{bounds.X,bounds.Y,bounds.Width,bounds.Height},b.Layer.BindingError};}).ToArray();
    bool minimized=IsIconic(new WindowInteropHelper(controller).Handle);
    var state=new{Mode=host.S.Layer,ControllerMinimized=minimized,Rows=bars,Docked=host.S.Docked,TempDirectory=temp};
    string json=JsonSerializer.Serialize(state,new JsonSerializerOptions{WriteIndented=true});File.WriteAllText(report,json);
    if(json!=previous){File.AppendAllText(report+".journal",JsonSerializer.Serialize(new{Utc=DateTimeOffset.UtcNow,State=state})+Environment.NewLine);previous=json;}
    status.Text=$"{host.S.Layer}: {bars.Count(b=>b.DesktopHosted)} desktop children; {bars.Count(b=>b.HitOwnClock)} hit-test visible";
    if(++ticks>960)Finish();
   };
   timer.Start();app.Run();
  }finally{
   if(host!=null&&!host.Exiting){host.Tray.Dispose();host.Disk.Dispose();}
   if(Directory.Exists(temp))Directory.Delete(temp,true);
  }
 }
}
