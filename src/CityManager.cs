using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WorldTime;
public sealed class CityManager : Window {
 readonly Host host;
 StackPanel cities=null!;
 Button normal=null!,top=null!;
 TextBlock count=null!;
 string language="";
 public CityManager(Host host){
  this.host=host;Title="Gravity WorldTime";Width=540;Height=500;MinWidth=470;MinHeight=380;
  ShowInTaskbar=false;WindowStartupLocation=WindowStartupLocation.CenterScreen;
  Background=new SolidColorBrush(Color.FromRgb(243,246,249));FontFamily=new FontFamily("Segoe UI, Microsoft YaHei UI, Yu Gothic UI");FontSize=13;
  Build();
  Closing+=(_,e)=>{if(!host.Exiting){e.Cancel=true;Hide();}};
  StateChanged+=(_,_)=>{if(WindowState==WindowState.Minimized)Hide();};
 }
 void Build(){
  language=host.Language;var root=new DockPanel{Margin=new Thickness(22)};Content=root;
  var head=new StackPanel();DockPanel.SetDock(head,Dock.Top);root.Children.Add(head);
  head.Children.Add(new TextBlock{Text=host.T("City clocks"),FontSize=23,FontWeight=FontWeights.SemiBold});
  count=new TextBlock{Foreground=Brushes.SlateGray,Margin=new Thickness(0,8,0,16)};head.Children.Add(count);
  var tools=new WrapPanel{Margin=new Thickness(0,0,0,14)};head.Children.Add(tools);
  tools.Children.Add(Button("＋ "+host.T("Add city"),()=>host.Add(host.S.Rows.LastOrDefault())));
  tools.Children.Add(Button(host.T("Settings"),()=>host.Settings()));
  tools.Children.Add(Button(host.T("Recover clocks (pin on top)"),host.RecoverClocks));
  tools.Children.Add(Button(host.T("Collapse all to edge"),host.CollapseAll));
  tools.Children.Add(Button(host.T("Expand all clocks"),host.ExpandAll));
  var layers=new WrapPanel{Margin=new Thickness(0,0,0,14)};head.Children.Add(layers);
  normal=Button(host.T("On desktop"),()=>host.SetLayer("normal"));top=Button("↑ "+host.T("Pin on top"),()=>host.SetLayer("top"));
  normal.ToolTip=host.T("Stay on the desktop when using Win+D");top.ToolTip=host.T("Keep clocks above ordinary windows");layers.Children.Add(normal);layers.Children.Add(top);
  var foot=new StackPanel{Margin=new Thickness(0,14,0,0)};DockPanel.SetDock(foot,Dock.Bottom);root.Children.Add(foot);
  foot.Children.Add(new TextBlock{Text=host.T("Top clock: move stack · Lower header: split from here · Edge: auto-fold"),Foreground=Brushes.SlateGray,TextWrapping=TextWrapping.Wrap});
  var exit=Button(host.T("Exit WorldTime"),host.Quit);exit.HorizontalAlignment=HorizontalAlignment.Right;exit.Margin=new Thickness(0,12,0,0);foot.Children.Add(exit);
  cities=new StackPanel();root.Children.Add(new ScrollViewer{Content=cities,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
 }
 static Button Button(string text,Action action){var b=new Button{Content=text,Padding=new Thickness(10,6,10,6),Margin=new Thickness(0,0,8,0)};b.Click+=(_,_)=>action();return b;}
 public void Reveal(){RefreshCities();Show();WindowState=WindowState.Normal;Activate();}
 public void RefreshCities(){
  if(language!=host.Language)Build();
  foreach(var (button,mode) in new[]{(normal,"normal"),(top,"top")}){bool selected=host.S.Layer==mode||(mode=="normal"&&host.S.Layer=="bottom");button.Background=selected?new SolidColorBrush(Color.FromRgb(38,88,125)):Brushes.White;button.Foreground=selected?Brushes.White:Brushes.Black;}
  count.Text=host.S.Docked.Count>0?string.Format(host.T("{0} cities · {1} groups folded"),host.S.Rows.Count,host.S.Docked.Count):string.Format(host.T("{0} cities · tray icon only"),host.S.Rows.Count);cities.Children.Clear();
  foreach(var row in host.S.Rows){
   var line=new DockPanel{Margin=new Thickness(0,0,0,8)};
   var remove=Button(host.T("Remove"),()=>host.Remove(row));remove.Margin=new Thickness(8,0,0,0);DockPanel.SetDock(remove,Dock.Right);line.Children.Add(remove);
   var edit=Button(host.T("Edit"),()=>host.Rename(row));edit.Margin=new Thickness(8,0,0,0);DockPanel.SetDock(edit,Dock.Right);line.Children.Add(edit);
   line.Children.Add(new TextBlock{Text=$"{row.Code}  {L10n.CityName(row,host.S.Language)}",VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis,ToolTip=row.Zone});
   cities.Children.Add(new Border{Child=line,Padding=new Thickness(10,8,10,0),Background=Brushes.White,CornerRadius=new CornerRadius(6)});
  }
  if(host.S.Rows.Count==0)cities.Children.Add(new TextBlock{Text=host.T("No cities yet. Choose Add city."),Margin=new Thickness(10)});
 }
}
