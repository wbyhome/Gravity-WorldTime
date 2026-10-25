using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace WorldTime;
// Geometry describes membership, never a shared calendar interval on the clock face.
public sealed record RulerDateRail(RulerDateGroup Group,int Row,double Start,double End,double Y,Rect LabelBounds) {
 public bool Hit(Point p)=>LabelBounds.Contains(p)||(p.X>=Start-4&&p.X<=End+4&&p.Y>=Y-4&&p.Y<=Y+9);
}
public static class RulerDateLayout {
 public static IReadOnlyList<RulerDateRail> Arrange(IReadOnlyList<RulerDateGroup> groups,double origin,double width,double step,string language){
  double labelWidth=L10n.Resolve(language)=="en"?62:84;
  return groups.Select((g,i)=>{
   double start=origin+g.Start-7,end=origin+g.End+7,center=Math.Clamp(origin+(g.Start+g.End)/2,labelWidth/2+3,width-labelWidth/2-3);
   return new RulerDateRail(g,i,start,end,i*step,new Rect(center-labelWidth/2,i*step+9,labelWidth,16));
  }).ToArray();
 }
 public static string Caption(RulerDateGroup group,string language,bool full=false){
  string resolved=L10n.Resolve(language);var date=group.Members[0].Local;
  if(resolved=="en")return date.ToString(full?"dddd, MMMM d, yyyy":"MMM d",CultureInfo.GetCultureInfo("en-US"));
  return date.ToString(full?"yyyy年M月d日":"M月d日",CultureInfo.InvariantCulture);
 }
}
