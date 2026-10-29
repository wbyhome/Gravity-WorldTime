using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace WorldTime;
// Both city-add entry points use the same explicit-selection offline component.
public sealed class RulerContactDialog : Window {
 public readonly CitySearchControl Search;
 public RulerContactDialog(Host host,RulerWindow ruler,int? reference=null,Row? editing=null){
  Title="WorldTime · "+host.T(editing==null?"Add contact":"Change city or time zone");Width=600;ShowInTaskbar=false;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=host.Manager.FontFamily;FontSize=13;Background=new SolidColorBrush(Color.FromRgb(242,244,247));var panel=new StackPanel{Margin=new Thickness(20)};Content=panel;
  MaxHeight=SystemParameters.WorkArea.Height-32;Content=new ScrollViewer{Content=panel,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};if(reference.HasValue)panel.Children.Add(new TextBlock{Text=string.Format(host.T("Reference {0}; choose a city explicitly. The marker follows its actual current local time."),CityRoutine.Time(reference.Value)),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,8)});
  var name=new TextBox{MaxLength=80,Text=editing==null?"":L10n.CityName(editing,host.S.Language),Margin=new Thickness(0,4,0,12)};string original=name.Text;panel.Children.Add(new TextBlock{Text=host.T("Contact name")});panel.Children.Add(name);Search=new(host);panel.Children.Add(Search);
  var avatar=new CheckBox{Content=host.T("Choose avatar after adding"),IsChecked=true,Margin=new Thickness(0,10,0,4)};if(editing==null)panel.Children.Add(avatar);
  var buttons=new WrapPanel{Margin=new Thickness(0,12,0,0)};panel.Children.Add(buttons);var save=new Button{Content=host.T(editing==null?"Add contact":"Save"),Padding=new Thickness(12,6,12,6),IsEnabled=false};buttons.Children.Add(save);var cancel=new Button{Content=host.T("Cancel"),Padding=new Thickness(12,6,12,6),Margin=new Thickness(8,0,0,0)};buttons.Children.Add(cancel);cancel.Click+=(_,_)=>Close();Search.SelectionChanged+=()=>save.IsEnabled=Search.CanAdd;
  void Confirm(){var selected=Search.MakeSelection();if(selected==null)return;Row row=editing??selected;var old=(row.City,row.Code,row.Zone,row.PlaceId,row.Iana,row.Name);
   if(editing!=null){row.City=selected.City;row.Code=selected.Code;row.Zone=selected.Zone;row.PlaceId=selected.PlaceId;row.Iana=selected.Iana;}
   row.Name=editing!=null&&editing.Name.Length==0&&name.Text==original?"":name.Text.Trim();if(editing==null){if(!host.AddRow(row,null))return;}else{host.Sync();if(!host.Save()){(row.City,row.Code,row.Zone,row.PlaceId,row.Iana,row.Name)=old;host.Sync();return;}}Close();if(editing==null&&avatar.IsChecked==true)ruler.ChooseAvatar(row.Id);
  }
  save.Click+=(_,_)=>Confirm();Search.ConfirmRequested+=Confirm;Loaded+=(_,_)=>Search.SearchBox.Focus();Closed+=(_,_)=>Search.Stop();
 }
}
