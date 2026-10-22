using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WorldTime;
public sealed class CityPicker : Window {
 readonly Host host;readonly Row? anchor;
 public readonly TextBox SearchBox=new(){Margin=new Thickness(0,0,0,8)};
 public readonly ComboBox CountrySelector=new(){Height=30,Margin=new Thickness(0,0,0,10),DisplayMemberPath="Label",IsTextSearchEnabled=true};
 public readonly ListBox CityList=new(){Height=250};
 public readonly ComboBox ZoneSelector=new(){Height=28,DisplayMemberPath="Id",IsTextSearchEnabled=true};
 public readonly TextBlock Results=new(){Margin=new Thickness(0,8,0,10),TextWrapping=TextWrapping.Wrap,Foreground=Brushes.SlateGray};
 public readonly Button AddButton=new(){Height=34,Margin=new Thickness(0,12,0,0)};
 public CityPicker(Host host,Row? anchor){
  this.host=host;this.anchor=anchor;host.Manager.Reveal();Owner=host.Manager;
  Title="WorldTime · "+host.T("Add city");Width=560;ShowInTaskbar=false;ResizeMode=ResizeMode.NoResize;SizeToContent=SizeToContent.Height;WindowStartupLocation=WindowStartupLocation.CenterOwner;
  Background=new SolidColorBrush(Color.FromRgb(242,244,247));FontFamily=host.Manager.FontFamily;FontSize=14;
  var panel=new StackPanel{Margin=new Thickness(22)};Content=panel;
  panel.Children.Add(new TextBlock{Text=host.T("Country or region"),Margin=new Thickness(0,0,0,4)});panel.Children.Add(CountrySelector);
  CountrySelector.ItemsSource=new[]{new CountryChoice("",host.T("All countries and regions"))}.Concat(Catalog.Countries(host.S.Language)).ToArray();CountrySelector.SelectedIndex=0;
  panel.Children.Add(new TextBlock{Text=host.T("Search country or city"),Margin=new Thickness(0,0,0,4)});panel.Children.Add(SearchBox);panel.Children.Add(CityList);panel.Children.Add(Results);
  panel.Children.Add(new TextBlock{Text=host.T("Or choose a Windows time zone"),Margin=new Thickness(0,0,0,4)});panel.Children.Add(ZoneSelector);ZoneSelector.ItemsSource=TimeZoneInfo.GetSystemTimeZones();
  AddButton.Content=host.T("Add selected city or time zone");panel.Children.Add(AddButton);
  void Enable()=>AddButton.IsEnabled=CityList.SelectedItem!=null||ZoneSelector.SelectedItem!=null;
  SearchBox.TextChanged+=(_,_)=>Filter();CountrySelector.SelectionChanged+=(_,_)=>Filter();
  CityList.SelectionChanged+=(_,_)=>{if(CityList.SelectedItem!=null)ZoneSelector.SelectedIndex=-1;Enable();};
  ZoneSelector.SelectionChanged+=(_,_)=>{if(ZoneSelector.SelectedItem!=null)CityList.SelectedIndex=-1;Enable();};
  AddButton.Click+=(_,_)=>AddSelected();Filter();
  Loaded+=(_,_)=>SearchBox.Focus();
 }
 public void Filter(){
  ZoneSelector.SelectedIndex=-1;
  string country=(CountrySelector.SelectedItem as CountryChoice)?.Code??"";var cities=Catalog.Search(SearchBox.Text,country).ToArray();
  CityList.Items.Clear();
  foreach(var city in cities){
   string local=Catalog.CityName(city,host.S.Language);string english=L10n.Resolve(host.S.Language)=="en"||local==city.En?"":" / "+city.En;
   CityList.Items.Add(new ListBoxItem{Tag=city,Content=new TextBlock{Text=city.Code+"  "+Catalog.CountryName(city,host.S.Language)+" · "+local+english,TextWrapping=TextWrapping.Wrap},ToolTip=city.Zone});
  }
  if(cities.Length>0)CityList.SelectedIndex=0;
  Results.Text=cities.Length==0?host.T("No matches. Try another country or city."):string.Format(host.T("{0} matching places · {1} countries/regions available offline"),cities.Length,Catalog.Supported.Select(c=>c.Code).Distinct().Count());
  AddButton.IsEnabled=CityList.SelectedItem!=null||ZoneSelector.SelectedItem!=null;
 }
 public bool AddSelected(){
  Row row;if(ZoneSelector.SelectedItem is TimeZoneInfo zone)row=new Row{City=zone.Id,Zone=zone.Id,Code="◎"};else if(CityList.SelectedItem is ListBoxItem item&&item.Tag is City city)row=host.Make(city);else return false;
  host.AddRow(row,anchor);Close();return true;
 }
}
