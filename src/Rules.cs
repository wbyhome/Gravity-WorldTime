using System;
using System.Globalization;
using System.Collections.Generic;
namespace WorldTime;
public static class Rules {
 static readonly Dictionary<(long,string,CityRoutine?,int,int,int,bool),DateTimeOffset?> nextCache=new();
 public static void ClearCache()=>nextCache.Clear();
 public static bool Workday(DateTimeOffset t,State s,Row? row=null)=>row?.Routine is CityRoutine routine?routine.Workday(t.Date):!s.Weekend||(t.DayOfWeek!=DayOfWeek.Saturday&&t.DayOfWeek!=DayOfWeek.Sunday);
 public static bool Available(DateTimeOffset t,State s,Row? row=null)=>row?.Routine is CityRoutine routine?routine.Available(t.DateTime):Workday(t,s)&&t.Hour>=s.Start&&t.Hour<s.End&&t.Hour!=s.Lunch;
 static bool Lunch(DateTimeOffset t,State s,Row? row=null)=>row?.Routine is CityRoutine routine?routine.OnBreak(t.Hour*60+t.Minute):t.Hour==s.Lunch&&t.Hour>=s.Start&&t.Hour<s.End;
 static string Phase(DateTimeOffset t,State s,Row? row=null){
  if(row?.Routine is CityRoutine routine){
   int m=t.Hour*60+t.Minute;
   if(routine.Sleeping(m))return "sleep";
   if(Available(t,s,row))return t.Hour<8||t.Hour>=23?"night-shift":"busy";
   if(routine.OnBreak(m))return "lunch";
   return "unwind";
  }
  if((t.Hour<5||t.Hour>=23)&&Available(t,s,row))return "night-shift";
  if(t.Hour<5)return "sleep";
  if(t.Hour>=23)return "bedtime";
  if(t.Hour<8)return Workday(t,s)?"waking":"weekend-bed";
  if(Lunch(t,s))return "lunch";
  if(t.Hour>=21)return "unwind";
  if(t.Hour>=19)return "dinner";
  if(!Workday(t,s))return "weekend";
  if(t.Hour>=17)return "commute";
  if(t.Hour<9)return "breakfast";
  return "busy";
 }
 public static string Brief(DateTimeOffset t,State s,Row? row=null)=>Hint(t,s,0,row);
 public static string Short(DateTimeOffset t,State s,Row? row=null)=>Hint(t,s,1,row);
 public static string Micro(DateTimeOffset t,State s,Row? row=null)=>Hint(t,s,2,row);
 static string Hint(DateTimeOffset t,State s,int length,Row? row){
  if(!s.Life&&!s.Advice)return "";
  if(!s.Life)return ContactLine(t,s,length,row);
  return FriendHints.Get(Phase(t,s,row),s.Language,s.Advice?length:2);
 }
 public static DateTimeOffset? NextContact(DateTimeOffset local,State state,Row row){
  var zone=TimeZoneInfo.FindSystemTimeZoneById(row.Zone);
  local=TimeZoneInfo.ConvertTime(local,zone);
  if(Available(local,state,row))return local;
  var key=(local.UtcTicks/TimeSpan.TicksPerMinute,row.Zone,row.Routine is CityRoutine routine?routine with{}:null,state.Start,state.End,state.Lunch,state.Weekend);
  if(nextCache.TryGetValue(key,out var cached))return cached;
  if(nextCache.Count>256)nextCache.Clear();
  long ticks=local.UtcTicks-local.UtcTicks%TimeSpan.TicksPerMinute+TimeSpan.TicksPerMinute;
  for(int i=0;i<15*1440;i++){
   var candidate=TimeZoneInfo.ConvertTime(new DateTimeOffset(ticks+i*TimeSpan.TicksPerMinute,TimeSpan.Zero),zone);
   if(Available(candidate,state,row)){nextCache[key]=candidate;return candidate;}
  }
  nextCache[key]=null;return null;
 }
 public static DateTime NextWindow(DateTimeOffset t,State s){
  var next=t.Date.AddHours(s.Start==s.Lunch?s.Start+1:s.Start);if(next<=t.DateTime)next=next.AddDays(1);
  while(s.Weekend&&(next.DayOfWeek==DayOfWeek.Saturday||next.DayOfWeek==DayOfWeek.Sunday))next=next.AddDays(1);
  if(Workday(t,s)&&Lunch(t,s)&&s.Lunch+1<s.End)next=t.Date.AddHours(s.Lunch+1);
  return next;
 }
 public static string ContactLine(DateTimeOffset t,State s,int length=0,Row? row=null){
  if(!s.Advice)return "";
  string phase=Phase(t,s,row);
  if(Available(t,s,row))return FriendHints.Get("ask-first",s.Language,length);
  if(row?.Routine!=null){
   var next=NextContact(t,s,row);if(next==null)return L10n.Text(s.Language,"No contact window in the next 15 days.");
   string day=next.Value.Date==t.Date?"today":next.Value.Date==t.Date.AddDays(1)?"tomorrow":"later";
   return FriendHints.When(day,s.Language,length,next.Value.ToString("HH:mm",CultureInfo.InvariantCulture),next.Value.ToString("MM-dd",CultureInfo.InvariantCulture));
  }
  if(phase=="lunch"&&!Workday(t,s))return FriendHints.Get("after-meal",s.Language,length);
  if(phase is "sleep" or "waking" or "breakfast" or "bedtime" or "weekend-bed" or "lunch"){
   // A friend's weekend is not a reason to postpone every message until Monday.
   if(!Workday(t,s))return FriendHints.Get("weekend-wait",s.Language,length);
   var next=NextWindow(t,s);if(next.Date>t.Date.AddDays(1))return FriendHints.Get("weekend-wait",s.Language,length);
   string clock=next.ToString("HH:mm",CultureInfo.InvariantCulture);
   string day=next.Date==t.Date?"today":next.Date==t.Date.AddDays(1)?"tomorrow":"later";
   return FriendHints.When(day,s.Language,length,clock,next.ToString("MM-dd",CultureInfo.InvariantCulture));
  }
  return FriendHints.Get(phase=="commute"?"after-trip":phase=="dinner"?"after-meal":"ask-first",s.Language,length);
 }
 public static string Text(DateTimeOffset t,State s,Row? row=null){
  string text=Brief(t,s,row);if(s.Life&&s.Advice)text+="\n"+ContactLine(t,s,0,row);
  if(!s.Advice||Available(t,s,row))return text;
  if(row?.Routine!=null)return text+"\n"+L10n.Text(s.Language,"Uses this city's local routine; actual availability is unknown.");
  return text+"\n"+string.Format(L10n.Text(s.Language,"Next usual contact time: {0}."),NextWindow(t,s).ToString("MM-dd HH:mm",CultureInfo.InvariantCulture));
 }
}
