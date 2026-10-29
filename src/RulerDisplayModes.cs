using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace WorldTime;
public sealed partial class RulerWindow {
 readonly Canvas ring=new();IReadOnlyList<RingNode> ringNodes=Array.Empty<RingNode>();bool switchingMode;
 public WorldTimeView DisplayMode=>(WorldTimeView)view.Display;
 public bool IsRing=>DisplayMode==WorldTimeView.Ring;
 public IReadOnlyList<RingNode> RingNodes=>ringNodes;
 public string ModeError {get;private set;}="";
 public Geometry VisibleBody {get{if(!IsRing)return new RectangleGeometry(BodyBounds,20,20);double radius=RingLayout.Radius(Width);return Geometry.Combine(new EllipseGeometry(new Point(Width/2,Width/2),radius+RingLayout.HalfThickness(Width),radius+RingLayout.HalfThickness(Width)),new EllipseGeometry(new Point(Width/2,Width/2),Math.Max(1,radius-RingLayout.HalfThickness(Width)),Math.Max(1,radius-RingLayout.HalfThickness(Width))),GeometryCombineMode.Exclude,null);}}
 Point AvatarCenter(RulerCluster c)=>IsRing?ringNodes.First(n=>n.Cluster==c).Center:new(CoreOrigin+c.X,AvatarTop+2+AvatarSize/2);
 void RememberLayout(){view.X=Desktop.Position.X;view.Y=Desktop.Position.Y;view.Width=Width;var layout=new ViewLayout(view.X,view.Y,Width);if(IsRing)view.RingLayout=layout;else view.RulerLayout=layout;}
 internal Func<WorldTimeView,bool>? ModeGateForTest;
 public bool SetDisplayMode(WorldTimeView mode){
  if(!Enum.IsDefined(mode))throw new ArgumentOutOfRangeException(nameof(mode));if(switchingMode)return false;if(ModalBusy){ModeError=host.T("Close the open settings window before switching display mode.");return false;}if(mode==DisplayMode){Reveal();return true;}
  EndFloatingResize();RememberLayout();var backup=JsonSerializer.Deserialize<RulerViewPreferences>(JsonSerializer.Serialize(view))!;bool wasVisible=desiredVisible;switchingMode=true;ModeError="";
  try{CloseDetail();NativePointerLeft();if(openMenu!=null)openMenu.IsOpen=false;var next=mode==WorldTimeView.Ring?view.RingLayout:view.RulerLayout;next??=new ViewLayout(Desktop.Position.X,Desktop.Position.Y,mode==WorldTimeView.Ring?560:720);view.Display=(int)mode;MinWidth=200;Width=next.Size;view.Width=Width;Desktop.SetPosition(new Point(next.X,next.Y),false);desiredVisible=true;built=false;Refresh(force:true);
   if(ModeGateForTest?.Invoke(mode)==false)throw new InvalidOperationException("Display binding rejected by test gate");Desktop.Show(view.Desktop,view.Pinned);if(view.Desktop&&(!Desktop.Hosted||Desktop.BindingError.Length>0))throw new InvalidOperationException(Desktop.BindingError);if(!SaveView("display-mode"))throw new InvalidOperationException(ViewError);host.Tray.Refresh();return true;
  }catch(Exception e){ModeError=e.Message;view=backup;MinWidth=200;Width=backup.Width;Desktop.SetPosition(new Point(backup.X,backup.Y),false);desiredVisible=wasVisible;built=false;Refresh(force:true);if(wasVisible)Desktop.Show(view.Desktop,view.Pinned);else Conceal();host.Tray.Refresh();return false;
  }finally{switchingMode=false;SyncMotion();}
 }
 void SelectDisplayMode(WorldTimeView mode){if(!SetDisplayMode(mode))MessageBox.Show(host.T("Could not switch display mode. The previous view was restored.")+"\n"+ModeError,"WorldTime");}
 OutlinedLabel LightText(string text,double size,double width,double height,string color="#F4F7FC",bool strong=false)=>new(){Text=text,TextSize=size,Width=width,Height=height,Ink=Brush(color),Family=FontFamily,Strong=strong,IsHitTestVisible=false};
 StackPanel LightPersonLabel(RulerCluster c,double width){var p=c.Members[0];var panel=new StackPanel();panel.Children.Add(LightText(p.Name+(c.Members.Count>1?" +"+(c.Members.Count-1):""),10.5,width,15,p.Self?"#FFE0A1":"#F4F7FC",true));panel.Children.Add(LightText(p.Time(host.S.Twelve),12,width,17,"#F4F7FC",true));return panel;}
 bool ShowRingDate(RulerPerson p)=>view.RingDates==1||view.RingDates==0&&(p.Self||p.Date!=snapshots.FirstOrDefault(x=>x.Self)?.Date);
 public int RingDateMode=>view.RingDates;
 public bool SetRingDateMode(int value){if(value<0||value>2)throw new ArgumentOutOfRangeException(nameof(value));int old=view.RingDates;view.RingDates=value;Refresh(force:true);if(SaveView("ring-dates"))return true;view.RingDates=old;Refresh(force:true);return false;}
 bool IsCollapsedRingCluster(RulerCluster c)=>IsRing&&ringNodes.FirstOrDefault(n=>n.Cluster==c)?.Collapsed==true;
 string MoveHelp=>host.T(IsRing?"Drag the ring band to move. Drag the faint arrow handle outward or inward to resize.":"Drag the left end to move. Drag the right end to resize. Right-click for settings.");
 void SmallScreenFallback(){ModeError=host.T("The ring needs a work area of at least 440 by 440 DIP. The horizontal ruler remains available.");if(switchingMode)throw new InvalidOperationException(ModeError);RememberLayout();view.Display=0;var old=view.RulerLayout;MinWidth=200;Width=old?.Size??720;if(old!=null)Desktop.SetPosition(new Point(old.X,old.Y),false);built=false;Refresh(force:true);}
}
