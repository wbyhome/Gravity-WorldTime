using System;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Windows.Media;
namespace WorldTime;
public sealed partial class RulerWindow {
 Point? floatingResizeStart;Point floatingCenter;double floatingStartWidth,floatingScale;
 double ringGripHour=7.5;
 Point RingResizePoint=>RingLayout.PointAt(ringGripHour/24,RingLayout.Radius(Width)-4,new Point(Width/2,Width/2));
 void ChooseRingResizeGrip(){if(Desktop.Surface?.Interacting==true||floatingResizeStart.HasValue)return;foreach(double hour in new[]{ringGripHour,7.5,8.5,9.5,10.5,4.5}){var point=RingLayout.PointAt(hour/24,RingLayout.Radius(Width)-4,new Point(Width/2,Width/2));var hit=new Rect(point.X-26,point.Y-26,52,52);if(ringNodes.All(n=>!n.Footprint.IntersectsWith(hit))){ringGripHour=hour;return;}}}
 Geometry RingGripArc(){double radius=RingLayout.Radius(Width)+RingLayout.HalfThickness(Width)-3,half=14/(2*Math.PI*radius);var center=new Point(Width/2,Width/2);var g=new StreamGeometry();using(var dc=g.Open()){dc.BeginFigure(RingLayout.PointAt(ringGripHour/24-half,radius,center),false,false);dc.ArcTo(RingLayout.PointAt(ringGripHour/24+half,radius,center),new Size(radius,radius),0,false,SweepDirection.Clockwise,true,false);}g.Freeze();return g;}
 Geometry RingGripHit(){var g=new GeometryGroup{FillRule=FillRule.Nonzero};g.Children.Add(new EllipseGeometry(RingResizePoint,13,13));g.Children.Add(RingGripArc().GetWidenedPathGeometry(new Pen(Brushes.White,8)));return g;}
 public Rect ResizeBand=>IsRing?RingGripHit().Bounds:new(BodyBounds.Right-18,ScaleTop,18,40);
 public bool NativeCanResize(Point p)=>!ModalBusy&&ResizeBand.Contains(p)&&(IsRing?RingGripHit().FillContains(p):VisibleBody.FillContains(p))&&ClusterAt(p)==null;
 public double WidthLimit {get{var area=DockScreen.AtPoint(Desktop.Position);return Math.Clamp(IsRing?Math.Min(area.Right-area.Left,area.Bottom-area.Top)/area.Scale-20:(area.Right-Math.Max(area.Left,Desktop.Position.X))/area.Scale,200,1100);}}
 public void SetRulerWidth(double width,bool save=true){if(!double.IsFinite(width))throw new ArgumentOutOfRangeException(nameof(width));double limit=WidthLimit;MinWidth=Math.Min(IsRing?RingLayout.MinSize:300,limit);double next=Math.Clamp(width,MinWidth,limit);bool changed=Math.Abs(Width-next)>.25;if(changed){var area=DockScreen.AtPoint(Desktop.Position);double delta=(next-Width)*area.Scale/2;var previous=Desktop.Position;Width=next;if(IsRing)Desktop.SetPosition(new Point(previous.X-delta,previous.Y-delta),false);if(IsRing&&!Desktop.Hosted&&IsVisible){Left=Desktop.Position.X/area.Scale;Top=Desktop.Position.Y/area.Scale;}Refresh(force:true);resize.Stop();}if(save){if(IsRing&&!changed)Refresh(force:true);SaveView("resize-release");}}
 void SetPreset(double width,bool compact){if(IsRing){SetRulerWidth(compact?420:width>=1000?720:560);return;}Compact=compact;SetRulerWidth(width,false);Refresh(force:true);SaveView("size-preset");}
 void EndFloatingResize(){if(!floatingResizeStart.HasValue)return;floatingResizeStart=null;if(IsRing)Refresh(force:true);SaveView("resize-release");if(root.IsMouseCaptured)root.ReleaseMouseCapture();}
 void ResizePointerDown(object sender,MouseButtonEventArgs e){if(Desktop.Hosted||!NativeCanResize(e.GetPosition(root)))return;if(!root.CaptureMouse())return;floatingResizeStart=RulerNativeSurface.Cursor();floatingStartWidth=Width;floatingScale=DockScreen.AtPoint(Desktop.Position).Scale;floatingCenter=new Point(Desktop.Position.X+Width*floatingScale/2,Desktop.Position.Y+Width*floatingScale/2);HideNativeTip();e.Handled=true;}
 void ResizePointerMove(object sender,MouseEventArgs e){if(floatingResizeStart is Point start){if(e.LeftButton!=MouseButtonState.Pressed){EndFloatingResize();return;}SetRulerWidth(IsRing?RingLayout.RadialDiameter(floatingStartWidth,floatingCenter,start,RulerNativeSurface.Cursor(),floatingScale):floatingStartWidth+(RulerNativeSurface.Cursor().X-start.X)/floatingScale,false);e.Handled=true;}}
 void ResizePointerUp(object sender,MouseButtonEventArgs e){if(!floatingResizeStart.HasValue)return;EndFloatingResize();e.Handled=true;}
 internal void RestorePreviewPosition(Point p){Desktop.SetPosition(p);if(!Desktop.Hosted&&IsVisible){var area=DockScreen.AtPoint(p);Left=p.X/area.Scale;Top=p.Y/area.Scale;}Desktop.Refresh();}
}
