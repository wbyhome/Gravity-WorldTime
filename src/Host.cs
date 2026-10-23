using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
namespace WorldTime;
public class Host {
 public State S;public Store Disk;public Dictionary<string,Bar> Bars=new();
 public readonly Dictionary<string,EdgeTab> EdgeTabs=new();
 readonly DispatcherTimer timer=new(){Interval=TimeSpan.FromSeconds(1)};
 readonly DispatcherTimer environmentTimer=new(){Interval=TimeSpan.FromMilliseconds(300)};
 public readonly CityManager Manager;public readonly DesktopTray Tray;public bool Exiting;string minute="";
 public string Language=>L10n.Resolve(S.Language);
 public string T(string key)=>L10n.Text(S.Language,key);
 public Host(Store disk){Disk=disk;bool firstRun=!File.Exists(disk.FilePath);S=disk.Load();
  if(S.AppearanceVersion<2){if(S.AppearanceVersion<1||Math.Abs(S.Opacity-.72)<.0001)S.Opacity=.35;S.AppearanceVersion=2;disk.Save(S);}
  Manager=new CityManager(this);Tray=new DesktopTray(this);
  if(firstRun){string g=Guid.NewGuid().ToString("N");foreach(var name in new[]{"伦敦","纽约","东京"}){var r=Make(Catalog.All.First(x=>x.Zh==name));r.Group=g;r.Order=S.Rows.Count;r.Y=160+r.Order*Layout.Step;S.Rows.Add(r);}}
  if(S.Displays.Count==0){double legacyScale=DockScreen.Find("").Scale;foreach(var r in S.Rows.Where(r=>r.Monitor.Length==0&&Math.Abs(r.PositionScale-1)<.001))r.PositionScale=legacyScale;}
  bool adjusted=DisplayLayout.Reconcile(S,DockScreen.All());foreach(var r in S.Rows.ToList())Open(r);
  Sync();Manager.RefreshCities(); // Startup stays in tray; never show or minimize a taskbar panel.
  if(firstRun||adjusted)Save();
  environmentTimer.Tick+=(_,_)=>{environmentTimer.Stop();try{RefreshEnvironment();}catch(Exception e){WindowLayer.Diagnostics?.Invoke("Display refresh deferred: "+e.Message);environmentTimer.Start();}};
  timer.Tick+=(_,_)=>Tick();timer.Start();SystemEvents.TimeChanged+=TimeChanged;SystemEvents.PowerModeChanged+=PowerChanged;SystemEvents.DisplaySettingsChanged+=ScreensChanged;
 }
 void TimeChanged(object? o,EventArgs e)=>Application.Current.Dispatcher.BeginInvoke(()=>{TimeZoneInfo.ClearCachedData();Rules.ClearCache();Tick(true);});
 void PowerChanged(object o,PowerModeChangedEventArgs e)=>Application.Current.Dispatcher.BeginInvoke(()=>{if(e.Mode==PowerModes.Resume){environmentTimer.Stop();environmentTimer.Start();}Tick(true);});
 void ScreensChanged(object? o,EventArgs e)=>Application.Current.Dispatcher.BeginInvoke(()=>{environmentTimer.Stop();environmentTimer.Start();});
 void Tick(bool force=false){foreach(var b in Bars.Values)b.Layer.Maintain();Manager.RefreshDisplayStatus();string m=DateTimeOffset.UtcNow.ToString("yyyyMMddHHmm");if(force||m!=minute){minute=m;foreach(var b in Bars.Values)b.Refresh();}}
 public void RefreshEnvironment(){
  if(Exiting)return;if(Bars.Values.Any(b=>b.IsDragging)){environmentTimer.Start();return;}
  bool changed=DisplayLayout.Reconcile(S,DockScreen.All());foreach(var b in Bars.Values)b.Layer.Maintain();Sync();Tick(true);if(changed)Save();
 }
 public bool TryAutoCollapse(Row row){
  var rows=Layout.Group(S,row);if(rows.Count==0)return false;
  var area=DockScreen.For(Bars[row.Id]);var bounds=DockScreen.Bounds(Bars[rows[0].Id]);
  foreach(var r in rows.Skip(1))bounds.Union(DockScreen.Bounds(Bars[r.Id]));
  string? edge=DockGeometry.Contact(area,bounds);if(edge==null)return false;
  // Save an on-screen expansion position even when the release overshot an edge.
  double x=Math.Clamp(bounds.Left,area.Left,Math.Max(area.Left,area.Right-bounds.Width));
  double y=Math.Clamp(bounds.Top,area.Top,Math.Max(area.Top,area.Bottom-bounds.Height));
  foreach(var r in rows){r.X+=(x-bounds.Left)/area.Scale;r.Y+=(y-bounds.Top)/area.Scale;}
  Sync();CollapseGroup(row,edge);return true;
 }
 public Row Make(City c)=>new(){City=c.Zh,Zone=c.Zone,Code=c.Code};
 public void EditRoutine(Row row)=>new RoutineEditor(this,row).ShowDialog();
 public void RecoverClocks(){
  RefreshEnvironment();
  S.Docked.Clear();
  foreach(var group in S.Rows.GroupBy(r=>r.Group)){
   var rows=group.OrderBy(r=>r.Order).ToList();var bounds=DockScreen.Bounds(Bars[rows[0].Id]);
   var area=DockScreen.For(Bars[rows[0].Id]);
   double margin=24*area.Scale;
   double x=Math.Clamp(bounds.Left,area.Left+margin,Math.Max(area.Left+margin,area.Right-rows.Max(r=>r.Width)*area.Scale-margin));
   double y=Math.Clamp(bounds.Top,area.Top+margin,Math.Max(area.Top+margin,area.Bottom-(rows.Count-1)*Layout.Step*area.Scale-50*area.Scale-margin));
   double dx=x/area.Scale-rows[0].X,dy=y/area.Scale-rows[0].Y;
   foreach(var r in rows){r.X+=dx;r.Y+=dy;}
  }
  SetLayer("top");if(S.Layer!="top")return;
  try{foreach(var b in Bars.Values)b.Layer.RestoreNoActivate();Save();}
  catch(Exception e){MessageBox.Show(T("Could not restore every clock.")+"\n"+e.Message,"WorldTime");}
 }
 void Open(Row r){var b=new Bar(this,r);Bars[r.Id]=b;b.Show();if(S.Docked.Any(d=>d.Group==r.Group))b.Hide();}
 public void CollapseGroup(Row row,string? edge=null){
  if(S.Docked.Any(d=>d.Group==row.Group))return;
  var rows=Layout.Group(S,row);if(rows.Count==0)return;
  var area=DockScreen.For(Bars[row.Id]);var bounds=DockScreen.Bounds(Bars[rows[0].Id]);
  foreach(var r in rows.Skip(1))bounds.Union(DockScreen.Bounds(Bars[r.Id]));
  string chosen=edge??DockGeometry.Nearest(area,bounds);
  S.Docked.Add(new DockedGroup{Group=row.Group,Edge=chosen,Monitor=area.Device,Offset=DockGeometry.Anchor(area,bounds,chosen)});Sync();Save();
 }
 public void CollapseAll(){foreach(var row in S.Rows.GroupBy(r=>r.Group).Select(g=>g.OrderBy(r=>r.Order).First()).ToList())CollapseGroup(row);}
 public void ExpandGroup(string group){S.Docked.RemoveAll(d=>d.Group==group);Sync();Save();}
 public void ExpandAll(){S.Docked.Clear();Sync();Save();}
 public void SetLayer(string mode){
  if(mode is not ("normal" or "top" or "bottom"))throw new ArgumentException("Unknown layer");
  var previous=S.Layer;var old=Bars;var replacements=new Dictionary<string,Bar>();
  try{
   // WPF layered HWNDs must not be reused across desktop-child / popup roles.
   // Construct each presentation in its final role; keep the rows and groups.
   S.Layer=mode;
   foreach(var r in S.Rows){var b=new Bar(this,r);replacements.Add(r.Id,b);b.Show();if(S.Docked.Any(d=>d.Group==r.Group))b.Hide();}
  }catch(Exception e){
   foreach(var b in replacements.Values)b.Close();S.Layer=previous;
   MessageBox.Show(T("Could not change window order; attempted to restore the previous mode.")+"\n"+e.Message,"WorldTime");return;
  }
  Bars=replacements;foreach(var b in old.Values)b.Close();Sync();Save();
 }
 public void Sync(){
  S.Docked.RemoveAll(d=>!S.Rows.Any(r=>r.Group==d.Group));
  foreach(string group in EdgeTabs.Keys.Where(g=>!S.Docked.Any(d=>d.Group==g)).ToList()){EdgeTabs[group].Close();EdgeTabs.Remove(group);}
  foreach(var b in Bars.Values){b.Left=b.R.X;b.Top=b.R.Y;b.Width=b.R.Width;b.Refresh();if(S.Docked.Any(d=>d.Group==b.R.Group))b.Hide();else if(!b.IsVisible)b.Show();b.Layer.Place(b.R.X,b.R.Y,b.R.Width,50);}
  foreach(var dock in S.Docked){if(!EdgeTabs.ContainsKey(dock.Group)){var tab=new EdgeTab(this,dock);EdgeTabs[dock.Group]=tab;tab.Show();}EdgeTabs[dock.Group].Refresh();}
  // Separate nearby tabs, while retaining the requested normalized anchor in data.
  foreach(var lane in EdgeTabs.Values.GroupBy(t=>DockScreen.Find(t.Dock.Monitor).Device+":"+t.Dock.Edge)){
   var tabs=lane.OrderBy(t=>t.Dock.Offset).ToList();var rects=tabs.Select(t=>t.DesiredBounds).ToList();if(rects.Count<2)continue;
   bool side=tabs[0].Dock.Edge is "left" or "right";var area=DockScreen.Find(tabs[0].Dock.Monitor);double gap=6*area.Scale;
   for(int i=1;i<rects.Count;i++){var rect=rects[i];if(side)rect.Y=Math.Max(rect.Y,rects[i-1].Bottom+gap);else rect.X=Math.Max(rect.X,rects[i-1].Right+gap);rects[i]=rect;}
   double overflow=Math.Max(0,side?rects[^1].Bottom-area.Bottom:rects[^1].Right-area.Right);
   for(int i=0;i<tabs.Count;i++){var rect=rects[i];if(side)rect.Y=Math.Max(area.Top,rect.Y-overflow);else rect.X=Math.Max(area.Left,rect.X-overflow);DockScreen.Place(tabs[i],rect);}
  }
 }
 public bool Save(){try{Disk.Save(S);Manager.RefreshCities();Tray.Refresh();return true;}catch(Exception e){MessageBox.Show(T("Could not save. The layout is still in memory. Check the data folder and try again.")+"\n"+e.Message,"WorldTime");return false;}}
 public void Add(Row? anchor)=>new CityPicker(this,anchor).ShowDialog();
 public void AddRow(Row r,Row? anchor){
  if(anchor!=null&&S.Rows.Contains(anchor)){var g=Layout.Group(S,anchor);r.Group=anchor.Group;r.Order=g.Count;r.X=g[0].X;r.Y=g[^1].Y+Layout.Step;r.Width=anchor.Width;r.PositionScale=anchor.PositionScale;r.Monitor=anchor.Monitor;}else{var screen=DockScreen.Find("");r.PositionScale=screen.Scale;r.Monitor=screen.Device;r.X=screen.Left/screen.Scale+140+(S.Rows.Count%8)*20;r.Y=screen.Top/screen.Scale+140+(S.Rows.Count%8)*30;}
  S.Rows.Add(r);Open(r);Sync();Save();
 }
 public Window Dialog(string title){Manager.Reveal();return new Window{Owner=Manager,ShowInTaskbar=false,Title="WorldTime · "+title,Width=540,SizeToContent=SizeToContent.Height,WindowStartupLocation=WindowStartupLocation.CenterOwner,ResizeMode=ResizeMode.NoResize,Background=new SolidColorBrush(Color.FromRgb(242,244,247)),FontFamily=Manager.FontFamily,FontSize=14,Content=new StackPanel{Margin=new Thickness(22)}};}
 public void Settings(){
  var w=Dialog(T("Settings"));var p=(StackPanel)w.Content;p.Children.Add(new TextBlock{Text=T("Language"),Margin=new Thickness(0,0,0,4)});
  var languages=new ComboBox{ItemsSource=L10n.Names,SelectedIndex=Array.IndexOf(L10n.Languages,S.Language),Height=30,Margin=new Thickness(0,0,0,8)};p.Children.Add(languages);
  var checks=new List<(CheckBox,Action<bool>)>();void Check(string key,bool value,Action<bool> set){var c=new CheckBox{Content=T(key),IsChecked=value,Margin=new Thickness(0,4,0,4)};p.Children.Add(c);checks.Add((c,set));}
  Check("12-hour clock",S.Twelve,v=>S.Twelve=v);Check("Life hints",S.Life,v=>S.Life=v);Check("Contact advice",S.Advice,v=>S.Advice=v);Check("Saturday and Sunday off",S.Weekend,v=>S.Weekend=v);Check("Magnetic snapping",S.Snap,v=>S.Snap=v);
  TextBox Field(string key,string val){p.Children.Add(new TextBlock{Text=T(key),Margin=new Thickness(0,8,0,3)});var t=new TextBox{Text=val};p.Children.Add(t);return t;}
  var start=Field("Work starts (0–23)",S.Start.ToString());var end=Field("Work ends (1–24)",S.End.ToString());var lunch=Field("One-hour break starts (0–23)",S.Lunch.ToString());var opacity=Field("Background opacity (15–100%)",(S.Opacity*100).ToString("0"));var font=Field("Hint font size (11–18)",S.Font.ToString());
  p.Children.Add(new TextBlock{Text=T("Typical schedules only; holidays and personal calendars are unknown."),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,8)});
  var save=new Button{Content=T("Save"),Height=34};p.Children.Add(save);save.Click+=(_,_)=>{if(!int.TryParse(start.Text,out int a)||!int.TryParse(end.Text,out int b)||!int.TryParse(lunch.Text,out int c)||!double.TryParse(opacity.Text,out double o)||!double.TryParse(font.Text,out double f)||a<0||b>24||a>=b||(b-a==1&&c==a)||c<0||c>23||!double.IsFinite(o)||o<15||o>100||!double.IsFinite(f)||f<11||f>18||languages.SelectedIndex<0){MessageBox.Show(T("Invalid values"));return;}foreach(var (box,set) in checks)set(box.IsChecked==true);S.Language=L10n.Languages[languages.SelectedIndex];S.Start=a;S.End=b;S.Lunch=c;S.Opacity=o/100;S.Font=f;Sync();Save();w.Close();};w.ShowDialog();
 }
 public void Rename(Row r){var w=Dialog(T("Name and width"));var p=(StackPanel)w.Content;var name=new TextBox{Text=L10n.CityName(r,S.Language),MaxLength=80};p.Children.Add(name);p.Children.Add(new TextBlock{Text=T("Width (480–900 DIP)"),Margin=new Thickness(0,10,0,4)});var width=new TextBox{Text=r.Width.ToString()};p.Children.Add(width);var ok=new Button{Content=T("Save"),Margin=new Thickness(0,12,0,0)};p.Children.Add(ok);ok.Click+=(_,_)=>{if(!double.TryParse(width.Text,out double x)||!double.IsFinite(x)||x<480||x>900){MessageBox.Show(T("Invalid values"));return;}r.Name=name.Text.Trim();var g=Layout.Group(S,r);Layout.Arrange(g,g[0].X,g[0].Y,x);Sync();Save();w.Close();};w.ShowDialog();}
 public void Reorder(Row r,int delta){var g=Layout.Group(S,r);int i=g.IndexOf(r),j=i+delta;if(j<0||j>=g.Count)return;(g[i],g[j])=(g[j],g[i]);Layout.Arrange(g,g[0].X,g.Min(x=>x.Y),r.Width);Sync();Save();}
 public void Remove(Row r){Layout.Detach(S,r);S.Rows.Remove(r);Bars[r.Id].Close();Bars.Remove(r.Id);Sync();Save();}
 public void Quit(){if(!Save())return;timer.Stop();environmentTimer.Stop();SystemEvents.TimeChanged-=TimeChanged;SystemEvents.PowerModeChanged-=PowerChanged;SystemEvents.DisplaySettingsChanged-=ScreensChanged;Exiting=true;Tray.Dispose();Disk.Dispose();Application.Current.Shutdown();}
}
