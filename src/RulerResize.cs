using System;
using System.Windows;
using System.Windows.Input;
namespace WorldTime;
public sealed partial class RulerWindow {
 Point? floatingResizeStart;double floatingStartWidth,floatingScale;
 public Rect ResizeBand=>new(BodyBounds.Right-18,ScaleTop,18,40);
 public bool NativeCanResize(Point p)=>!ModalBusy&&ResizeBand.Contains(p)&&new System.Windows.Media.RectangleGeometry(BodyBounds,20,20).FillContains(p);
 public double WidthLimit {get{var area=DockScreen.AtPoint(Desktop.Position);return Math.Clamp((area.Right-Math.Max(area.Left,Desktop.Position.X))/area.Scale,200,1100);}}
 public void SetRulerWidth(double width,bool save=true){if(!double.IsFinite(width))throw new ArgumentOutOfRangeException(nameof(width));double limit=WidthLimit;MinWidth=Math.Min(300,limit);double next=Math.Clamp(width,MinWidth,limit);if(Math.Abs(Width-next)>.25){Width=next;Refresh(force:true);resize.Stop();}if(save)SaveView("resize-release");}
 void SetPreset(double width,bool compact){Compact=compact;SetRulerWidth(width,false);Refresh(force:true);SaveView("size-preset");}
 void EndFloatingResize(){if(!floatingResizeStart.HasValue)return;floatingResizeStart=null;SaveView("resize-release");if(root.IsMouseCaptured)root.ReleaseMouseCapture();}
 void ResizePointerDown(object sender,MouseButtonEventArgs e){if(Desktop.Hosted||!NativeCanResize(e.GetPosition(root)))return;if(!root.CaptureMouse())return;floatingResizeStart=RulerNativeSurface.Cursor();floatingStartWidth=Width;floatingScale=DockScreen.AtPoint(Desktop.Position).Scale;HideNativeTip();e.Handled=true;}
 void ResizePointerMove(object sender,MouseEventArgs e){if(floatingResizeStart is Point start){if(e.LeftButton!=MouseButtonState.Pressed){EndFloatingResize();return;}SetRulerWidth(floatingStartWidth+(RulerNativeSurface.Cursor().X-start.X)/floatingScale,false);e.Handled=true;}}
 void ResizePointerUp(object sender,MouseButtonEventArgs e){if(!floatingResizeStart.HasValue)return;EndFloatingResize();e.Handled=true;}
}
