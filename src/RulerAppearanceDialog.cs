using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace WorldTime;
// Transparency and language save to separate files through separate dialogs.
public sealed class RulerAppearanceDialog : Window {
 public RulerAppearanceDialog(Host host,RulerWindow ruler,bool opacityOnly=false){
  Title="WorldTime · "+host.T(opacityOnly?"Adjust transparency…":"Language");Width=opacityOnly?420:360;ShowInTaskbar=true;ResizeMode=ResizeMode.NoResize;SizeToContent=SizeToContent.Height;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=host.Manager.FontFamily;FontSize=13;Background=new SolidColorBrush(Color.FromRgb(242,244,247));var panel=new StackPanel{Margin=new Thickness(18)};Content=panel;bool applied=false;double old=ruler.BackgroundOpacity;
  var language=new ComboBox{ItemsSource=L10n.Names,SelectedIndex=Array.IndexOf(L10n.Languages,host.S.Language),Height=28};
  if(opacityOnly){var caption=new TextBlock();panel.Children.Add(caption);var slider=new Slider{Minimum=0,Maximum=100,Value=(1-old)*100,TickFrequency=5,IsSnapToTickEnabled=true,Margin=new Thickness(0,12,0,8)};panel.Children.Add(slider);void Caption()=>caption.Text=string.Format(host.T("Background transparency: {0}%"),(int)Math.Round(slider.Value));Caption();slider.ValueChanged+=(_,_)=>{Caption();ruler.SetBackgroundOpacity(1-slider.Value/100,false);};panel.Children.Add(new TextBlock{Text=host.T("0% is opaque; 100% is transparent. Names, ticks and avatars stay clear."),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});var defaults=new Button{Content=host.T("Restore default"),Padding=new Thickness(6)};panel.Children.Add(defaults);defaults.Click+=(_,_)=>slider.Value=35;}
  else{panel.Children.Add(new TextBlock{Text=host.T("Language"),Margin=new Thickness(0,0,0,8)});panel.Children.Add(language);}
  var save=new Button{Content=host.T("Save"),Padding=new Thickness(8),Margin=new Thickness(0,16,0,6)};panel.Children.Add(save);var cancel=new Button{Content=host.T("Cancel"),Padding=new Thickness(8),IsCancel=true};panel.Children.Add(cancel);
  save.Click+=(_,_)=>{if(opacityOnly){if(!ruler.SaveView("transparency")){MessageBox.Show(this,host.T("Could not save ruler settings. Changes remain in memory."),Title);return;}}else{if(language.SelectedIndex<0)return;string previous=host.S.Language,next=L10n.Languages[language.SelectedIndex];if(next!=previous){host.S.Language=next;host.Sync();if(!host.Save()){host.S.Language=previous;host.Sync();return;}}}applied=true;Close();};cancel.Click+=(_,_)=>Close();Closed+=(_,_)=>{if(opacityOnly&&!applied)ruler.SetBackgroundOpacity(old,false);};
 }
}
