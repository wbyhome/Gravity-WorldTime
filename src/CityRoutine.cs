using System;
using System.IO;
using System.Linq;

namespace WorldTime;
public sealed record CityRoutine {
 public int Wake {get;set;}=8*60;
 public int Sleep {get;set;}=23*60;
 public bool SleepEnabled {get;set;}=true;
 public int Start {get;set;}=9*60;
 public int End {get;set;}=18*60;
 public bool BreakEnabled {get;set;}=true;
 public int BreakStart {get;set;}=12*60;
 public int BreakEnd {get;set;}=13*60;
 public int DaysOff {get;set;}=65; // Sunday bit 0, Saturday bit 6.
 public static bool Contains(int start,int end,int minute)=>start<end?minute>=start&&minute<end:minute>=start||minute<end;
 public bool Sleeping(int minute)=>SleepEnabled&&Contains(Sleep,Wake,minute);
 public bool OnBreak(int minute)=>BreakEnabled&&Contains(BreakStart,BreakEnd,minute);
 public bool Workday(DateTime date)=>(DaysOff&(1<<(int)date.DayOfWeek))==0;
 public bool Available(DateTime local){
  int minute=local.Hour*60+local.Minute;
  var shiftDay=Start>End&&minute<End?local.Date.AddDays(-1):local.Date;
  return Workday(shiftDay)&&Contains(Start,End,minute)&&!Sleeping(minute)&&!OnBreak(minute);
 }
 public void Validate(){
  if(Wake<0||Wake>=1440||Sleep<0||Sleep>=1440||Start<0||Start>=1440||End<0||End>1440||BreakStart<0||BreakStart>=1440||BreakEnd<0||BreakEnd>1440)throw new InvalidDataException("Use HH:mm; only an end time may be 24:00.");
  if(SleepEnabled&&Wake==Sleep)throw new InvalidDataException("Wake and sleep times must differ.");
  if(Start==End||DaysOff<0||DaysOff>=127)throw new InvalidDataException("Choose a contact window and at least one contact day.");
  if(BreakEnabled&&(BreakStart==BreakEnd||Enumerable.Range(0,1440).Any(m=>Contains(BreakStart,BreakEnd,m)&&!Contains(Start,End,m))))throw new InvalidDataException("The break must fit inside the contact window.");
  if(!Enumerable.Range(0,1440).Any(m=>Contains(Start,End,m)&&!OnBreak(m)&&!Sleeping(m)))throw new InvalidDataException("Keep some contact time outside sleep and breaks.");
 }
 public static CityRoutine FromDefaults(State state)=>new(){Start=state.Start*60,End=state.End*60,BreakStart=state.Lunch*60,BreakEnd=(state.Lunch+1)*60,BreakEnabled=state.Lunch>=state.Start&&state.Lunch<state.End,DaysOff=state.Weekend?65:0};
 public static string Time(int minute)=>minute==1440?"24:00":$"{minute/60:00}:{minute%60:00}";
 public static bool Parse(string value,bool allow24,out int minute){
  minute=0;var parts=value.Trim().Split(':');if(parts.Length!=2||!int.TryParse(parts[0],out int h)||!int.TryParse(parts[1],out int m)||h<0||m<0||m>59||h>23&&!(allow24&&h==24&&m==0))return false;minute=h*60+m;return true;
 }
}

public static class CalendarRelation {
 public static int Difference(DateTimeOffset target,DateTimeOffset viewer)=>(target.Date-viewer.Date).Days;
 public static string Label(DateTimeOffset target,DateTimeOffset viewer,string language)=>L10n.Text(language,Difference(target,viewer) switch{-2=>"Two days behind",-1=>"Yesterday",0=>"Today",1=>"Tomorrow",2=>"Two days ahead",_=>"Different day"});
}
