using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WorldTime;
public static class DisplayLayout {
 public static Point Physical(Row row)=>new(row.X*row.PositionScale,row.Y*row.PositionScale);
 public static ScreenArea Nearest(Point point,IReadOnlyList<ScreenArea> screens)=>screens.OrderBy(s=>Distance(point,s)).First();
 static double Distance(Point p,ScreenArea s){double x=Math.Max(s.Left-p.X,Math.Max(0,p.X-s.Right)),y=Math.Max(s.Top-p.Y,Math.Max(0,p.Y-s.Bottom));return x*x+y*y;}
 public static bool Reconcile(State state,IReadOnlyList<ScreenArea> screens){
  if(screens.Count==0)return false;bool changed=false;
  foreach(var grouping in state.Rows.GroupBy(r=>r.Group)){
   var rows=grouping.OrderBy(r=>r.Order).ToList();var first=rows[0];var physical=Physical(first);
   var target=screens.FirstOrDefault(s=>s.Device==first.Monitor)??Nearest(physical,screens);
   var old=state.Displays.FirstOrDefault(s=>s.Device==first.Monitor);
   bool remap=old!=null&&old!=target;
   if(remap&&old!.Device==target.Device){physical.X=target.Left+(physical.X-old.Left)/old.Scale*target.Scale;physical.Y=target.Top+(physical.Y-old.Top)/old.Scale*target.Scale;}
   double width=rows.Max(r=>r.Width)*target.Scale,height=((rows.Count-1)*Layout.Step+50)*target.Scale;
   double x=Math.Clamp(physical.X,target.Left,Math.Max(target.Left,target.Right-width));
   double y=Math.Clamp(physical.Y,target.Top,Math.Max(target.Top,target.Bottom-height));
   bool relocate=remap||Math.Abs(x-physical.X)>.1||Math.Abs(y-physical.Y)>.1||Math.Abs(first.PositionScale-target.Scale)>.001;
   for(int i=0;i<rows.Count;i++){
    var row=rows[i];if(row.Monitor!=target.Device){row.Monitor=target.Device;changed=true;}
    if(relocate){row.X=x/target.Scale;row.Y=y/target.Scale+i*Layout.Step;row.PositionScale=target.Scale;changed=true;}
   }
  }
  if(!state.Displays.SequenceEqual(screens)){state.Displays=screens.ToList();changed=true;}
  return changed;
 }
}
