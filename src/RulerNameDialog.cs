using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace WorldTime;
public sealed class RulerNameDialog : Window {
 public RulerNameDialog(Host host,RulerWindow ruler,Row? row){
  Title="WorldTime · "+host.T(row==null?"My display name…":"Contact name");Width=370;SizeToContent=SizeToContent.Height;ShowInTaskbar=true;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=host.Manager.FontFamily;FontSize=13;Background=new SolidColorBrush(Color.FromRgb(242,244,247));
  var panel=new StackPanel{Margin=new Thickness(18)};Content=panel;panel.Children.Add(new TextBlock{Text=host.T("A display name only; city, time zone, avatar and routine stay unchanged."),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
  var name=new TextBox{Text=row==null?ruler.SelfName:row.Name,MaxLength=80,MinHeight=28};panel.Children.Add(name);panel.Children.Add(new TextBlock{Text=host.T("Leave blank to use the default name."),Margin=new Thickness(0,7,0,12)});
  var save=new Button{Content=host.T("Save"),Padding=new Thickness(8)};panel.Children.Add(save);var cancel=new Button{Content=host.T("Cancel"),Padding=new Thickness(8),Margin=new Thickness(0,7,0,0),IsCancel=true};panel.Children.Add(cancel);
  var error=new TextBlock{Foreground=Brushes.Firebrick,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,8,0,0)};panel.Children.Add(error);
  save.Click+=(_,_)=>{try{if(row==null?ruler.RenameSelf(name.Text):ruler.RenameContact(row,name.Text))Close();else error.Text=host.T("Could not save. The previous name was kept.");}catch(Exception e){error.Text=e.Message;}};cancel.Click+=(_,_)=>Close();Loaded+=(_,_)=>{name.Focus();name.SelectAll();};
 }
}
