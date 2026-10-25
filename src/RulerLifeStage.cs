using System;
namespace WorldTime;
public enum LifeStage { Neutral, Waking, Preparing, Active, Resting, Sleeping }
public sealed record LifeEstimate(LifeStage Stage,string Key,string Basis);
// This is a routine-based illustration. Contact hours never establish work or activity.
public static class RulerLifeStage {
 public static LifeEstimate At(RulerPerson person,State state){
  var routine=person.Row?.Routine??CityRoutine.FromDefaults(state);
  int minute=person.Local.Hour*60+person.Local.Minute;
  string basis=person.Row?.Routine==null?"Based on the default routine; actual activity is unknown.":"Based on this person's routine; actual activity is unknown.";
  LifeEstimate E(LifeStage stage,string key)=>new(stage,key,basis);
  if(routine.Sleeping(minute))return E(LifeStage.Sleeping,"May be sleeping");
  if(routine.SleepEnabled){int awake=(minute-routine.Wake+1440)%1440;if(awake<30)return E(LifeStage.Waking,"May have just woken up");if(awake<90)return E(LifeStage.Preparing,"May be getting ready for their day");}
  var breakDay=routine.Start>routine.End&&minute<routine.End?person.Local.Date.AddDays(-1):person.Local.Date;
  if(routine.OnBreak(minute)&&routine.Workday(breakDay))return E(LifeStage.Resting,"May be taking a break");
  if(routine.SleepEnabled&&(routine.Sleep-minute+1440)%1440 is >0 and <=90)return E(LifeStage.Resting,"May be winding down");
  return routine.SleepEnabled?E(LifeStage.Active,"May be going about their day"):E(LifeStage.Neutral,"Their activity is unknown");
 }
}
