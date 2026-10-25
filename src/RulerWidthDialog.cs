using System;
using System.Windows;
using System.Windows.Controls;
namespace WorldTime;
public sealed class RulerWidthDialog : Window {
 public RulerWidthDialog(Host host,RulerWindow ruler){
  Title="WorldTime · "+host.T("Custom width…");Width=380;ShowInTaskbar=true;ResizeMode=ResizeMode.NoResize;SizeToContent=SizeToContent.Height;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=host.Manager.FontFamily;
  double old=ruler.Width;bool saved=false;var p=new StackPanel{Margin=new Thickness(18)};Content=p;var text=new TextBlock();p.Children.Add(text);var slider=new Slider{Minimum=Math.Min(300,ruler.WidthLimit),Maximum=ruler.WidthLimit,Value=old,TickFrequency=1,Margin=new Thickness(0,14,0,14)};p.Children.Add(slider);void Caption()=>text.Text=string.Format(host.T("Ruler width: {0} DIP"),(int)Math.Round(slider.Value));Caption();slider.ValueChanged+=(_,_)=>{Caption();ruler.SetRulerWidth(slider.Value,false);};p.Children.Add(new TextBlock{Text=host.T("Drag the right end to resize."),TextWrapping=TextWrapping.Wrap});
  var save=new Button{Content=host.T("Save"),Padding=new Thickness(8),Margin=new Thickness(0,16,0,6)};p.Children.Add(save);var cancel=new Button{Content=host.T("Cancel"),Padding=new Thickness(8),IsCancel=true};p.Children.Add(cancel);save.Click+=(_,_)=>{if(ruler.SaveView("width-dialog")){saved=true;Close();}};cancel.Click+=(_,_)=>Close();Closed+=(_,_)=>{if(!saved)ruler.SetRulerWidth(old,false);};
 }
}
