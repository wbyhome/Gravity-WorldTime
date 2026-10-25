using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace WorldTime;
public sealed class RulerAvatarDialog : Window {
 public RulerAvatarDialog(Host host,RulerAvatarStore store,string id,Action changed){
  Title="WorldTime · "+host.T("Choose avatar");Width=390;SizeToContent=SizeToContent.Height;ResizeMode=ResizeMode.NoResize;ShowInTaskbar=false;WindowStartupLocation=WindowStartupLocation.CenterScreen;FontFamily=host.Manager.FontFamily;Background=new SolidColorBrush(Color.FromRgb(242,244,247));FontSize=13;
  RulerAvatarChoice? selected=store.Choice(id);var panel=new StackPanel{Margin=new Thickness(18)};Content=panel;var preview=new Image{Width=72,Height=72,Source=store.Image(id,selected),Margin=new Thickness(0,0,0,12)};panel.Children.Add(preview);var grid=new WrapPanel();panel.Children.Add(grid);
  foreach(int preset in new[]{0,5,10,15,19,23,27,31,35,39,43,47}){int captured=preset;var b=new Button{Content=new Image{Source=PersonAvatar.Preset(preset),Width=46,Height=46},Padding=new Thickness(3),Margin=new Thickness(2),ToolTip=host.T("Preset avatar")+" "+(preset+1)};b.Click+=(_,_)=>{selected=new(captured);preview.Source=store.Image(id,selected);};grid.Children.Add(b);}
  void Button(string text,Action action){var b=new Button{Content=host.T(text),Margin=new Thickness(0,8,0,0),Padding=new Thickness(7)};b.Click+=(_,_)=>action();panel.Children.Add(b);}
  Button("Default avatar",()=>{selected=null;preview.Source=store.Image(id,null);});Button("Import local image",()=>{var picker=new OpenFileDialog{Filter="PNG / JPEG|*.png;*.jpg;*.jpeg",CheckFileExists=true,Multiselect=false};if(picker.ShowDialog(this)!=true)return;try{selected=store.Import(picker.FileName);preview.Source=store.Image(id,selected);}catch{MessageBox.Show(this,host.T("Use a valid PNG/JPEG up to 12 MB and 16 megapixels. Original files are read only."),Title);}});
  panel.Children.Add(new TextBlock{Text=host.T("Images are cropped locally and copied into preview data. Nothing is uploaded."),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,10,0,2)});
  Button("Save",()=>{try{store.Set(id,selected);changed();Close();}catch{MessageBox.Show(this,host.T("Could not save avatar. Existing settings were preserved."),Title);}});Button("Cancel",Close);
 }
}
