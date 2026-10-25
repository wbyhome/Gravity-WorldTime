using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WorldTime;
public sealed record RulerPerson(string Id,string Name,string City,DateTimeOffset Local,TimeSpan Difference,string Status,bool Contactable,bool Self,Row? Row){
 public double Fraction=>(Local.Hour*60+Local.Minute)/1440d;
 public string Date=>Local.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
 public string Time(bool twelve)=>Local.ToString(twelve?"hh:mm tt":"HH:mm",CultureInfo.InvariantCulture);
}
public sealed record RulerPlacement(RulerPerson Person,double X,double CardLeft,int Lane);
public static class RulerModel {
 public static List<RulerPerson> Snapshot(State state,DateTimeOffset instant,TimeZoneInfo? viewer=null,string selfName=""){
  var mine=TimeZoneInfo.ConvertTime(instant,viewer??TimeZoneInfo.Local);
  string selfId="@me";while(state.Rows.Any(r=>r.Id==selfId))selfId+="@";
  var result=new List<RulerPerson>{new(selfId,string.IsNullOrWhiteSpace(selfName)?L10n.Text(state.Language,"Me"):selfName,(viewer??TimeZoneInfo.Local).DisplayName,mine,TimeSpan.Zero,L10n.Text(state.Language,DayPart.Key(mine.Hour)),false,true,null)};
  foreach(var row in state.Rows){
   var local=Catalog.Local(row.Zone,instant);var routine=row.Routine??CityRoutine.FromDefaults(state);int minute=local.Hour*60+local.Minute;
   bool available=Rules.Available(local,state,row);
   bool waking=(minute-routine.Wake+1440)%1440<60;
   string status=routine.Sleeping(minute)?"May be resting":available?"Usually convenient":routine.OnBreak(minute)&&Rules.Workday(local,state,row)?"May be on a break":waking?"May be waking up":"Consider a message";
   var city=Catalog.Find(row);
   result.Add(new(row.Id,L10n.CityName(row,state.Language),city==null?row.City:Catalog.CityName(city,state.Language),local,local.Offset-mine.Offset,L10n.Text(state.Language,status),available,false,row));
  }
  return result;
 }
 // True-hour anchors never shift; only cards get another vertical lane.
 public static List<RulerPlacement> Arrange(IReadOnlyList<RulerPerson> people,double width,double cardWidth=170){
  if(!double.IsFinite(width)||width<cardWidth)throw new ArgumentOutOfRangeException(nameof(width));
  var occupied=new List<List<(double Left,double Right)>>();var result=new List<RulerPlacement>();
  foreach(var p in people.OrderBy(p=>p.Fraction).ThenBy(p=>p.Self?0:1).ThenBy(p=>p.Id,StringComparer.Ordinal)){
   double x=p.Fraction*width,left=Math.Clamp(x-20,0,width-cardWidth);int lane=0;
   while(lane<occupied.Count&&occupied[lane].Any(r=>left<r.Right+10&&left+cardWidth+10>r.Left))lane++;
   if(lane==occupied.Count)occupied.Add(new());occupied[lane].Add((left,left+cardWidth));result.Add(new(p,x,left,lane));
  }
  return result;
 }
 public static string Difference(RulerPerson p,string language){
  int minutes=(int)p.Difference.TotalMinutes;if(minutes==0)return L10n.Text(language,"Same local time as me");
  return string.Format(L10n.Text(language,minutes>0?"Ahead of me by {0}":"Behind me by {0}"),$"{Math.Abs(minutes)/60:00}:{Math.Abs(minutes)%60:00}");
 }
 public static string Detail(RulerPerson p,State state){
  string text=p.Name+" · "+p.City+"\n"+p.Local.ToString("yyyy-MM-dd HH:mm zzz",CultureInfo.InvariantCulture)+"\n"+Difference(p,state.Language);
  if(p.Row is Row row){var r=row.Routine??CityRoutine.FromDefaults(state);text+="\n"+L10n.Text(state.Language,row.Routine==null?"Default routine":"Custom routine")+": "+CityRoutine.Time(r.Start)+"–"+CityRoutine.Time(r.End)+"\n"+p.Status+"\n"+Rules.ContactLine(p.Local,state,0,row);}
  return text;
 }
}
