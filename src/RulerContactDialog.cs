using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WorldTime;
// Explicit selection; a clicked clock hour is only a sorting reference.
public sealed class RulerContactDialog : Window {
 readonly Host host;readonly RulerWindow ruler;readonly Row? editing;readonly int? reference;
 readonly TextBox search=new(),name=new(){MaxLength=80};readonly ListBox cities=new(){Height=220};readonly ComboBox zones=new(){DisplayMemberPath="Id",Height=28};readonly TextBlock summary=new(){TextWrapping=TextWrapping.Wrap};
 public RulerContactDialog(Host host,RulerWindow ruler,int? reference=null,Row? editing=null){
  this.host=host;this.ruler=ruler;this.editing=editing;this.reference=reference;Title="WorldTime · "+host.T(editing==null?"Add contact":"Change city or time zone");Width=560;ShowInTaskbar=false;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=host.Manager.FontFamily;FontSize=13;Background=new SolidColorBrush(Color.FromRgb(242,244,247));var panel=new StackPanel{Margin=new Thickness(18)};Content=panel;
  void Label(string key)=>panel.Children.Add(new TextBlock{Text=host.T(key),Margin=new Thickness(0,8,0,4)});
  if(reference.HasValue){summary.Text=string.Format(host.T("Reference {0}; choose a city explicitly. The marker follows its actual current local time."),CityRoutine.Time(reference.Value));panel.Children.Add(summary);}
  Label("Contact name");name.Text=editing==null?"":L10n.CityName(editing,host.S.Language);string originalNameText=name.Text;panel.Children.Add(name);Label("Search country or city");panel.Children.Add(search);panel.Children.Add(cities);Label("Or choose a Windows time zone");zones.ItemsSource=TimeZoneInfo.GetSystemTimeZones();panel.Children.Add(zones);
  var selectAvatar=new CheckBox{Content=host.T("Choose avatar after adding"),IsChecked=true,Margin=new Thickness(0,10,0,4)};if(editing==null)panel.Children.Add(selectAvatar);
  var buttons=new WrapPanel{Margin=new Thickness(0,12,0,0)};panel.Children.Add(buttons);var save=new Button{Content=host.T(editing==null?"Add contact":"Save"),Padding=new Thickness(12,6,12,6),IsEnabled=false};buttons.Children.Add(save);var cancel=new Button{Content=host.T("Cancel"),Padding=new Thickness(12,6,12,6),Margin=new Thickness(8,0,0,0)};buttons.Children.Add(cancel);cancel.Click+=(_,_)=>Close();
  void Enable()=>save.IsEnabled=cities.SelectedItem!=null||zones.SelectedItem!=null;
  cities.SelectionChanged+=(_,_)=>{if(cities.SelectedItem!=null)zones.SelectedIndex=-1;Enable();};zones.SelectionChanged+=(_,_)=>{if(zones.SelectedItem!=null)cities.SelectedIndex=-1;Enable();};
  void Filter(){cities.Items.Clear();var now=DateTimeOffset.UtcNow;var candidates=Catalog.Search(search.Text);if(reference.HasValue)candidates=candidates.OrderBy(c=>{var local=Catalog.Local(c.Zone,now);int delta=Math.Abs(local.Hour*60+local.Minute-reference.Value);return Math.Min(delta,1440-delta);});foreach(var c in candidates)cities.Items.Add(new ListBoxItem{Tag=c,Content=c.Code+" · "+Catalog.CityName(c,host.S.Language)+" · "+Catalog.CountryName(c,host.S.Language)});Enable();}
  search.TextChanged+=(_,_)=>Filter();Filter();save.Click+=(_,_)=>{
   City? city=(cities.SelectedItem as ListBoxItem)?.Tag as City;var zone=zones.SelectedItem as TimeZoneInfo;if(city==null&&zone==null)return;Row row=editing??(city!=null?host.Make(city):new Row{City=zone!.Id,Code="◎",Zone=zone.Id});
   if(editing!=null){if(city!=null){row.City=city.Zh;row.Code=city.Code;row.Zone=city.Zone;}else{row.City=zone!.Id;row.Code="◎";row.Zone=zone.Id;}}row.Name=editing!=null&&editing.Name.Length==0&&name.Text==originalNameText?"":name.Text.Trim();if(editing==null)host.AddRow(row,null);else{host.Sync();host.Save();}Close();if(editing==null&&selectAvatar.IsChecked==true)ruler.ChooseAvatar(row.Id);
  };
 }
}
