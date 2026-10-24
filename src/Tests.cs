using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
namespace WorldTime;
public static class Tests {
 public static void Run(string[] args){var lines=new List<string>();int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);lines.Add("PASS "+name);count++;}
 try{
 DateTimeOffset U(string x)=>DateTimeOffset.Parse(x);
 Check(Catalog.Local("Eastern Standard Time",U("2026-01-15T12:00:00Z")).Hour==7,"New York winter UTC-5");
 Check(Catalog.Local("Eastern Standard Time",U("2026-07-15T12:00:00Z")).Hour==8,"New York summer UTC-4");
 Check(Catalog.Local("Eastern Standard Time",U("2026-03-08T06:59:00Z")).ToString("HH:mm")=="01:59" && Catalog.Local("Eastern Standard Time",U("2026-03-08T07:00:00Z")).ToString("HH:mm")=="03:00","DST spring skips hour");
 var fallA=Catalog.Local("Eastern Standard Time",U("2026-11-01T05:30:00Z"));var fallB=Catalog.Local("Eastern Standard Time",U("2026-11-01T06:30:00Z"));Check(fallA.Hour==fallB.Hour&&fallA.Offset!=fallB.Offset,"DST repeated hour offsets");
 Check(Catalog.Local("Nepal Standard Time",U("2026-01-15T00:00:00Z")).ToString("HH:mm")=="05:45","Nepal quarter-hour offset");
 Check(Catalog.Local("India Standard Time",U("2026-01-15T00:00:00Z")).ToString("HH:mm")=="05:30","India half-hour offset");
 Check(Catalog.Local("Tokyo Standard Time",U("2026-01-15T20:00:00Z")).Day==16,"Tokyo next date");
 Check(Catalog.Local("Pacific Standard Time",U("2026-01-15T01:00:00Z")).Day==14,"Los Angeles previous date");
 Check(Catalog.Local("AUS Eastern Standard Time",U("2026-01-15T00:00:00Z")).Hour==11 && Catalog.Local("AUS Eastern Standard Time",U("2026-07-15T00:00:00Z")).Hour==10,"Southern hemisphere DST");
 foreach(var c in Catalog.All)Check(Catalog.Local(c.Zone,DateTimeOffset.UtcNow).Offset.TotalHours is >=-14 and <=14,"Offline catalog zone "+c.En);
 Check(Catalog.All.Length>=400&&Catalog.All.Select(c=>c.Code).Distinct().Count()>=240,"Catalog covers hundreds of places and countries/regions");
 Check(Catalog.All.All(c=>new[]{c.Zh,c.En,c.Hant,c.Ja,c.CountryZh,c.CountryEn,c.CountryHant,c.CountryJa,c.Zone,c.Code}.All(v=>!string.IsNullOrWhiteSpace(v))),"Every location has city/country display names in all four languages");
 Check(Catalog.All.Select(c=>c.Code+"|"+c.Zone+"|"+c.Zh).Distinct().Count()==Catalog.All.Length,"No duplicate city/country/zone identities");
 Check(Catalog.Search("韩国").Any(c=>c.En=="Seoul")&&Catalog.Search("韓國").Any(c=>c.En=="Seoul"),"Country search works in Simplified and Traditional Chinese");
 Check(Catalog.Search("Thailand").Any(c=>c.En=="Bangkok")&&Catalog.Search("日本").Any(c=>c.En=="Tokyo"),"Country search works across English and Japanese");
 Check(Catalog.Search("USA").Any()&&Catalog.Search("US").All(c=>c.Code=="US")&&Catalog.Search("UK").All(c=>c.Code=="GB"),"Country codes and common aliases resolve to the intended country");
 Check(Catalog.Search("Sao Paulo").Any(c=>c.Zh=="圣保罗"),"Unaccented search finds Sao Paulo");
 Check(Catalog.Search("United States New York").Any(c=>c.Zh=="纽约"),"Combined country and city search uses every token");
 Check(Catalog.Search("", "CA").Select(c=>c.Zone).Distinct().Count()>3,"Multi-zone countries retain multiple local clock choices");
 Check(!Catalog.Search("Thailand","KR").Any(),"Country filter intersects search rather than adding unrelated matches");
 Check(Catalog.Find(new Row{City="纽约",Zone="Eastern Standard Time",Code="US"})?.En=="New York"&&L10n.CityName(new Row{City="纽约",Zone="Eastern Standard Time",Code="US"},"ja")=="ニューヨーク","Original saved rows keep their localized city names");
 var s=new State();Check(Rules.Available(U("2026-10-19T10:00:00+09:00"),s),"Monday contact window");Check(!Rules.Available(U("2026-10-17T10:00:00+09:00"),s),"Saturday excluded");Check(!Rules.Available(U("2026-10-18T10:00:00+09:00"),s),"Sunday excluded");Check(!Rules.Available(U("2026-10-19T12:00:00+09:00"),s),"Lunch excluded");Check(!Rules.Available(U("2026-10-19T18:00:00+09:00"),s),"End exclusive");
 Check(Rules.Text(U("2026-10-23T23:00:00Z"),s).Contains("10-26 09:00"),"Friday late suggests Monday");Check(Rules.Text(U("2026-10-19T12:10:00Z"),s).Contains("10-19 13:00"),"Lunch suggests same day after break");s.Life=false;s.Advice=false;Check(Rules.Text(DateTimeOffset.UtcNow,s)=="","Both hints hidden");s=new State();
 var custom=new State{Start=9,End=18,Lunch=9};Check(Rules.Text(U("2026-10-19T07:00:00Z"),custom).Contains("10-19 10:00"),"Contact window skips configured opening break"); bool invalid=false;try{Store.Validate(new State{Start=9,End=10,Lunch=9});}catch(InvalidDataException){invalid=true;}Check(invalid,"Reject contact schedule with no available hour");
 var a=new Row{X=100,Y=100,Group="a"};var b=new Row{X=109,Y=160,Group="b"};s.Rows.AddRange(new[]{a,b});var c1=Layout.Find(s,new(){b});Check(c1!=null&&!c1.Before,"Snap below within threshold");Layout.Merge(s,new(){b},c1!);Check(a.Group==b.Group&&b.X==100&&b.Y==154&&b.Order==1,"Merge group exact geometry");
 var friend=new State{Language="zh-Hans"};var sleepy=U("2026-10-22T04:26:00-07:00");
 Check(DayPart.Key(sleepy.Hour)=="Wee hours"&&L10n.Text(friend.Language,DayPart.Key(sleepy.Hour))=="凌晨","04:26 is explicitly labelled wee hours");
 Check(Rules.Short(sleepy,friend).Contains("睡得正香")&&Rules.ContactLine(sleepy,friend).Contains("09:00"),"Sleep estimate and destination-local contact time are both present");
 Check(Rules.Short(U("2026-10-22T07:26:00-04:00"),friend).Contains("被窝"),"Early morning is tentative waking copy, not a workday invitation");
 Check(Rules.Short(U("2026-10-22T19:26:00+08:00"),friend).Contains("晚饭")&&DayPart.Key(19)=="Night","19:26 suggests dinner rather than the end of the day");
 Check(Rules.ContactLine(U("2026-10-22T12:26:00+08:00"),friend).Contains("13:00"),"Lunch contact line uses the configured end of the break");
 Check(Rules.Short(U("2026-10-24T04:26:00Z"),friend).Contains("睡得正香"),"Weekend does not mask a sleep warning at 04:26");
 Check(!Rules.ContactLine(U("2026-10-23T23:26:00Z"),friend).Contains("10-26"),"Friday-night friend copy does not postpone a personal hello to Monday");
 var night=new State{Language="zh-Hans",Start=0,End=7,Lunch=12};
 Check(!Rules.Brief(sleepy,night).Contains("睡")&&Rules.ContactLine(sleepy,night).Contains("方便"),"Configured night contact window overrides a generic sleep assumption");
 friend.Life=false;Check(!Rules.Brief(sleepy,friend).Contains("睡"),"Life estimates remain disabled when only contact advice is enabled");
 friend.Advice=false;Check(Rules.Brief(sleepy,friend)==""&&Rules.ContactLine(sleepy,friend)=="","Disabling both hints hides both lines");
 Check(FriendHints.Complete,"All friendly hint variants cover four languages and three widths");
 Layout.Detach(s,b);b.X+=100;Check(a.Group!=b.Group&&Layout.Find(s,new(){b})==null,"Detach escapes magnet");s.Snap=false;b.X=100;Check(Layout.Find(s,new(){b})==null,"Snap switch disables candidates");s.Snap=true;
 b.Y=46;var c2=Layout.Find(s,new(){b});Check(c2!=null&&c2.Before,"Snap above");Layout.Merge(s,new(){b},c2!);Check(b.Order==0&&a.Order==1&&a.Y-b.Y==54,"Prepend keeps order");
 var d=new Row{Group="d",X=100,Y=154};var e=new Row{Group="d",Order=1,X=100,Y=208};s.Rows.AddRange(new[]{d,e});var c3=Layout.Find(s,new(){d,e});Check(c3!=null,"Two-group candidate");Layout.Merge(s,new(){d,e},c3!);Check(Layout.Group(s,a).Select(x=>x.Id).SequenceEqual(new[]{b.Id,a.Id,d.Id,e.Id}),"Merge two groups preserves sequence");
 var round=JsonSerializer.Deserialize<State>(JsonSerializer.Serialize(s))!;Check(JsonSerializer.Serialize(round)==JsonSerializer.Serialize(s),"JSON roundtrip exact layout");
 string temp=Path.Combine(Path.GetTempPath(),"WorldTime-test-"+Guid.NewGuid().ToString("N"));try{using(var disk=new Store(temp)){disk.Save(s);disk.Save(round);Check(File.Exists(disk.FilePath+".bak"),"Atomic save retains previous generation");Check(JsonSerializer.Serialize(disk.Load())==JsonSerializer.Serialize(s),"Disk restart restores geometry and membership");bool locked=false;try{using var other=new Store(temp);}catch(IOException){locked=true;}Check(locked,"Concurrent writer rejected");File.WriteAllText(disk.FilePath,"broken");bool rejected=false;try{disk.Load();}catch(JsonException){rejected=true;}Check(rejected&&File.ReadAllText(disk.FilePath)=="broken","Corrupt state rejected without overwrite");}}finally{System.IO.Directory.Delete(temp,true);}
 var screen=new ScreenArea("negative",-1920,-200,0,880,1.5);
 foreach(string edge in DockGeometry.Edges){var tab=DockGeometry.Tab(screen,edge,1);Check(tab.Left>=screen.Left && tab.Right<=screen.Right && tab.Top>=screen.Top && tab.Bottom<=screen.Bottom,"Scaled negative-coordinate edge geometry "+edge);}
 var dockState=new State();dockState.Rows.Add(new Row{Group="saved"});dockState.Docked.Add(new DockedGroup{Group="saved",Edge="left",Monitor="negative",Offset=.75});Check(JsonSerializer.Deserialize<State>(JsonSerializer.Serialize(dockState))!.Docked.Single().Offset==.75,"Folded monitor and offset JSON roundtrip");
 bool badDock=false;try{Store.Validate(new State{Docked=new(){new DockedGroup{Group="bad",Edge="invalid"}}});}catch(InvalidDataException){badDock=true;}Check(badDock,"Reject unsupported folded edge");
 var stack=new State();for(int i=0;i<4;i++)stack.Rows.Add(new Row{Group="stack",Order=i,X=140,Y=180+i*Layout.Step});
 Check(Layout.DragSelection(stack,stack.Rows[0],false).Count==4,"First row body carries whole stack");
 Check(Layout.DragSelection(stack,stack.Rows[0],true).Count==4,"First header carries whole stack");
 Check(Layout.DragSelection(stack,stack.Rows[1],false).Count==0,"Lower row body cannot start a split");
 var suffix=Layout.DragSelection(stack,stack.Rows[1],true);Check(suffix.SequenceEqual(stack.Rows.Skip(1)),"Second header selects itself and every following row");
 Check(Layout.DragSelection(stack,stack.Rows[^1],true).Single()==stack.Rows[^1],"Last header selects only the last clock");
 var coordinates=stack.Rows.Select(r=>(r.X,r.Y,r.Width)).ToArray();Layout.SplitForDrag(stack,suffix);
 Check(stack.Rows[0].Group=="stack"&&suffix.Select(r=>r.Group).Distinct().Count()==1&&suffix[0].Group!="stack","Suffix split preserves prefix and creates one group");
 Check(suffix.Select(r=>r.Order).SequenceEqual(new[]{0,1,2}),"Split suffix gets contiguous order");
 Check(stack.Rows.Select(r=>(r.X,r.Y,r.Width)).SequenceEqual(coordinates),"Splitting changes no saved positions or widths");
 string same=suffix[0].Group;Layout.SplitForDrag(stack,suffix);Check(suffix.All(r=>r.Group==same),"Whole group move does not split again");
 var a1=new ScreenArea("main",0,0,1920,1040,1);
 Check(DockGeometry.Contact(a1,new System.Windows.Rect(3,220,620,158))=="left","Near left edge requests automatic folding");
 Check(DockGeometry.Contact(a1,new System.Windows.Rect(1300,220,620,158))=="right","Near right edge requests automatic folding");
 Check(DockGeometry.Contact(a1,new System.Windows.Rect(600,2,620,158))=="top","Near top edge requests automatic folding");
 Check(DockGeometry.Contact(a1,new System.Windows.Rect(600,883,620,158))=="bottom","Crossing bottom edge requests automatic folding");
 Check(DockGeometry.Contact(a1,new System.Windows.Rect(600,300,620,158))==null,"Middle of screen does not auto-fold");
 Check(DockGeometry.Contact(a1,new System.Windows.Rect(3000,300,620,158))==null,"An unrelated monitor rectangle does not auto-fold here");
 Check(DockGeometry.Contact(screen,new System.Windows.Rect(-1900,100,700,160))=="left","Auto-fold threshold scales on negative-coordinate monitor");
 var clock=System.Diagnostics.Stopwatch.StartNew();var large=new State();for(int i=0;i<500;i++)large.Rows.Add(new Row{X=i*5,Y=i*54});for(int i=0;i<100;i++)Layout.Find(large,new(){large.Rows[0]});clock.Stop();lines.Add("BENCH 500 rows, 100 candidate searches: "+clock.ElapsedMilliseconds+" ms (model only)");
 FeatureChecks.Run(Check);
 TimeZoneChecks.Run(Check);
 lines.Add("TOTAL "+count+" PASS");Environment.ExitCode=0;
 }catch(Exception e){lines.Add(e.ToString());Environment.ExitCode=1;}
 string dest=args.Contains("--report")?args[Array.IndexOf(args,"--report")+1]:"test-results.txt";File.WriteAllLines(dest,lines);
 }
}
