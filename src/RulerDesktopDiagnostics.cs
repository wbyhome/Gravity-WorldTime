using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace WorldTime;
// Explicit opt-in, bounded observation; no screenshots, input injection or foreign titles.
public static class RulerDesktopDiagnostics {
 public static void Start(Host host,string path){
  path=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path)!);string previous="";int ticks=0;var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(500)};
  timer.Tick+=(_,_)=>{
   if(host.Exiting||++ticks>1800){timer.Stop();return;}
   try{var r=host.Ruler;if(r==null)return;var s=r.Desktop.Surface;var b=s?.Bounds??Rect.Empty;var band=r.DragBand;var probe=r.IsRing?RingLayout.PointAt(.75,RingLayout.Radius(r.Width),new Point(r.Width/2,r.Width/2)):new Point(band.X+12,band.Y+8);var point=new Point(b.IsEmpty?0:b.X+probe.X*r.Desktop.Scale,b.IsEmpty?0:b.Y+probe.Y*r.Desktop.Scale);var transparent=new Point(b.IsEmpty?0:b.X+2,b.IsEmpty?0:b.Y+3);
    var data=new{Utc=DateTimeOffset.UtcNow,host.RulerPreviewOnly,host.RulerDesktopRequested,DisplayMode=r.DisplayMode.ToString(),r.RulerVisible,r.IsVisible,r.ActualPinned,r.Width,r.ModalBusy,r.ResizeHovered,r.HandleHovered,r.HandleHighlightChangeCount,r.HoveredDate,r.HighlightChangeCount,r.RenderCount,ChosenMotion=r.MotionMode.ToString(),EffectiveMotion=r.EffectiveMotionMode.ToString(),r.FollowSystemMotion,r.MotionRunning,r.MotionFps,r.MotionFrames,r.StaticRasterCount,r.MotionTransitions,r.MotionRenderMilliseconds,r.MotionError,r.AuditError,PersonDates=r.PersonDates.Select(label=>new{label.Person.Id,label.Date,X=label.Bounds.X+label.Bounds.Width/2,Y=label.Bounds.Y+label.Bounds.Height/2}),Hosted=r.Desktop.Hosted,r.Desktop.BindingError,r.Desktop.BindAttempts,r.Desktop.Scale,Handle=s?.Handle.ToInt64(),Bounds=b.IsEmpty?null:new{b.X,b.Y,b.Width,b.Height},NativeSurfaceCount=RulerNativeSurface.ActiveCount,s?.VisibilityCalls,s?.ZOrderCalls,s?.Painted,s?.PaintCount,s?.PaintError,s?.Shown,s?.ClickCount,s?.HoverCount,s?.MenuCount,s?.DragCount,s?.ResizeCount,s?.LastInputError,OpaquePointHitsRuler=s!=null&&RulerNativeSurface.HitAt(point)==s.Handle,CenterPointHitsRuler=s!=null&&RulerNativeSurface.HitAt(new Point(b.X+b.Width/2,b.Y+b.Height/2))==s.Handle,TransparentPointHitsRuler=s!=null&&RulerNativeSurface.HitAt(transparent)==s.Handle,Selected=r.SelectedId,DetailsVisible=r.DetailWindow?.IsVisible==true,ContactCount=r.Placements.Count,PhysicalWinDAccepted=false};
    string json=JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true});File.WriteAllText(path,json);string signature=json.Replace(data.Utc.ToString("O"),"");if(signature!=previous){File.AppendAllText(path+".journal",JsonSerializer.Serialize(data)+Environment.NewLine);previous=signature;}
   }catch(Exception e){WindowLayer.Diagnostics?.Invoke("Ruler observer: "+e.Message);}
  };timer.Start();
 }
}
