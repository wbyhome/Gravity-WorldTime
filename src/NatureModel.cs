using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WorldTime;
public sealed record GeoPoint(string Iana,double Latitude,double Longitude);
public sealed record NatureInfo(string Light,string Season,double? Elevation,GeoPoint? Point);
public static class NatureModel {
 // Pinned IANA 2025b zone.tab: reference locations, not the person's actual GPS.
 static readonly Dictionary<string,GeoPoint> points=Load();
 static Dictionary<string,GeoPoint> Load(){
  using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("WorldTime.zone.tab")??throw new InvalidDataException("Missing coordinate reference");
  using var reader=new StreamReader(stream);var result=new Dictionary<string,GeoPoint>(StringComparer.Ordinal);
  while(reader.ReadLine() is string line){if(line.Length==0||line[0]=='#')continue;var fields=line.Split('\t');if(fields.Length<3)continue;var c=fields[1];int split=c.IndexOfAny(new[]{'+','-'},1);if(split<0)continue;
   double Part(string v,int degree){int sign=v[0]=='-'?-1:1;double d=int.Parse(v.Substring(1,degree),CultureInfo.InvariantCulture),m=int.Parse(v.Substring(1+degree,2),CultureInfo.InvariantCulture),s=v.Length>degree+3?int.Parse(v.Substring(degree+3,2),CultureInfo.InvariantCulture):0;return sign*(d+m/60+s/3600);}
   result[fields[2]]=new(fields[2],Part(c[..split],2),Part(c[split..],3));
  }return result;
 }
 public static GeoPoint? Resolve(Row? row){if(row==null)return null;var city=Catalog.Find(row);return city!=null&&city.Code==row.Code&&city.Iana.Length>0&&points.TryGetValue(city.Iana,out var point)?point:null;}
 public static NatureInfo At(RulerPerson person,DateTimeOffset instant){
  var point=Resolve(person.Row);if(point==null)return new("Location unknown","Season unknown",null,null);
  var (elevation,declination,angle)=Solar(instant,point.Latitude,point.Longitude);
  double maximum=90-Math.Abs(point.Latitude-declination),minimum=Math.Abs(point.Latitude+declination)-90;
  string light=minimum>-.833?"Polar daylight estimate":maximum<-.833?"Polar night estimate":elevation>0?"Estimated daylight":elevation> -6?(angle<0?"Near dawn":"Near dusk"):"Estimated night";
  int month=person.Local.Month;if(point.Latitude<0)month=(month+5)%12+1;
  string season=Math.Abs(point.Latitude)<30?"Local seasons vary":month is >=3 and <=5?"Calendar spring":month is >=6 and <=8?"Calendar summer":month is >=9 and <=11?"Calendar autumn":"Calendar winter";
  return new(light,season,elevation,point);
 }
 // NOAA General Solar Position Calculations: geometric altitude, no terrain or weather.
 // UTC is used throughout; civil DST never alters the same instant's solar altitude.
 public static (double Elevation,double Declination,double HourAngle) Solar(DateTimeOffset instant,double latitude,double longitude){
  if(!double.IsFinite(latitude)||!double.IsFinite(longitude)||Math.Abs(latitude)>90||Math.Abs(longitude)>180)throw new ArgumentOutOfRangeException(nameof(latitude));
  var utc=instant.UtcDateTime;double rad=Math.PI/180,g=2*Math.PI/(DateTime.IsLeapYear(utc.Year)?366:365)*(utc.DayOfYear-1+(utc.TimeOfDay.TotalHours-12)/24);
  double eq=229.18*(.000075+.001868*Math.Cos(g)-.032077*Math.Sin(g)-.014615*Math.Cos(2*g)-.040849*Math.Sin(2*g));
  double decl=.006918-.399912*Math.Cos(g)+.070257*Math.Sin(g)-.006758*Math.Cos(2*g)+.000907*Math.Sin(2*g)-.002697*Math.Cos(3*g)+.00148*Math.Sin(3*g);
  double solar=((utc.TimeOfDay.TotalMinutes+eq+4*longitude)%1440+1440)%1440,ha=solar/4-180;
  double cosine=Math.Sin(latitude*rad)*Math.Sin(decl)+Math.Cos(latitude*rad)*Math.Cos(decl)*Math.Cos(ha*rad);
  return (90-Math.Acos(Math.Clamp(cosine,-1,1))/rad,decl/rad,ha);
 }
 public static string SeasonSymbol(string season)=>season switch{"Calendar spring"=>"✿","Calendar summer"=>"☀","Calendar autumn"=>"❧","Calendar winter"=>"❄","Local seasons vary"=>"◌",_=>""};
 public static string LightSymbol(string light)=>light switch{"Estimated daylight" or "Polar daylight estimate"=>"☀","Near dawn" or "Near dusk"=>"◒","Location unknown"=>"",_=>"☾"};
 public static string Detail(NatureInfo nature,string language)=>L10n.Text(language,nature.Light)+" · "+L10n.Text(language,nature.Season)+"\n"+L10n.Text(language,nature.Point==null?"No precise place: general ambience only":"Reference-city solar estimate; calendar seasons, not weather.");
}

public sealed record RulerCluster(List<RulerPerson> Members,double X,double CardLeft,int Lane);
public static class CompactRulerLayout {
 public static List<RulerCluster> Arrange(IReadOnlyList<RulerPerson> people,double width,double cardWidth,int lanes){
  if(!double.IsFinite(width)||width<cardWidth||cardWidth<40||lanes<1)throw new ArgumentOutOfRangeException(nameof(width));
  var result=new List<RulerCluster>();
  foreach(var p in people.OrderBy(p=>p.Self?0:1).ThenBy(p=>p.Fraction).ThenBy(p=>p.Id,StringComparer.Ordinal)){
   double x=p.Fraction*width,left=Math.Clamp(x-cardWidth/2,0,width-cardWidth);int lane=0;
   var same=result.FirstOrDefault(g=>Math.Abs(g.X-x)<.5);if(same!=null){same.Members.Add(p);continue;}
   while(lane<lanes&&result.Any(g=>g.Lane==lane&&left<g.CardLeft+cardWidth+8&&left+cardWidth+8>g.CardLeft))lane++;
   if(lane==lanes){result.OrderBy(g=>Math.Abs(g.X-x)).First().Members.Add(p);}
   else result.Add(new(new(){p},x,left,lane));
  }return result;
 }
}
