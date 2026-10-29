using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
namespace WorldTime;
public enum WorldTimeView { Ruler, Ring }
public sealed record ViewLayout(double X,double Y,double Size) {
 public bool Valid=>double.IsFinite(X)&&double.IsFinite(Y)&&Math.Abs(X)<100000&&Math.Abs(Y)<100000&&double.IsFinite(Size)&&Size>=200&&Size<=1100;
}
public sealed record RingNode(RulerCluster Cluster,Point Center,Rect Label,Rect Date,bool Inset){
 public RulerPerson Person=>Cluster.Members[0];
 public bool Collapsed=>Inset;
 public Rect Footprint {get{var r=new Rect(Center.X-(Collapsed?13:18),Center.Y-(Collapsed?13:18),Collapsed?26:36,Collapsed?26:36);if(!Label.IsEmpty)r.Union(Label);if(!Date.IsEmpty)r.Union(Date);return r;}}
}
public static class RingLayout {
 public const double MinSize=420,DefaultSize=560,MaxSize=1100,OuterMargin=74;
 public static Point PointAt(double fraction,double radius,Point center){double a=fraction*Math.PI*2;return new(center.X+Math.Sin(a)*radius,center.Y-Math.Cos(a)*radius);}
 public static double CircularDistance(double a,double b){double d=Math.Abs(a-b)%1;return Math.Min(d,1-d);}
 public static double Chord(double a,double b,double radius)=>2*radius*Math.Sin(Math.PI*CircularDistance(a,b));
 public static double Radius(double size)=>Math.Max(22,PersonRadius(size)-60);
 public static double PersonRadius(double size)=>Math.Max(40,size/2-OuterMargin);
 public static double HalfThickness(double size)=>Math.Clamp(size*.025,11,18);
 public static List<RulerCluster> Group(IReadOnlyList<RulerPerson> people,double radius,double trackWidth,IReadOnlyList<RulerCluster>? previous=null,Func<RulerPerson,bool>? showDate=null){
  var other=people.Where(p=>!p.Self).OrderBy(p=>p.Fraction).ThenBy(p=>p.Id,StringComparer.Ordinal).ToArray();var prior=new Dictionary<string,string>();if(previous!=null)foreach(var c in previous)foreach(var p in c.Members)prior[p.Id]=c.Members[0].Id;
  int[] parent=Enumerable.Range(0,other.Length).ToArray();int Find(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}
  for(int i=0;i<other.Length&&other.Length>1;i++){int j=(i+1)%other.Length;bool same=prior.TryGetValue(other[i].Id,out var a)&&prior.TryGetValue(other[j].Id,out var b)&&a==b;if(Chord(other[i].Fraction,other[j].Fraction,radius)<(same?112:100))parent[Find(j)]=Find(i);}
  RulerCluster Make(List<RulerPerson> members){var rep=members.OrderBy(p=>prior.TryGetValue(p.Id,out var old)&&old==p.Id?0:1).ThenBy(p=>p.Id,StringComparer.Ordinal).First();members.Remove(rep);members.Insert(0,rep);return new(members,rep.Fraction*trackWidth,0,0);}
  var result=Enumerable.Range(0,other.Length).GroupBy(Find).Select(g=>Make(g.Select(i=>other[i]).ToList())).ToList();foreach(var me in people.Where(p=>p.Self))result.Add(new(new(){me},me.Fraction*trackWidth,0,0));
  // Self is never merged. Only intersecting contact footprints are consolidated.
  for(int pass=0;pass<people.Count;pass++){var nodes=Place(result,2*(radius+OuterMargin),showDate);RingNode? first=null,second=null;for(int i=0;i<nodes.Count&&first==null;i++)for(int j=i+1;j<nodes.Count;j++)if(!nodes[i].Person.Self&&!nodes[j].Person.Self&&nodes[i].Footprint.IntersectsWith(nodes[j].Footprint)){first=nodes[i];second=nodes[j];break;}if(first==null||second==null)break;result.Remove(first.Cluster);result.Remove(second.Cluster);result.Add(Make(first.Cluster.Members.Concat(second.Cluster.Members).ToList()));}
  return result.OrderBy(c=>c.Members[0].Fraction).ThenBy(c=>c.Members[0].Id,StringComparer.Ordinal).ToList();
 }
 public static List<RingNode> Place(IReadOnlyList<RulerCluster> clusters,double size,Func<RulerPerson,bool>? showDate=null){
  var center=new Point(size/2,size/2);double radius=PersonRadius(size);var result=new List<RingNode>();
  RingNode Full(RulerCluster c){var p=c.Members[0];var at=PointAt(p.Fraction,radius,center);bool lower=at.Y>center.Y+8;double w=Math.Min(92,Math.Max(62,size/4));double y=lower?at.Y+23:at.Y-69;var label=new Rect(Math.Clamp(at.X-w/2,4,size-w-4),Math.Clamp(y,4,size-50),w,46);bool date=showDate?.Invoke(p)??true;return new(c,at,label,date?new Rect(label.X,label.Y+32,w,14):Rect.Empty,false);}
  var me=clusters.FirstOrDefault(c=>c.Members[0].Self);var mine=me==null?null:Full(me);
  foreach(var c in clusters){var node=Full(c);if(!node.Person.Self&&mine!=null&&node.Footprint.IntersectsWith(mine.Footprint)){var at=PointAt(node.Person.Fraction,Radius(size)+12,center);node=new(c,at,Rect.Empty,Rect.Empty,true);}result.Add(node);}return result;
 }
 public static double RadialDiameter(double startSize,Point center,Point down,Point now,double dpi){if(dpi<=0||!double.IsFinite(dpi))throw new ArgumentOutOfRangeException(nameof(dpi));return startSize+2*((now-center).Length-(down-center).Length)/dpi;}
}