using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace WorldTime;
public sealed class CityPicker : Window {
 readonly Host host;readonly Row? anchor;public readonly CitySearchControl Search;
 public TextBox SearchBox=>Search.SearchBox;public ComboBox CountrySelector=>Search.CountrySelector;public ListBox CityList=>Search.CityList;public ComboBox ZoneSelector=>Search.ZoneSelector;public TextBlock Results=>Search.Results;
 public readonly Button AddButton=new(){Height=34,Margin=new Thickness(0,12,0,0)};
 public CityPicker(Host host,Row? anchor){
  this.host=host;this.anchor=anchor;host.Manager.Reveal();Owner=host.Manager;Title="WorldTime · "+host.T("Add city");Width=600;ShowInTaskbar=false;ResizeMode=ResizeMode.NoResize;SizeToContent=SizeToContent.Height;WindowStartupLocation=WindowStartupLocation.CenterOwner;Background=new SolidColorBrush(Color.FromRgb(242,244,247));FontFamily=host.Manager.FontFamily;FontSize=14;
  MaxHeight=SystemParameters.WorkArea.Height-32;var panel=new StackPanel{Margin=new Thickness(22)};Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};Search=new(host);panel.Children.Add(Search);AddButton.Content=host.T("Add selected city or time zone");AddButton.IsEnabled=false;panel.Children.Add(AddButton);Search.SelectionChanged+=()=>AddButton.IsEnabled=Search.CanAdd;Search.ConfirmRequested+=()=>AddSelected();AddButton.Click+=(_,_)=>AddSelected();Loaded+=(_,_)=>SearchBox.Focus();Closed+=(_,_)=>Search.Stop();
 }
 public void Filter()=>_=Search.RefreshAsync();
 public bool AddSelected(){Row? row=Search.MakeSelection();if(row==null||!host.AddRow(row,anchor))return false;Close();return true;}
}
