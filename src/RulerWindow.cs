using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;

namespace WorldTime;
// Transparent ruler visual; independent native bridge, separate ordinary detail windows.
public sealed partial class RulerWindow : Window {
 readonly Host host;readonly StackPanel root=new();
 readonly Canvas people=new(),scale=new(),dates=new();Window? detailWindow,tipWindow;ContextMenu? openMenu;TextBlock? detailText;string groupId="",hoverId="",hoverTick="";readonly Dictionary<string,Button> detailMembers=new();Point? flyoutPoint;bool desiredVisible,changingVisibility;
 readonly HashSet<Window> transients=new();RulerViewPreferences view;readonly string? viewPath;bool viewWritable=true;
 public RulerDesktopBridge Desktop {get;}
 internal Window? TransientOwner {get;set;}
 public string ViewError {get;private set;}="";public string AuditError {get;private set;}="";
 public bool RulerVisible=>desiredVisible;
 readonly DispatcherTimer resize=new(){Interval=TimeSpan.FromMilliseconds(100)};
 string selected="",key="",language="";double trackWidth=640;bool built;const double BandOrigin=44;double Origin=>BandOrigin+trackWidth/24;double SkyWidth=>trackWidth*26/24;
 DateTimeOffset frameInstant;TimeZoneInfo? frameViewer;List<RulerPerson> snapshots=new();Dictionary<string,NatureInfo> nature=new();
 public IReadOnlyList<RulerPlacement> Placements {get;private set;}=Array.Empty<RulerPlacement>();
 public IReadOnlyList<RulerCluster> Clusters {get;private set;}=Array.Empty<RulerCluster>();
 public Dictionary<string,Button> PersonButtons {get;}=new();
 public string SelectedId=>selected;
 public bool Compact {get;private set;}
 public bool NatureEnabled {get;private set;}=true;
 public int RenderCount {get;private set;}
 public FrameworkElement? PopupContent=>detailWindow?.Content as FrameworkElement;
 public Window? DetailWindow=>detailWindow;
 public FrameworkElement Visual=>root;
 public IReadOnlyList<RulerDateGroup> DateGroups {get;private set;}=Array.Empty<RulerDateGroup>();
 public double TrackWidth=>trackWidth;
 public double AvatarGap=>2;
 public double ScaleTop=>people.Height;
 public double DateTop=>IsRing?0:people.Height+scale.Height;
 public Rect BodyBounds=>IsRing?VisibleBody.Bounds:new(6,people.Height,BandOrigin+SkyWidth-6,40);
 public Rect DragBand=>IsRing?VisibleBody.Bounds:new(6,people.Height,BandOrigin-6,40);
 internal ContextMenu? NativeMenu=>openMenu;
 public double CoreOrigin=>Origin;
 internal bool ModalBusy=>transients.Count>0;
 public bool ActualPinned=>Desktop.Hosted&&Desktop.Surface?.Pinned==true;
 public double BackgroundOpacity=>view.BackgroundOpacity;
 public bool ShowDates=>IsRing?view.RingDates!=2:view.Dates;
 public RulerAvatarStore Avatars {get;}
 public RulerWindow(Host host){
  this.host=host;
  viewPath=host.RulerPreviewOnly?System.IO.Path.Combine(host.Disk.DirectoryPath,"ruler-view.json"):null;
  try{view=viewPath==null?new(){Desktop=false}:RulerViewPreferences.Load(viewPath,host.RulerDesktopRequested);}catch(Exception e){view=new(){Desktop=host.RulerDesktopRequested};ViewError=e.Message;viewWritable=false;}
  Avatars=new RulerAvatarStore(host.Disk.DirectoryPath);
  Compact=view.Compact;NatureEnabled=view.Nature;Width=view.Width;MinWidth=300;MaxWidth=1100;Title="Gravity WorldTime 0.3.1 · "+host.T("Time ruler (preview)");SizeToContent=SizeToContent.Height;MaxHeight=650;
  WindowStyle=WindowStyle.None;AllowsTransparency=true;ShowActivated=false;ShowInTaskbar=false;WindowStartupLocation=WindowStartupLocation.Manual;Left=view.X;Top=view.Y;ResizeMode=ResizeMode.NoResize;FontFamily=host.Manager.FontFamily;FontSize=12;Background=Brushes.Transparent;Foreground=Brush("#24354B");Content=root;
  Desktop=new RulerDesktopBridge(this,new Point(view.X,view.Y));
  InitializeMotion();
  people.HorizontalAlignment=scale.HorizontalAlignment=dates.HorizontalAlignment=HorizontalAlignment.Left;
  resize.Tick+=(_,_)=>{resize.Stop();if(desiredVisible)Refresh(force:true);};SizeChanged+=(_,e)=>{if(e.WidthChanged&&desiredVisible){resize.Stop();resize.Start();}};
  Loaded+=(_,_)=>Refresh(force:true);
  Closing+=(_,e)=>{if(!host.Exiting){e.Cancel=true;Conceal();}else{Desktop.Dispose();CloseDetail();HideNativeTip();}};
  root.LostMouseCapture+=(_,_)=>EndFloatingResize();root.PreviewMouseLeftButtonDown+=ResizePointerDown;root.PreviewMouseMove+=ResizePointerMove;root.PreviewMouseLeftButtonUp+=ResizePointerUp;root.MouseMove+=(_,e)=>{if(!floatingResizeStart.HasValue)NativePointerMoved(e.GetPosition(root));};root.MouseLeave+=(_,_)=>NativePointerLeft();
  root.MouseRightButtonUp+=(_,e)=>{OpenNativeMenu(RulerNativeSurface.Cursor());e.Handled=true;};
  root.MouseLeftButtonDown+=(_,e)=>{if(!Desktop.Hosted&&NativeCanDrag(e.GetPosition(root))){HideNativeTip();DragMove();var bounds=DockScreen.Bounds(this);Desktop.SetPosition(bounds.TopLeft);SaveView();e.Handled=true;}};
 }

 static SolidColorBrush Brush(string value)=>new((Color)ColorConverter.ConvertFromString(value));
 TextBlock Text(string value,double size=12,string color="#2B4058")=>new(){Text=value,FontSize=size,Foreground=Brush(color),TextWrapping=TextWrapping.Wrap};
 Button Action(string label,System.Action action,bool icon=false){var b=new Button{Content=label,Padding=new Thickness(icon?5:9,icon?2:5,icon?5:9,icon?2:5),Margin=new Thickness(0,0,3,0),Background=Brush("#E7ECF3"),Foreground=Brush("#324963"),BorderThickness=new Thickness(0),MinWidth=icon?28:0,MinHeight=icon?24:0,ToolTip=label};AutomationProperties.SetName(b,label);b.Click+=(_,_)=>action();return b;}
 Button Glyph(string glyph,string label,System.Action action){var b=Action(host.T(label),action,true);b.Content=glyph;return b;}
 void Build(){root.Children.Clear();language=host.Language;if(IsRing)root.Children.Add(ring);else{root.Children.Add(people);root.Children.Add(scale);root.Children.Add(dates);}built=true;}
 public void OpenNativeMenu(Point screen)=>BuildNativeMenu(screen);
 string AvatarId(RulerPerson p)=>p.Self?"$self":p.Id;
 public void ChooseAvatar(string id){HideNativeTip();ShowTransient(new RulerAvatarDialog(host,Avatars,id,()=>Refresh(force:true)));}
 public void OpenAppearance()=>ShowTransient(new RulerAppearanceDialog(host,this,true));
 public void AddContact()=>ShowTransient(new RulerContactDialog(host,this));
 public void EditRoutine(Row row)=>ShowTransient(new RoutineEditor(host,row));
 void ShowTransient(Window dialog){EndFloatingResize();NativePointerLeft();if(TransientOwner!=null){dialog.Owner=TransientOwner;dialog.WindowStartupLocation=WindowStartupLocation.CenterOwner;dialog.ShowInTaskbar=true;}dialog.Topmost=ActualPinned;transients.Add(dialog);SyncMotion();try{dialog.ShowDialog();}finally{transients.Remove(dialog);SyncMotion();}}
 internal void NativeModeChanged(){if(host.Exiting)return;if(host.RulerPreviewOnly){host.Manager.Topmost=ActualPinned;host.Manager.RefreshLayerStatus();}if(detailWindow!=null)detailWindow.Topmost=ActualPinned;foreach(var dialog in transients)dialog.Topmost=ActualPinned;HideNativeTip();if(openMenu!=null)openMenu.IsOpen=false;host.Tray.Refresh();if(desiredVisible)Refresh(force:true);}
 internal void ConfirmRemove(Row row){bool confirmed=false;var dialog=host.Dialog(host.T("Remove"));var panel=(StackPanel)dialog.Content;panel.Children.Add(Text(host.T("Remove this contact from the ruler?")));panel.Children.Add(Text(L10n.CityName(row,host.S.Language),14));var actions=new WrapPanel{Margin=new Thickness(0,14,0,0)};actions.Children.Add(Action(host.T("Remove"),()=>{confirmed=true;dialog.Close();}));actions.Children.Add(Action(host.T("Cancel"),dialog.Close));panel.Children.Add(actions);ShowTransient(dialog);if(confirmed&&!host.Exiting&&host.S.Rows.Contains(row)){CloseDetail();host.Remove(row);}}
 public void SetPinned(bool value){EndFloatingResize();view.Pinned=value;view.Desktop=true;desiredVisible=true;Desktop.Show(true,value);if(detailWindow!=null)detailWindow.Topmost=ActualPinned;Refresh(force:true);SaveView();}
 public void SetBackgroundOpacity(double opacity,bool save=true){if(!double.IsFinite(opacity)||opacity<0||opacity>1)throw new ArgumentOutOfRangeException(nameof(opacity));view.BackgroundOpacity=opacity;Refresh(force:true);if(save)SaveView();}
 public void SetDates(bool show,bool save=true){NativePointerLeft();view.Dates=show;Refresh(force:true);if(save)SaveView();}
 public void ResetPosition(){var area=DockScreen.Find("");Desktop.SetPosition(new Point(area.Left+30,area.Top+80));desiredVisible=true;Desktop.Show(view.Desktop,view.Pinned);SaveView();}
 public void FitToWorkArea(Rect area){double available=Math.Max(200,area.Width-20);MinWidth=Math.Min(300,available);Width=Math.Clamp(Width,MinWidth,Math.Min(MaxWidth,available));MaxHeight=Math.Max(140,area.Height-20);Left=Math.Clamp(Left,area.Left,Math.Max(area.Left,area.Right-Width));Top=Math.Clamp(Top,area.Top,Math.Max(area.Top,area.Bottom-ActualHeight));}
 internal void FitNativeWidth(double available)=>FitNativeSize(available,650);
 internal void FitNativeSize(double available,double height){if(IsRing&&Math.Min(available,height)<RingLayout.MinSize+20)SmallScreenFallback();double limit=IsRing?Math.Min(available,height):available;MinWidth=Math.Min(IsRing?RingLayout.MinSize:300,Math.Max(200,limit-20));double previous=Width;Width=Math.Clamp(Width,MinWidth,Math.Min(1100,Math.Max(MinWidth,limit-20)));if(Math.Abs(previous-Width)>.1)Refresh(force:true);}
 public void Reveal(){desiredVisible=true;Refresh(force:true);Desktop.Show(view.Desktop,view.Pinned);if(!view.Desktop)ShowFloating(true);SyncMotion();}
 public void Conceal(){EndFloatingResize();NativePointerLeft();desiredVisible=false;SyncMotion();Desktop.Hide();HideFloating();CloseDetail();HideNativeTip();}
 public void SetDesktop(bool value){EndFloatingResize();view.Desktop=value;view.Pinned=false;desiredVisible=true;HideNativeTip();Desktop.Show(value);if(detailWindow!=null)detailWindow.Topmost=false;SaveView();}
 internal void ShowFloating(bool userAction){if(changingVisibility)return;changingVisibility=true;try{var area=DockScreen.AtPoint(Desktop.Position);FitNativeSize((area.Right-area.Left)/area.Scale,(area.Bottom-area.Top)/area.Scale);Left=Desktop.Position.X/area.Scale;Top=Desktop.Position.Y/area.Scale;if(!IsVisible)Show();WindowState=WindowState.Normal;Refresh(force:true);UpdateLayout();var size=new Size(ActualWidth*area.Scale,ActualHeight*area.Scale);var at=RulerDesktopBridge.Clamp(Desktop.Position,size.Width,size.Height,area);DockScreen.Place(this,new Rect(at,size));if(userAction)Activate();}finally{changingVisibility=false;}}
 internal void HideFloating(){if(changingVisibility)return;changingVisibility=true;try{if(IsVisible)Hide();}finally{changingVisibility=false;}}
 public void SetCompact(bool value){Compact=value;Width=IsRing?(value?440:560):(value?560:720);Refresh(force:true);SaveView();}
 public void SetNature(bool value){NatureEnabled=value;Refresh(force:true);SaveView();}
 public bool SaveView([System.Runtime.CompilerServices.CallerMemberName]string reason=""){if(viewPath==null)return true;if(!viewWritable)return false;try{string previous=RulerViewAudit.Hash(viewPath);RememberLayout();view.X=Desktop.Position.X;view.Y=Desktop.Position.Y;view.Width=Width;view.Compact=Compact;view.Nature=NatureEnabled;view.Save(viewPath);ViewError="";AuditError=RulerViewAudit.Record(viewPath,previous,reason);host.Tray.Refresh();return true;}catch(Exception e){ViewError=e.Message;return false;}}
 public BitmapSource RenderNative(double scaleFactor)=>RenderMotionNative(scaleFactor);
 public bool NativeCanDrag(Point local)=>!ModalBusy&&DragBand.Contains(local)&&VisibleBody.FillContains(local)&&!NativeCanResize(local);
 RulerCluster? ClusterAt(Point local)=>IsRing?ringNodes.FirstOrDefault(n=>new Rect(n.Center.X-(n.Collapsed?14:18),n.Center.Y-(n.Collapsed?14:18),n.Collapsed?28:36,n.Collapsed?28:36).Contains(local)||(!n.Label.IsEmpty&&n.Label.Contains(local)))?.Cluster:Clusters.FirstOrDefault(c=>new Rect(Origin+c.X-(AvatarSize+4)/2,AvatarTop,AvatarSize+4,AvatarSize+4).Contains(local)||new Rect(Origin+c.CardLeft,0,LabelWidth,AvatarTop).Contains(local));
 public void NativeClick(Point local,Point screen){if(ModalBusy)return;if(DateAt(local) is { } label){flyoutPoint=screen;OpenCluster(label.Cluster,PersonButtons[label.Cluster.Members[0].Id]);return;}var c=ClusterAt(local);if(c!=null){flyoutPoint=screen;OpenCluster(c,PersonButtons[c.Members[0].Id]);}}
 public void NativePointerMoved(Point local){lastPointer=local;if(ModalBusy){NativePointerLeft();return;}SetHandleHover(NativeCanDrag(local));SetResizeHover(NativeCanResize(local));string next=ClusterAt(local)?.Members[0].Id??"";if(next!=hoverId){HideNativeTip();hoverId=next;}SetDateHover(DateAt(local));string tick=TickTip(local);if(tick!=hoverTick){HideNativeTip();hoverTick=tick;}}
 public void NativePointerLeft(){lastPointer=null;HideNativeTip();hoverId="";hoverTick="";SetHandleHover(false);SetResizeHover(false);ClearDateHover();}
 public void NativeHover(Point local,Point screen){if(ModalBusy||detailWindow?.IsVisible==true||tipWindow?.IsVisible==true||openMenu?.IsOpen==true)return;string text=DateAt(local) is { } label?DateTip(label):ClusterAt(local) is { } c?ClusterTip(c):NativeCanResize(local)?host.T(IsRing?"Drag the faint arrow handle outward or inward to resize the ring.":"Drag the right end to resize."):IsRing&&TickTip(local).Length>0?TickTip(local)+"\n"+MoveHelp:NativeCanDrag(local)?MoveHelp:TickTip(local);if(text.Length==0)return;HideNativeTip();tipWindow=new RulerTipWindow{IsHitTestVisible=false,Focusable=false,WindowStyle=WindowStyle.None,ResizeMode=ResizeMode.NoResize,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=false,ShowActivated=false,Topmost=ActualPinned,SizeToContent=SizeToContent.WidthAndHeight,Content=new Border{Padding=new Thickness(9),Background=Brush("#F4F6FA"),BorderBrush=Brush("#AAB9CC"),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(5),Child=new TextBlock{Text=text,MaxWidth=340,TextWrapping=TextWrapping.Wrap,FontFamily=FontFamily,FontSize=12,Foreground=Brush("#24354B")}}};if(TransientOwner!=null)tipWindow.Owner=TransientOwner;try{ShowAt(tipWindow,new Point(screen.X+12,screen.Y+18));}catch(Exception e){HideNativeTip();WindowLayer.Diagnostics?.Invoke("Read-only tip suppressed: "+e.Message);}}
 public void HideNativeTip(){tipWindow?.Close();tipWindow=null;}
 void ShowAt(Window window,Point point){var area=DockScreen.AtPoint(point);window.Left=point.X/area.Scale;window.Top=point.Y/area.Scale;window.Show();window.UpdateLayout();var dpi=VisualTreeHelper.GetDpi(window).DpiScaleX;double width=window.ActualWidth*dpi,height=window.ActualHeight*dpi;var p=RulerDesktopBridge.Clamp(point,width,height,area);DockScreen.Place(window,new Rect(p,new Size(width,height)));}
 public void Refresh(DateTimeOffset? instant=null,TimeZoneInfo? viewer=null,bool force=false){
  if(!desiredVisible&&!IsVisible&&!force)return;var now=instant??DateTimeOffset.UtcNow;string next=view.Display+"/"+now.UtcDateTime.ToString("yyyyMMddHHmm",CultureInfo.InvariantCulture)+host.Language+host.S.Twelve+view.RingDates+Compact+NatureEnabled+Width+view.BackgroundOpacity+view.Dates+ActualPinned+string.Join("|",host.S.Rows.Select(r=>r.Id+"/"+r.Name+"/"+r.Zone+"/"+r.City+"/"+r.Code+"/"+r.Routine?.GetHashCode()))+host.S.Start+host.S.End+host.S.Lunch+host.S.Weekend+host.S.Life+host.S.Advice+view.SelfName+view.Motion+(viewer??TimeZoneInfo.Local).Id;
  if(!force&&next==key)return;key=next;frameInstant=now;frameViewer=viewer;RenderCount++;if(!built||language!=host.Language)Build();Title="Gravity WorldTime 0.3.1 · "+host.T("Time ruler (preview)");
  // One bounded lane: nearby contacts aggregate instead of hanging above the ruler.
  trackWidth=RulerAxis.CoreWidth(Math.Max(96,Width-68));double cardWidth=LabelWidth;snapshots=RulerModel.Snapshot(host.S,now,viewer,view.SelfName);RememberLifeStages();nature=snapshots.ToDictionary(p=>p.Id,p=>NatureModel.At(p,now));Clusters=IsRing?RingLayout.Group(snapshots,RingLayout.PersonRadius(Width),trackWidth,Clusters,ShowRingDate):CompactRulerLayout.Arrange(snapshots,trackWidth,cardWidth,1);ringNodes=IsRing?RingLayout.Place(Clusters,Width,ShowRingDate):Array.Empty<RingNode>();Placements=Clusters.SelectMany(c=>c.Members.Select(p=>new RulerPlacement(p,p.Fraction*trackWidth,c.CardLeft,0))).ToArray();DateGroups=RulerDateBand.Group(snapshots,trackWidth);
  DrawFrame();
  if(selected.Length>0&&!snapshots.Any(p=>p.Id==selected)){selected="";CloseDetail();}
  RefreshDetail();if(tipWindow?.Content is Border tipBorder&&tipBorder.Child is TextBlock tipText&&Clusters.FirstOrDefault(c=>c.Members[0].Id==hoverId) is { } tipCluster)tipText.Text=ClusterTip(tipCluster);else if(tipWindow?.Content is Border dateTipBorder&&dateTipBorder.Child is TextBlock dateTipText&&PersonDates.FirstOrDefault(g=>g.Cluster.Members[0].Id==HoveredPersonId) is { } activeDate)dateTipText.Text=DateTip(activeDate);Desktop.Refresh();SyncMotion();
 }
 void DrawSky(){
  var gradient=new LinearGradientBrush{StartPoint=new Point(0,0),EndPoint=new Point(1,0)};
  var palette=new[]{(0d,"#101C3B"),(.16,"#263458"),(.23,"#9D92AF"),(.28,"#E8BE86"),(.34,"#BBDCEC"),(.50,"#C4E6F3"),(.65,"#B9DCEA"),(.74,"#EDBC83"),(.81,"#AD7B9B"),(.89,"#333858"),(1d,"#131E3A")};
  double bodyWidth=BodyBounds.Width,padding=BandOrigin-BodyBounds.Left;
  for(int hour=-1;hour<=25;hour++){double fraction=((hour%24+24)%24)/24d;int index=0;while(index<palette.Length-2&&palette[index+1].Item1<fraction)index++;var a=(Color)ColorConverter.ConvertFromString(palette[index].Item2);var b=(Color)ColorConverter.ConvertFromString(palette[index+1].Item2);double t=(fraction-palette[index].Item1)/(palette[index+1].Item1-palette[index].Item1);var color=Color.FromArgb((byte)Math.Round(255*view.BackgroundOpacity),(byte)(a.R+(b.R-a.R)*t),(byte)(a.G+(b.G-a.G)*t),(byte)(a.B+(b.B-a.B)*t));if(hour==-1)gradient.GradientStops.Add(new GradientStop(color,0));gradient.GradientStops.Add(new GradientStop(color,(padding+(hour+1)/26d*SkyWidth)/bodyWidth));}gradient.Freeze();
  var sky=new Canvas{Width=bodyWidth,Height=40,IsHitTestVisible=false,Clip=new RectangleGeometry(new Rect(0,0,bodyWidth,40),20,20)};sky.Children.Add(new Rectangle{Width=bodyWidth,Height=40,Fill=gradient});Canvas.SetLeft(sky,BodyBounds.Left);scale.Children.Add(sky);
  var day=new Canvas{Width=SkyWidth,Height=40,IsHitTestVisible=false};Canvas.SetLeft(day,padding);sky.Children.Add(day);
  // Frozen vector symbols are ambience, not weather or a shared sunrise calculation.
 }
 string TimeRange(RulerCluster c){var ordered=c.Members.OrderBy(p=>p.Fraction).ToArray();string a=ordered[0].Time(host.S.Twelve),b=ordered[^1].Time(host.S.Twelve);return a==b?a:a+"–"+b;}
 static ControlTemplate AvatarFrame(){var template=new ControlTemplate(typeof(Button));var frame=new FrameworkElementFactory(typeof(Border));frame.SetValue(Border.CornerRadiusProperty,new CornerRadius(24));foreach(var property in new[]{Border.BackgroundProperty,Border.BorderBrushProperty,Border.BorderThicknessProperty})frame.SetBinding(property,new System.Windows.Data.Binding(property.Name){RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent)});var content=new FrameworkElementFactory(typeof(ContentPresenter));content.SetValue(FrameworkElement.HorizontalAlignmentProperty,HorizontalAlignment.Center);content.SetValue(FrameworkElement.VerticalAlignmentProperty,VerticalAlignment.Center);frame.AppendChild(content);template.VisualTree=frame;return template;}
 string PersonDetail(RulerPerson p)=>RulerModel.Detail(p,host.S)+(IsRing?"\n"+ClockPeriod.Caption(p.Local,host.S.Language,host.S.Twelve):"")+"\n\n"+LifeDescription(p)+"\n\n"+LifeDetail(p)+(NatureEnabled?"\n\n"+NatureModel.Detail(nature[p.Id],host.S.Language):"");
 string LifeDetail(RulerPerson p){if(p.Self||!host.S.Life)return "";return host.T(p.Status==host.T("May be resting")?"They may have turned in; a message can wait.":p.Contactable?"Their chosen contact window is open; actual availability is unknown.":p.Status==host.T("May be waking up")?"They may be starting their day. Give them a little time.":"Their routine suggests leaving a message for now.");}
 string ClusterTip(RulerCluster c)=>c.Members.Count==1?PersonDetail(c.Members[0]):host.T("Nearby local hours; click to see every person.")+"\n"+string.Join("\n",c.Members.Select(p=>p.Name+" · "+(IsRing?ClockPeriod.Caption(p.Local,host.S.Language,host.S.Twelve):p.Time(host.S.Twelve))+" · "+p.Date));
 void CloseDetail(){detailWindow?.Close();detailWindow=null;detailText=null;groupId="";detailMembers.Clear();}
 void OpenBody(UIElement body){HideNativeTip();CloseDetail();detailWindow=new Window{Title="WorldTime · "+host.T("Contact details"),ShowInTaskbar=false,Topmost=ActualPinned,Width=360,SizeToContent=SizeToContent.Height,ResizeMode=ResizeMode.NoResize,FontFamily=FontFamily,FontSize=12,Background=Brush("#FBFCFE"),Content=new Border{Padding=new Thickness(14),Child=body}};detailWindow.Closed+=(_,_)=>{detailWindow=null;detailText=null;groupId="";detailMembers.Clear();};if(TransientOwner!=null)detailWindow.Owner=TransientOwner;Point point=flyoutPoint??(IsVisible?PointToScreen(new Point(40,40)):Desktop.Position);ShowAt(detailWindow,new Point(point.X+8,point.Y+12));}
 void OpenCluster(RulerCluster c,Button target){if(c.Members.Count==1){SelectPerson(c.Members[0].Id);return;}selected="";var list=new StackPanel();var buttons=new Dictionary<string,Button>();list.Children.Add(Text(host.T("Nearby local hours; click to see every person."),11,"#73839A"));foreach(var p in c.Members){var person=p;var button=Action(p.Name+" · "+p.Time(host.S.Twelve)+" · "+p.Local.ToString("MM/dd"),()=>SelectPerson(person.Id));buttons[p.Id]=button;list.Children.Add(button);}OpenBody(new ScrollViewer{Content=list,MaxHeight=330,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});groupId=c.Members[0].Id;foreach(var pair in buttons)detailMembers[pair.Key]=pair.Value;}
 public void SelectPerson(string id){selected=id;var p=snapshots.FirstOrDefault(p=>p.Id==id);if(p==null)return;var body=new StackPanel{Width=300};var text=Text(PersonDetail(p),12);body.Children.Add(text);if(p.Row is Row row){var controls=new WrapPanel{Margin=new Thickness(0,12,0,0)};controls.Children.Add(Action(host.T("Contact name"),()=>{CloseDetail();Rename(row);}));controls.Children.Add(Action(host.T("City routine"),()=>{CloseDetail();ShowTransient(new RoutineEditor(host,row));}));controls.Children.Add(Action(host.T("Choose avatar"),()=>ChooseAvatar(row.Id)));controls.Children.Add(Action(host.T("Change city or time zone"),()=>{CloseDetail();ShowTransient(new RulerContactDialog(host,this,null,row));}));controls.Children.Add(Action(host.T("Remove"),()=>ConfirmRemove(row)));body.Children.Add(controls);}else if(p.Self){var controls=new WrapPanel{Margin=new Thickness(0,12,0,0)};controls.Children.Add(Action(host.T("My display name…"),()=>{CloseDetail();EditDisplayName();}));controls.Children.Add(Action(host.T("Choose avatar"),()=>ChooseAvatar("$self")));body.Children.Add(controls);}OpenBody(body);detailText=text;}
 void RefreshDetail(){if(detailWindow?.IsVisible!=true)return;if(detailText!=null&&snapshots.FirstOrDefault(p=>p.Id==selected) is { } p)detailText.Text=PersonDetail(p);foreach(var (id,button) in detailMembers){var member=snapshots.FirstOrDefault(p=>p.Id==id);button.IsEnabled=member!=null;if(member!=null)button.Content=member.Name+" · "+member.Time(host.S.Twelve)+" · "+member.Local.ToString("MM/dd");}}
 public bool RenameContact(Row row,string name){ValidateDisplayName(name);if(!host.S.Rows.Contains(row))throw new ArgumentException("Unknown contact",nameof(row));string previous=row.Name;row.Name=name.Trim();if(!host.Save()){row.Name=previous;host.Sync();return false;}host.Sync();return true;}
 internal void Rename(Row row)=>EditDisplayName(row);
}
