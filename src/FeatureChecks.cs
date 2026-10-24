using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;

namespace WorldTime;
public static class FeatureChecks {
 public static void Run(Action<bool,string> check){
  DateTimeOffset T(string value)=>DateTimeOffset.Parse(value);
  var state=new State{Language="zh-Hans"};var row=new Row{Zone="Eastern Standard Time",Routine=new CityRoutine{Start=22*60,End=6*60,Wake=17*60,Sleep=8*60,BreakEnabled=false,DaysOff=65}};
  row.Routine.Validate();
  check(Rules.Available(T("2026-10-23T23:00:00-04:00"),state,row),"Friday overnight contact window starts on Friday");
  check(Rules.Available(T("2026-10-24T04:26:00-04:00"),state,row),"Saturday early hours retain Friday's overnight shift");
  check(!Rules.Available(T("2026-10-25T04:26:00-04:00"),state,row),"Sunday early hours belong to Saturday's day off");
  check(!Rules.Available(T("2026-10-24T06:00:00-04:00"),state,row),"Overnight end is exclusive");
  check(!Rules.Brief(T("2026-10-24T04:26:00-04:00"),state,row).Contains("睡"),"Night-shift routine does not tell an awake friend to sleep");
  check(Rules.Short(T("2026-10-23T12:00:00-04:00"),state,row).Contains("睡"),"Daytime sleep follows the personal routine");
  var other=new Row{Zone=row.Zone};check(!Rules.Available(T("2026-10-24T04:26:00-04:00"),state,other)&&other.Routine==null,"One city's routine does not change another city's inherited defaults");
  var serialized=JsonSerializer.Serialize(row);check(JsonSerializer.Deserialize<Row>(serialized)!.Routine==row.Routine,"Personal routine survives JSON roundtrip");
  var old=JsonSerializer.Deserialize<Row>("{\"City\":\"纽约\",\"Zone\":\"Eastern Standard Time\",\"Code\":\"US\"}")!;
  check(old.Routine==null&&old.PositionScale==1,"Old city records inherit defaults without losing their identity");
  bool Reject(CityRoutine value){try{value.Validate();return false;}catch(InvalidDataException){return true;}}
  check(Reject(new CityRoutine{DaysOff=127}),"Reject an all-days-off contact calendar instead of searching forever");
  check(Reject(new CityRoutine{Start=9*60,End=10*60,BreakStart=9*60,BreakEnd=10*60}),"Reject a break that consumes all contact time");
  check(Reject(new CityRoutine{BreakStart=3*60,BreakEnd=4*60}),"Reject a break outside the contact window");
  check(Reject(new CityRoutine{Sleep=8*60,Wake=19*60,BreakEnabled=false}),"Reject contact time completely inside sleep");
  check(Reject(new CityRoutine{Wake=1440}),"24:00 is not accepted as a wake time");
  check(CityRoutine.Parse("24:00",true,out int midnight)&&midnight==1440&&!CityRoutine.Parse("24:00",false,out _)&&!CityRoutine.Parse("08:75",true,out _),"Time editor validates end-of-day and minute bounds");
  var friSat=new CityRoutine{DaysOff=96,BreakEnabled=false};check(!friSat.Workday(new DateTime(2026,10,23))&&!friSat.Workday(new DateTime(2026,10,24))&&friSat.Workday(new DateTime(2026,10,25)),"Independent Friday/Saturday rest days are supported");
  row.Routine=new CityRoutine{Start=2*60+15,End=2*60+45,DaysOff=0,SleepEnabled=false,BreakEnabled=false};
  var spring=Rules.NextContact(T("2026-03-08T06:00:00Z"),state,row);
  check(spring.HasValue&&spring.Value.Date==new DateTime(2026,3,9)&&spring.Value.Hour==2&&spring.Value.Minute==15&&spring.Value.Offset==TimeSpan.FromHours(-4),"Next-contact search skips a missing spring-forward wall time");
  row.Routine=new CityRoutine{Start=90,End=105,DaysOff=0,SleepEnabled=false,BreakEnabled=false};
  var fall=Rules.NextContact(T("2026-11-01T05:50:00Z"),state,row);
  check(fall==T("2026-11-01T06:30:00Z"),"Next contact can use the second occurrence of a fall-back hour");
  row.Routine=new CityRoutine{Start=120,End=180,DaysOff=0,SleepEnabled=false,BreakEnabled=false};
  var changed=Rules.NextContact(T("2026-11-01T05:50:00Z"),state,row);
  check(changed!=fall&&changed==T("2026-11-01T07:00:00Z"),"Changing a routine invalidates the next-contact cache key");
  var main=new ScreenArea("main",0,0,1920,1040,1);var left=new ScreenArea("left",-2560,0,0,1440,1.5);
  var group=new State{Displays=new(){main,left},Rows=new(){new Row{Group="g",Monitor="left",PositionScale=1.5,X=-1500,Y=100},new Row{Group="g",Order=1,Monitor="left",PositionScale=1.5,X=-1500,Y=154}}};
  string unchanged=JsonSerializer.Serialize(group.Rows);check(!DisplayLayout.Reconcile(group,new[]{main,left})&&JsonSerializer.Serialize(group.Rows)==unchanged,"Stable mixed-DPI topology preserves exact saved positions");
  group.Rows[0].Routine=row.Routine;var routine=group.Rows[0].Routine;var ids=group.Rows.Select(r=>r.Id).ToArray();
  check(DisplayLayout.Reconcile(group,new[]{main})&&group.Rows.All(r=>r.Monitor=="main"&&r.PositionScale==1),"Removing a negative-coordinate monitor relocates its clocks to an available screen");
  var p=DisplayLayout.Physical(group.Rows[0]);check(p.X>=main.Left&&p.Y>=main.Top&&p.X+620<=main.Right&&p.Y+104<=main.Bottom,"Recovered group fits the surviving work area");
  check(group.Rows.Select(r=>r.Id).SequenceEqual(ids)&&ReferenceEquals(group.Rows[0].Routine,routine)&&group.Rows[1].Y-group.Rows[0].Y==54,"Display recovery retains IDs, personal routine and group order");
  var before=new ScreenArea("panel",0,0,2560,1440,1);var after=new ScreenArea("panel",0,0,1920,1040,1);
  var shrink=new State{Displays=new(){before},Rows=new(){new Row{Monitor="panel",X=1900,Y=1300}}};DisplayLayout.Reconcile(shrink,new[]{after});check(shrink.Rows[0].X==1300&&shrink.Rows[0].Y==990,"Resolution shrink clamps a clock into the work area");
  var oldLeft=new ScreenArea("panel",-2560,0,0,1440,1);var newLeft=new ScreenArea("panel",-3840,0,0,2160,1.5);
  var scale=new State{Displays=new(){oldLeft},Rows=new(){new Row{Monitor="panel",PositionScale=1,X=-2000,Y=100}}};DisplayLayout.Reconcile(scale,new[]{newLeft});
  check(DisplayLayout.Physical(scale.Rows[0]).X==-3000&&scale.Rows[0].Y==100&&scale.Rows[0].PositionScale==1.5,"DPI change preserves monitor-relative DIP placement on a negative screen");
  var moved=new State{Displays=new(){new ScreenArea("panel",1920,0,3840,1080,1)},Rows=new(){new Row{Monitor="panel",X=2100,Y=100}}};DisplayLayout.Reconcile(moved,new[]{new ScreenArea("panel",-1920,0,0,1080,1)});
  check(moved.Rows[0].X==-1740,"Moving a monitor from right to left preserves its local position");
  var splitScreens=new State{Rows=new(){new Row{Group="one",Monitor="main",X=100,Y=100},new Row{Group="two",Monitor="left",X=100,Y=154}}};check(Layout.Find(splitScreens,new(){splitScreens.Rows[1]})==null,"Overlapping logical coordinates on different monitors cannot spuriously snap");
 }
}
