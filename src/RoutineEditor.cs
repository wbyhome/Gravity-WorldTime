using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WorldTime;
public sealed class RoutineEditor : Window {
 readonly Host host;readonly Row row;readonly StackPanel body=new();readonly CheckBox[] days=new CheckBox[7];
 public readonly CheckBox Inherit=new();public readonly CheckBox SleepEnabled=new();public readonly CheckBox BreakEnabled=new();
 public readonly TextBox Wake=new(),Sleep=new(),Start=new(),End=new(),BreakStart=new(),BreakEnd=new();
 public readonly TextBlock Error=new(){Foreground=Brushes.Firebrick,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)};
 public RoutineEditor(Host host,Row row){
  this.host=host;this.row=row;host.Manager.Reveal();Owner=host.Manager;Title="WorldTime · "+host.T("City routine")+" · "+L10n.CityName(row,host.S.Language);
  Width=540;SizeToContent=SizeToContent.Height;ShowInTaskbar=false;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;FontFamily=host.Manager.FontFamily;FontSize=14;Background=new SolidColorBrush(Color.FromRgb(243,246,249));
  var root=new StackPanel{Margin=new Thickness(22)};Content=root;
  root.Children.Add(new TextBlock{Text=host.T("All times below are local to this city. Overnight ranges are allowed."),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
  Inherit.Content=host.T("Use default schedule");Inherit.IsChecked=row.Routine==null;root.Children.Add(Inherit);root.Children.Add(body);
  var initial=row.Routine??CityRoutine.FromDefaults(host.S);
  void Pair(string a,TextBox left,string b,TextBox right,int x,int y){var grid=new Grid{Margin=new Thickness(0,8,0,0)};grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition());void Field(string label,TextBox box,int value,int col){var p=new StackPanel{Margin=new Thickness(col==0?0:10,0,col==0?10:0,0)};Grid.SetColumn(p,col);p.Children.Add(new TextBlock{Text=host.T(label)});box.Text=CityRoutine.Time(value);p.Children.Add(box);grid.Children.Add(p);}Field(a,left,x,0);Field(b,right,y,1);body.Children.Add(grid);}
  SleepEnabled.Content=host.T("Respect sleep hours");SleepEnabled.IsChecked=initial.SleepEnabled;SleepEnabled.Margin=new Thickness(0,12,0,0);body.Children.Add(SleepEnabled);Pair("Wake time",Wake,"Bedtime",Sleep,initial.Wake,initial.Sleep);
  Pair("Contact starts",Start,"Contact ends",End,initial.Start,initial.End);
  BreakEnabled.Content=host.T("Include a break");BreakEnabled.IsChecked=initial.BreakEnabled;BreakEnabled.Margin=new Thickness(0,12,0,0);body.Children.Add(BreakEnabled);Pair("Break starts",BreakStart,"Break ends",BreakEnd,initial.BreakStart,initial.BreakEnd);
  body.Children.Add(new TextBlock{Text=host.T("Days off (for overnight shifts, the start day counts)"),Margin=new Thickness(0,12,0,6),TextWrapping=TextWrapping.Wrap});var week=new WrapPanel();body.Children.Add(week);var names=new[]{"Sun","Mon","Tue","Wed","Thu","Fri","Sat"};
  for(int i=0;i<7;i++){days[i]=new CheckBox{Content=host.T(names[i]),IsChecked=(initial.DaysOff&(1<<i))!=0,Margin=new Thickness(0,0,12,0)};week.Children.Add(days[i]);}
  root.Children.Add(Error);var save=new Button{Content=host.T("Save"),Height=34,Margin=new Thickness(0,12,0,0)};root.Children.Add(save);save.Click+=(_,_)=>SaveChanges();
  Inherit.Checked+=(_,_)=>body.IsEnabled=false;Inherit.Unchecked+=(_,_)=>body.IsEnabled=true;body.IsEnabled=Inherit.IsChecked!=true;
 }
 public int DaysOff{get=>Enumerable.Range(0,7).Where(i=>days[i].IsChecked==true).Aggregate(0,(mask,i)=>mask|(1<<i));set{for(int i=0;i<7;i++)days[i].IsChecked=(value&(1<<i))!=0;}}
 public bool SaveChanges(){
  CityRoutine? routine=null;
  try{
   if(Inherit.IsChecked!=true){
    if(!CityRoutine.Parse(Wake.Text,false,out int wake)||!CityRoutine.Parse(Sleep.Text,false,out int sleep)||!CityRoutine.Parse(Start.Text,false,out int start)||!CityRoutine.Parse(End.Text,true,out int end)||!CityRoutine.Parse(BreakStart.Text,false,out int bs)||!CityRoutine.Parse(BreakEnd.Text,true,out int be))throw new System.IO.InvalidDataException("Use HH:mm; only an end time may be 24:00.");
    routine=new CityRoutine{Wake=wake,Sleep=sleep,Start=start,End=end,BreakStart=bs,BreakEnd=be,BreakEnabled=BreakEnabled.IsChecked==true,SleepEnabled=SleepEnabled.IsChecked==true,DaysOff=DaysOff};routine.Validate();
   }
   row.Routine=routine;host.Sync();if(!host.Save())return false;Close();return true;
  }catch(System.IO.InvalidDataException e){Error.Text=host.T(e.Message);return false;}
 }
}
