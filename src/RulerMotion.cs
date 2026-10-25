using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms=System.Windows.Forms;
namespace WorldTime;
public enum RulerMotionMode { Dynamic, LowMotion, Static }
// One timer per ruler. Immutable static pixels are reused between minute/interaction rebuilds.
public sealed partial class RulerWindow {
 readonly DispatcherTimer motionTimer=new(DispatcherPriority.Background){Interval=TimeSpan.FromMilliseconds(1000d/6)};
 readonly Stopwatch motionClock=Stopwatch.StartNew();
 readonly RulerMotionPlane motionPlane=new();
 BitmapSource? staticRaster;double staticRasterScale;long sceneRevision,staticRasterRevision=-1;
 bool motionInitialized,motionDisposed,motionFaulted;double motionSeconds,motionCost,policyAt=-10;int motionFrames,staticRasters,motionTransitions;
 bool systemMotion=true,battery;int slowFrames;RulerMotionMode? policyOverride;
 public RulerMotionMode MotionMode=>(RulerMotionMode)view.Motion;
 public RulerMotionMode EffectiveMotionMode=>MotionMode==RulerMotionMode.Dynamic&&view.FollowSystemMotion&&!systemMotion?RulerMotionMode.LowMotion:MotionMode;
 public bool FollowSystemMotion=>view.FollowSystemMotion;
 public bool MotionRunning=>motionTimer.IsEnabled;
 public int MotionFrames=>motionFrames;public int StaticRasterCount=>staticRasters;public int MotionTransitions=>motionTransitions;
 public double MotionRenderMilliseconds=>motionCost;
 public double MotionFps=>1000/motionTimer.Interval.TotalMilliseconds;
 public string MotionError {get;private set;}="";
 public string SelfName=>view.SelfName;
 internal BitmapSource? LastMotionBitmap {get;private set;}
 internal void InitializeMotion(){
  if(motionInitialized)return;motionInitialized=true;motionPlane.IsHitTestVisible=false;motionPlane.Render=DrawMotion;
  Content=null;var layers=new System.Windows.Controls.Grid();layers.Children.Add(root);layers.Children.Add(motionPlane);Content=layers;
  motionTimer.Tick+=(_,_)=>MotionTick();Closed+=(_,_)=>DisposeMotion();
  SystemParameters.StaticPropertyChanged+=MotionPreferenceChanged;UpdateMotionPolicy();
 }
 void MotionPreferenceChanged(object? sender,System.ComponentModel.PropertyChangedEventArgs e){if(!Dispatcher.CheckAccess()){Dispatcher.BeginInvoke(new Action(()=>MotionPreferenceChanged(sender,e)));return;}if(motionDisposed)return;policyAt=-10;UpdateMotionPolicy();SyncMotion();motionPlane.InvalidateVisual();Desktop.Refresh();}
 internal void InvalidateMotionRaster(){sceneRevision++;staticRaster=null;motionPlane.InvalidateVisual();}
 void UpdateMotionPolicy(){
  if(policyOverride.HasValue){systemMotion=policyOverride.Value==RulerMotionMode.Dynamic;battery=false;return;}
  double now=motionClock.Elapsed.TotalSeconds;if(now-policyAt<5)return;policyAt=now;
  try{systemMotion=SystemParameters.ClientAreaAnimation;}catch{systemMotion=false;}
  try{battery=Forms.SystemInformation.PowerStatus.PowerLineStatus==Forms.PowerLineStatus.Offline;}catch{battery=true;}
 }
 internal void SyncMotion(){
  if(!motionInitialized||motionDisposed)return;UpdateMotionPolicy();
  bool skyMotion=NatureEnabled&&BackgroundOpacity>0;
  bool personMotion=host.S.Life&&System.Linq.Enumerable.Any(Clusters,c=>{var p=c.Members[0];var stage=RulerLifeStage.At(p,host.S).Stage;return stage!=LifeStage.Neutral&&(stage!=LifeStage.Waking||!stageMemory.TryGetValue(p.Id,out var seen)||motionClock.Elapsed.TotalSeconds-seen.Since<3);});
  bool animate=desiredVisible&&!host.Exiting&&!motionFaulted&&(skyMotion||personMotion)&&EffectiveMotionMode==RulerMotionMode.Dynamic&&!ModalBusy&&openMenu?.IsOpen!=true;
  if(animate){if(!motionTimer.IsEnabled)motionTimer.Start();}else motionTimer.Stop();
 }
 internal void DisposeMotion(){if(motionDisposed)return;motionDisposed=true;motionTimer.Stop();SystemParameters.StaticPropertyChanged-=MotionPreferenceChanged;staticRaster=null;}
 void MotionTick(){
  SyncMotion();if(!motionTimer.IsEnabled)return;
  // Gestures have priority. A skipped frame never replays or accumulates a backlog.
  if(floatingResizeStart.HasValue||root.IsMouseCaptured||Desktop.Surface?.Interacting==true)return;
  var elapsed=Stopwatch.StartNew();
  try{motionSeconds=motionClock.Elapsed.TotalSeconds;motionPlane.InvalidateVisual();if(Desktop.Hosted)Desktop.Refresh();motionFrames++;}
  catch(Exception e){MotionError=e.Message;motionFaulted=true;motionTimer.Stop();}
  elapsed.Stop();motionCost=elapsed.Elapsed.TotalMilliseconds;
  if(motionCost>25)slowFrames++;else slowFrames=Math.Max(0,slowFrames-1);
  int fps=battery||slowFrames>=3?3:6;if(slowFrames>=8)fps=1;
  motionTimer.Interval=TimeSpan.FromMilliseconds(1000d/fps);
 }
 public bool SetMotionMode(RulerMotionMode mode){
  if(!Enum.IsDefined(mode))throw new ArgumentOutOfRangeException(nameof(mode));int previous=view.Motion;bool previousFollow=view.FollowSystemMotion;view.Motion=(int)mode;if(mode==RulerMotionMode.Dynamic)view.FollowSystemMotion=false;
  if(!SaveView("animation-mode")){view.Motion=previous;view.FollowSystemMotion=previousFollow;return false;}
  motionFaulted=false;MotionError="";slowFrames=0;motionSeconds=0;Refresh(force:true);SyncMotion();return true;
 }
 public bool SetFollowSystemMotion(bool follow){bool previous=view.FollowSystemMotion;view.FollowSystemMotion=follow;if(!SaveView("follow-system-motion")){view.FollowSystemMotion=previous;return false;}Refresh(force:true);SyncMotion();return true;}
 internal void SetMotionPolicyForTest(RulerMotionMode? mode){policyOverride=mode;policyAt=-10;UpdateMotionPolicy();SyncMotion();}
 internal void SetMotionFrameForTest(double seconds){motionSeconds=seconds;motionPlane.InvalidateVisual();}
 internal BitmapSource StaticMotionRaster(double factor){
  if(staticRaster!=null&&staticRasterRevision==sceneRevision&&Math.Abs(staticRasterScale-factor)<.001)return staticRaster;
  root.Measure(new Size(Width,double.PositiveInfinity));root.Arrange(new Rect(0,0,Width,root.DesiredSize.Height));root.UpdateLayout();
  var image=new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth*factor),(int)Math.Ceiling(root.ActualHeight*factor),96*factor,96*factor,PixelFormats.Pbgra32);image.Render(root);image.Freeze();staticRaster=image;staticRasterScale=factor;staticRasterRevision=sceneRevision;staticRasters++;return image;
 }
 internal BitmapSource RenderMotionNative(double factor){
  var background=StaticMotionRaster(factor);var drawing=new DrawingVisual();using(var dc=drawing.RenderOpen()){dc.DrawImage(background,new Rect(0,0,background.Width,background.Height));DrawMotion(dc);}
  var image=new RenderTargetBitmap(background.PixelWidth,background.PixelHeight,96*factor,96*factor,PixelFormats.Pbgra32);image.Render(drawing);image.Freeze();LastMotionBitmap=image;return image;
 }
 string LifeDescription(RulerPerson p){var estimate=RulerLifeStage.At(p,host.S);return host.T(estimate.Key)+"\n"+host.T(estimate.Basis);}
 public bool RenameSelf(string name){
  ValidateDisplayName(name);string previous=view.SelfName;view.SelfName=name.Trim();if(!SaveView("self-name")){view.SelfName=previous;return false;}Refresh(force:true);return true;
 }
 static void ValidateDisplayName(string name){if(name==null||name.Length>80||name.Any(char.IsControl))throw new ArgumentOutOfRangeException(nameof(name));}
 internal void EditDisplayName(Row? row=null)=>ShowTransient(new RulerNameDialog(host,this,row));
 internal void OpenMotionSettings(System.Windows.Controls.ItemsControl parent,Action<System.Windows.Controls.ItemsControl,string,Action,bool?> add){
  void Select(RulerMotionMode mode){if(!SetMotionMode(mode))MessageBox.Show(host.T("Could not save animation settings. The previous mode was kept."),"WorldTime");}
  add(parent,"Dynamic",()=>Select(RulerMotionMode.Dynamic),MotionMode==RulerMotionMode.Dynamic);
  add(parent,"Low motion",()=>Select(RulerMotionMode.LowMotion),MotionMode==RulerMotionMode.LowMotion);
  add(parent,"Completely static",()=>Select(RulerMotionMode.Static),MotionMode==RulerMotionMode.Static);
  add(parent,"Follow Windows motion preference",()=>{if(!SetFollowSystemMotion(!FollowSystemMotion))MessageBox.Show(host.T("Could not save animation settings. The previous mode was kept."),"WorldTime");},FollowSystemMotion);
 }
 sealed class RulerMotionPlane : FrameworkElement {public Action<DrawingContext>? Render;protected override void OnRender(DrawingContext dc)=>Render?.Invoke(dc);}
}
