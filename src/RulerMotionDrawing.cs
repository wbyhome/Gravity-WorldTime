using System;
using System.Windows;
using System.Windows.Media;
namespace WorldTime;
public sealed partial class RulerWindow {
 static SolidColorBrush MotionBrush(string color,double opacity=1){var b=Brush(color);b.Opacity=Math.Clamp(opacity,0,1);b.Freeze();return b;}
 static readonly Geometry moonShape=CreateMoon();
 static Geometry CreateMoon(){var g=Geometry.Combine(new EllipseGeometry(new Point(0,0),4.2,4.2),new EllipseGeometry(new Point(2,-1.5),4,4),GeometryCombineMode.Exclude,null);g.Freeze();return g;}
 void DrawMotion(DrawingContext dc){
  if(!built)return;double t=EffectiveMotionMode==RulerMotionMode.Dynamic?motionSeconds:0;
  // All ambience is inside the existing ruler body, above tick stems. At zero
  // background alpha it vanishes, so it cannot add animated mouse-hit islands.
  if(NatureEnabled&&BackgroundOpacity>0){var safeBody=BodyBounds;safeBody.Inflate(-1.5,-1.5);dc.PushClip(new RectangleGeometry(safeBody,18.5,18.5));dc.PushClip(new RectangleGeometry(new Rect(BandOrigin,ScaleTop+2,SkyWidth,10)));dc.PushOpacity(BackgroundOpacity);
   double X(double h)=>CoreOrigin+h/24*trackWidth;
   int index=0;foreach(var (hour,y) in new[]{(2.8,5d),(4.2,8d),(20.5,5d),(21.3,8d)}){double a=.65+.25*Math.Sin(t*.55+index++*1.9);var pen=new Pen(MotionBrush("#EDF4FF",a),.9);double x=X(hour);dc.DrawLine(pen,new Point(x-1.6,ScaleTop+y),new Point(x+1.6,ScaleTop+y));dc.DrawLine(pen,new Point(x,ScaleTop+y-1.6),new Point(x,ScaleTop+y+1.6));}
   foreach(double hour in new[]{1.4,22.6}){double pulse=.5+.5*Math.Sin(t*.48+hour);var at=new Point(X(hour),ScaleTop+6);dc.DrawEllipse(MotionBrush("#C9E2FF",.035+.035*pulse),null,at,6.5,6.5);dc.DrawEllipse(MotionBrush("#DCEAFF",.06+.05*pulse),null,at,5,5);dc.PushTransform(new TranslateTransform(at.X,at.Y));dc.DrawGeometry(MotionBrush("#E8F0FF"),null,moonShape);dc.Pop();}
   var sun=new Point(X(12),ScaleTop+6);double warm=.5+.5*Math.Sin(t*.42);dc.DrawEllipse(MotionBrush("#FFCE78",.05+.05*warm),null,sun,6.5,6.5);dc.DrawEllipse(MotionBrush("#FFE7A5",.10+.06*warm),null,sun,5.4,5.4);dc.DrawEllipse(MotionBrush("#FFF0BE"),null,sun,3.7,3.7);
   foreach(double start in new[]{9d,15.5}){double hour=EffectiveMotionMode==RulerMotionMode.Dynamic?6.5+(start-6.5+t*.045)%11:start;double edge=Math.Min(hour-6.5,17.5-hour);double fade=Math.Clamp(edge/.8,0,1);dc.PushOpacity(.76*fade);dc.PushTransform(new TranslateTransform(X(hour)-9,ScaleTop+2));dc.PushTransform(new ScaleTransform(.72,.72));dc.DrawGeometry(MotionBrush("#EDF8FF"),null,CloudGeometry);dc.Pop();dc.Pop();dc.Pop();}
   dc.Pop();dc.Pop();dc.Pop();
  }
  if(EffectiveMotionMode==RulerMotionMode.Static||!host.S.Life)return;
  foreach(var cluster in Clusters){var p=cluster.Members[0];var life=RulerLifeStage.At(p,host.S);double x=CoreOrigin+cluster.X;
   // Photos stay untouched; preset gestures remain within the
   // existing avatar disc, never to otherwise transparent desktop space.
   dc.PushClip(new EllipseGeometry(new Point(x,AvatarTop+2+AvatarSize/2),AvatarSize/2,AvatarSize/2));if(EffectiveMotionMode==RulerMotionMode.Dynamic)DrawCartoonGesture(dc,p,life.Stage,t,x);dc.PushTransform(new TranslateTransform(x+5,AvatarTop+AvatarSize-8));
   DrawLifeBadge(dc,life.Stage,t,life.Stage==LifeStage.Waking?WakeArrival(p.Id,life.Stage,t):1);dc.Pop();dc.Pop();
  }
 }
 void DrawCartoonGesture(DrawingContext dc,RulerPerson person,LifeStage stage,double seconds,double x){
  string id=AvatarId(person);var choice=Avatars.Choice(id);if(choice?.File.Length>0)return;
  int style=choice?.Preset>=0?choice.Preset:PersonAvatar.StyleFor(id);var skin=MotionBrush(PersonAvatar.SkinColorFor(style));var ink=MotionBrush("#393442");
  dc.PushTransform(new TranslateTransform(x-AvatarSize/2,AvatarTop+2));dc.PushTransform(new ScaleTransform(AvatarSize/48,AvatarSize/48));
  double since=stageMemory.TryGetValue(person.Id,out var memory)?Math.Max(0,seconds-memory.Since):99;
  bool eyesClosed=stage==LifeStage.Sleeping||stage==LifeStage.Waking&&since<.8||stage is LifeStage.Active or LifeStage.Resting&&(seconds%9)<.28;
  if(eyesClosed){foreach(double eyeX in new[]{19d,29}){dc.DrawEllipse(skin,null,new Point(eyeX,23),2.4,2.5);dc.DrawLine(new Pen(ink,1.25),new Point(eyeX-1.8,23.4),new Point(eyeX+1.8,23.4));}}
  if(stage==LifeStage.Waking&&since<3){double lift=7*Math.Sin(Math.Min(1,since/3)*Math.PI);var arm=new Pen(skin,3){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};dc.DrawLine(arm,new Point(12,43),new Point(7,40-lift));dc.DrawLine(arm,new Point(36,43),new Point(41,40-lift));}
  if(stage==LifeStage.Preparing){double shift=1.8*Math.Sin(seconds*1.8);dc.DrawRoundedRectangle(MotionBrush("#F4FBFA"),null,new Rect(23+shift,29,7,3),.8,.8);dc.DrawLine(new Pen(MotionBrush("#78B8BB"),1.8),new Point(28+shift,31),new Point(35+shift,35));}
  if(stage==LifeStage.Resting){double rise=(seconds*.3)%1;dc.DrawLine(new Pen(MotionBrush("#F5DEB2",.7*(1-rise)),1),new Point(31,38-rise*5),new Point(32,36-rise*5));}
  dc.Pop();dc.Pop();
 }
 static readonly Geometry CloudGeometry=Geometry.Parse("M 0,8 C 0,4 4,3 6,5 C 7,0 14,0 16,5 C 23,4 25,11 20,12 L 4,12 C 1,12 0,11 0,8 Z");
 readonly System.Collections.Generic.Dictionary<string,(LifeStage Stage,double Since)> stageMemory=new();
 double WakeArrival(string id,LifeStage stage,double seconds){
  return stageMemory.TryGetValue(id,out var old)?Math.Clamp(1-(seconds-old.Since)/3,0,1):0;
 }
 void RememberLifeStages(){
  foreach(var p in snapshots){var stage=RulerLifeStage.At(p,host.S).Stage;if(stageMemory.TryGetValue(p.Id,out var old)&&old.Stage!=stage){stageMemory[p.Id]=(stage,motionSeconds);motionTransitions++;}else if(!stageMemory.ContainsKey(p.Id))stageMemory[p.Id]=(stage,motionSeconds);}
  var activeIds=new System.Collections.Generic.HashSet<string>(System.Linq.Enumerable.Select(snapshots,p=>p.Id),StringComparer.Ordinal);foreach(string id in System.Linq.Enumerable.ToArray(stageMemory.Keys))if(!activeIds.Contains(id))stageMemory.Remove(id);
 }
 static void DrawLifeBadge(DrawingContext dc,LifeStage stage,double time,double arrival){
  double s=Math.Sin(time*1.4);dc.DrawEllipse(MotionBrush("#21344B",.94),new Pen(MotionBrush("#C9DFEA",.8),.6),new Point(0,0),6.4,6.4);
  var ink=MotionBrush(stage==LifeStage.Sleeping?"#DAE5FF":"#FFE0A0");var pen=new Pen(ink,1);pen.StartLineCap=pen.EndLineCap=PenLineCap.Round;
  if(stage==LifeStage.Sleeping){double rise=(time*.28)%1;dc.PushOpacity(.15+.8*Math.Sin(Math.PI*rise));dc.PushTransform(new TranslateTransform(0,-rise*4));dc.DrawLine(pen,new Point(-2,-2),new Point(2,-2));dc.DrawLine(pen,new Point(2,-2),new Point(-2,2));dc.DrawLine(pen,new Point(-2,2),new Point(2,2));dc.Pop();dc.Pop();}
  else if(stage==LifeStage.Waking){double lift=arrival*Math.Sin(Math.Min(1,(time%3)/3)*Math.PI);dc.DrawEllipse(ink,null,new Point(0,-2),1.3,1.3);dc.DrawLine(pen,new Point(0,0),new Point(0,3));dc.DrawLine(pen,new Point(0,0),new Point(-3,-1-1.5*lift));dc.DrawLine(pen,new Point(0,0),new Point(3,-1-1.5*lift));}
  else if(stage==LifeStage.Preparing){dc.PushTransform(new RotateTransform(18*s));dc.DrawRoundedRectangle(ink,null,new Rect(-3,-1,5,2),.6,.6);dc.DrawLine(new Pen(ink,1.2),new Point(1,0),new Point(4,3));dc.DrawLine(pen,new Point(-2,-2),new Point(-2,-1));dc.DrawLine(pen,new Point(0,-2),new Point(0,-1));dc.Pop();}
  else if(stage==LifeStage.Resting){dc.DrawRoundedRectangle(null,pen,new Rect(-3,0,5,3),.7,.7);dc.DrawEllipse(null,pen,new Point(3,1.2),1.2,1);for(int i=0;i<2;i++){double y=-1.5-((time*.35+i*.5)%1)*2;dc.DrawLine(new Pen(MotionBrush("#FFE0A0",.45+.3*Math.Sin(time+i)),.7),new Point(-2+i*2,y),new Point(-1.5+i*2,y-1));}}
  else if(stage==LifeStage.Active){for(int i=0;i<3;i++)dc.DrawEllipse(MotionBrush("#BEE4E3",.5+.3*Math.Sin(time*1.2+i*1.5)),null,new Point(-3+i*3,0),1.1,1.1);}
  else dc.DrawEllipse(MotionBrush("#B8CDDF"),null,new Point(0,0),1.6,1.6);
 }
}
