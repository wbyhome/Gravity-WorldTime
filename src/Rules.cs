using System;
using System.Globalization;
namespace WorldTime;
public static class Rules {
 public static bool Workday(DateTimeOffset t,State s)=>!s.Weekend||(t.DayOfWeek!=DayOfWeek.Saturday&&t.DayOfWeek!=DayOfWeek.Sunday);
 public static bool Available(DateTimeOffset t,State s)=>Workday(t,s)&&t.Hour>=s.Start&&t.Hour<s.End&&t.Hour!=s.Lunch;
 static bool Lunch(DateTimeOffset t,State s)=>t.Hour==s.Lunch&&t.Hour>=s.Start&&t.Hour<s.End;
 static string Phase(DateTimeOffset t,State s){
  if((t.Hour<5||t.Hour>=23)&&Available(t,s))return "night-shift";
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
 public static string Brief(DateTimeOffset t,State s)=>Hint(t,s,0);
 public static string Short(DateTimeOffset t,State s)=>Hint(t,s,1);
 public static string Micro(DateTimeOffset t,State s)=>Hint(t,s,2);
 static string Hint(DateTimeOffset t,State s,int length){
  if(!s.Life&&!s.Advice)return "";
  if(!s.Life)return ContactLine(t,s,length);
  return FriendHints.Get(Phase(t,s),s.Language,s.Advice?length:2);
 }
 public static DateTime NextWindow(DateTimeOffset t,State s){
  var next=t.Date.AddHours(s.Start==s.Lunch?s.Start+1:s.Start);if(next<=t.DateTime)next=next.AddDays(1);
  while(s.Weekend&&(next.DayOfWeek==DayOfWeek.Saturday||next.DayOfWeek==DayOfWeek.Sunday))next=next.AddDays(1);
  if(Workday(t,s)&&Lunch(t,s)&&s.Lunch+1<s.End)next=t.Date.AddHours(s.Lunch+1);
  return next;
 }
 public static string ContactLine(DateTimeOffset t,State s,int length=0){
  if(!s.Advice)return "";
  string phase=Phase(t,s);
  if(Available(t,s))return FriendHints.Get("ask-first",s.Language,length);
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
 public static string Text(DateTimeOffset t,State s){
  string text=Brief(t,s);if(s.Life&&s.Advice)text+="\n"+ContactLine(t,s);
  if(!s.Advice||Available(t,s))return text;
  return text+"\n"+string.Format(L10n.Text(s.Language,"Next usual contact time: {0}."),NextWindow(t,s).ToString("MM-dd HH:mm",CultureInfo.InvariantCulture));
 }
}
