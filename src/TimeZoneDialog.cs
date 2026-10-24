using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WorldTime;
public sealed class TimeZoneDialog : Window {
 readonly Host host;readonly ISystemTimeZone system;readonly string target;readonly bool restore;
 string current="";bool busy;
 public readonly TextBlock Summary=new(){TextWrapping=TextWrapping.Wrap};
 public readonly TextBlock Status=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,14,0,0)};
 public readonly Button ApplyButton=new(){Height=34,IsEnabled=false};
 readonly Button admin=new(){Height=32,Visibility=Visibility.Collapsed};
 public TimeZoneDialog(Host host,string target,bool restore=false,ISystemTimeZone? system=null){
  this.host=host;this.target=WindowsTimeZone.Validate(target);this.restore=restore;this.system=system??new WindowsTimeZone();
  host.Manager.Reveal();Owner=host.Manager;Title=host.T(restore?"Restore original computer time zone":"Set computer time zone");
  Width=560;SizeToContent=SizeToContent.Height;ShowInTaskbar=false;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterOwner;FontFamily=host.Manager.FontFamily;FontSize=14;
  Background=new SolidColorBrush(Color.FromRgb(243,246,249));var root=new StackPanel{Margin=new Thickness(22)};Content=root;
  root.Children.Add(Summary);root.Children.Add(new TextBlock{Text=host.T("This changes the Windows time zone for all apps. UTC stays unchanged. Automatic time-zone settings may override it."),TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,16,0,0)});
  root.Children.Add(Status);ApplyButton.Content=host.T("Apply to Windows");ApplyButton.Margin=new Thickness(0,16,0,6);root.Children.Add(ApplyButton);
  admin.Content=host.T("Retry as administrator");root.Children.Add(admin);
  var settings=new Button{Content=host.T("Open Windows date and time settings"),Height=30,Margin=new Thickness(0,8,0,0)};root.Children.Add(settings);
  settings.Click+=(_,_)=>{try{Process.Start(new ProcessStartInfo("ms-settings:dateandtime"){UseShellExecute=true});}catch(Exception e){Status.Text=e.Message;}};
  var cancel=new Button{Content=host.T("Cancel"),Height=30,Margin=new Thickness(0,8,0,0)};root.Children.Add(cancel);cancel.Click+=(_,_)=>Close();
  ApplyButton.Click+=async(_,_)=>await ApplyChanges();admin.Click+=async(_,_)=>await ApplyChanges(true);
  Loaded+=async(_,_)=>await LoadPreview();Closing+=(_,e)=>{if(busy)e.Cancel=true;};
 }
 public async Task LoadPreview(){
  try{current=await system.Read();var now=DateTimeOffset.UtcNow;string plain=target.EndsWith("_dstoff",StringComparison.Ordinal)?target[..^7]:target;
   var zone=TimeZoneInfo.FindSystemTimeZoneById(plain);var local=target.EndsWith("_dstoff",StringComparison.Ordinal)?now.ToOffset(zone.BaseUtcOffset):TimeZoneInfo.ConvertTime(now,zone);
   Summary.Text=host.T("Current time zone")+":\n"+current+"\n\n"+host.T("Target time zone")+":\n"+target+"\n"+local.ToString("yyyy-MM-dd HH:mm zzz");
   ApplyButton.IsEnabled=current!=target||restore;Status.Text=current==target?host.T("Windows already uses this time zone."):"";
  }catch(Exception e){ApplyButton.IsEnabled=false;Status.Text=e.Message;}
 }
 public async Task<bool> ApplyChanges(bool administrator=false){
  if(busy||current.Length==0)return false;busy=true;ApplyButton.IsEnabled=false;admin.IsEnabled=false;
  try{
   await new TimeZoneChange(host.S,host.Save,system).Apply(current,target,restore,administrator);
   TimeZoneInfo.ClearCachedData();Rules.ClearCache();host.RefreshEnvironment();
   Status.Foreground=Brushes.DarkGreen;Status.Text=host.T("Windows time zone updated.");admin.Visibility=Visibility.Collapsed;current=target;return true;
  }catch(Exception e){
   await LoadPreview();
   Status.Foreground=Brushes.Firebrick;Status.Text=e is Win32Exception w&&w.NativeErrorCode==1223?host.T("Administrator approval was cancelled. Check the current Windows time zone."):host.T("Could not complete the time zone change.")+"\n"+e.Message;
   admin.Visibility=Visibility.Visible;return false;
  }finally{busy=false;admin.IsEnabled=true;}
 }
}
