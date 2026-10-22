using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
namespace WorldTime;
public class Bar : Window {
 public readonly WindowLayer Layer;public readonly Row R;readonly Host host;readonly Border border,badge;readonly TextBlock time,hint,contact,date,city,period,handle;readonly Button fold;
 DateTimeOffset displayTime;
 List<Row>? moving;Point origin;Dictionary<string,Point> positions=new();Layout.Candidate? candidate;bool started;
 public string PeriodText=>period.Text;
 public string HintText=>hint.Text;
 public string ContactText=>contact.Text;
 public bool HintFits=>hint.ActualWidth>0&&Fits(hint.Text,hint.ActualWidth)&&(contact.Visibility==Visibility.Collapsed||Fits(contact.Text,contact.ActualWidth,contact));
 public void SurfaceHover(bool entered)=>fold.Visibility=entered?Visibility.Visible:Visibility.Hidden;
 public void FoldFromSurface()=>host.CollapseGroup(R);
 public void SurfaceMenu(Point point){ContextMenu=Menu();ContextMenu.Placement=System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;ContextMenu.HorizontalOffset=point.X;ContextMenu.VerticalOffset=point.Y;ContextMenu.IsOpen=true;}
 public Bar(Host h,Row r){host=h;R=r;Title="WorldTime · "+r.City;Width=r.Width;Height=50;Left=r.X;Top=r.Y;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;AllowsTransparency=true;Background=Brushes.Transparent;ShowInTaskbar=false;ShowActivated=false;Topmost=false;Layer=new WindowLayer(this,h.S.Layer);
  TextOptions.SetTextRenderingMode(this,TextRenderingMode.Grayscale);
  var grid=new Grid{Margin=new Thickness(10,0,12,0)};foreach(double x in new[]{24d,28,82,78,44,88})grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(x)});grid.ColumnDefinitions.Add(new ColumnDefinition());grid.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(24)});
  TextBlock Label(string text,int col,double size,Brush brush){var t=new TextBlock{Text=text,FontSize=size,Foreground=brush,VerticalAlignment=VerticalAlignment.Center,TextTrimming=TextTrimming.CharacterEllipsis};Grid.SetColumn(t,col);grid.Children.Add(t);return t;}
  var headerBackground=new Border{Background=new SolidColorBrush(Color.FromArgb(20,210,230,240)),CornerRadius=new CornerRadius(5),Margin=new Thickness(-4,6,8,6)};Grid.SetColumnSpan(headerBackground,3);grid.Children.Add(headerBackground);
  handle=Label("⠿",0,21,new SolidColorBrush(Color.FromRgb(135,153,165)));Label(r.Code,1,11,Brushes.LightSteelBlue);city=Label("",2,14,Brushes.WhiteSmoke);time=Label("",3,23,Brushes.White);time.FontFamily=new FontFamily("Consolas");date=Label("",4,10,Brushes.LightSteelBlue);
  period=new TextBlock{FontSize=11,FontWeight=FontWeights.SemiBold,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center};
  badge=new Border{Child=period,CornerRadius=new CornerRadius(5),Height=24,Margin=new Thickness(0,0,8,0),VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(badge,5);grid.Children.Add(badge);
  var messages=new StackPanel{VerticalAlignment=VerticalAlignment.Center};Grid.SetColumn(messages,6);grid.Children.Add(messages);
  hint=new TextBlock{FontSize=13,Foreground=Brushes.WhiteSmoke};contact=new TextBlock{FontSize=10.5,Foreground=Brushes.LightSteelBlue,Margin=new Thickness(0,2,0,0)};messages.Children.Add(hint);messages.Children.Add(contact);
  hint.TextTrimming=TextTrimming.None;hint.SizeChanged+=(_,_)=>FitHint();contact.SizeChanged+=(_,_)=>FitHint();
  fold=new Button{Content="⇥",FontSize=17,Foreground=Brushes.LightSteelBlue,Background=Brushes.Transparent,BorderThickness=new Thickness(0),Padding=new Thickness(0),Visibility=Visibility.Hidden,Focusable=false};Grid.SetColumn(fold,7);grid.Children.Add(fold);fold.Click+=(_,_)=>host.CollapseGroup(R);
  MouseEnter+=(_,_)=>fold.Visibility=Visibility.Visible;MouseLeave+=(_,_)=>fold.Visibility=Visibility.Hidden;
  border=new Border{CornerRadius=new CornerRadius(10),BorderBrush=Brushes.SlateGray,BorderThickness=new Thickness(1),Child=grid};Content=border;
  border.MouseLeftButtonDown+=(o,e)=>{if(fold.IsMouseOver)return;var local=e.GetPosition(this);if(!BeginDrag(PointToScreen(local),local.X<=154))return;CaptureMouse();e.Handled=true;};
  MouseMove+=(o,e)=>{if(moving==null||e.LeftButton!=MouseButtonState.Pressed)return;ContinueDrag(PointToScreen(e.GetPosition(this)));};
  MouseLeftButtonUp+=(o,e)=>EndDrag();LostMouseCapture+=(o,e)=>{if(moving!=null)EndDrag();};ContextMenuOpening+=(_,_)=>ContextMenu=Menu();ContextMenu=Menu();Refresh();
 }
 void Preview(){foreach(var b in host.Bars.Values)b.border.BorderBrush=Brushes.SlateGray;if(candidate!=null){host.Bars[candidate.Target.Id].border.BorderBrush=Brushes.Aquamarine;border.BorderBrush=Brushes.Aquamarine;ToolTip=host.T("Release to snap; hold Alt to cancel");}}
 public bool BeginDrag(Point screen,bool header){
  var selection=Layout.DragSelection(host.S,R,header);if(selection.Count==0)return false;
  origin=screen;started=false;moving=selection;candidate=null;positions=moving.ToDictionary(x=>x.Id,x=>new Point(x.X,x.Y));return true;
 }
 public void ContinueDrag(Point screen){
  if(moving==null)return;var dpi=VisualTreeHelper.GetDpi(this);double dx=(screen.X-origin.X)/dpi.DpiScaleX,dy=(screen.Y-origin.Y)/dpi.DpiScaleY;
  if(!started&&Math.Abs(dx)+Math.Abs(dy)<4)return;
  if(!started){started=true;Layout.SplitForDrag(host.S,moving);}
  foreach(var row in moving){row.X=positions[row.Id].X+dx;row.Y=positions[row.Id].Y+dy;}
  candidate=Layout.Find(host.S,moving);host.Sync();Preview();
 }
 void EndDrag()=>CompleteDrag(Keyboard.Modifiers.HasFlag(ModifierKeys.Alt));
 public void CompleteDrag(bool cancelSnap=false){
  if(moving==null)return;var rows=moving;moving=null;ReleaseMouseCapture();
  if(started){bool docked=!cancelSnap&&host.TryAutoCollapse(R);if(!docked&&candidate!=null&&!cancelSnap)Layout.Merge(host.S,rows,candidate);host.Sync();host.Save();}
  candidate=null;foreach(var b in host.Bars.Values){b.border.BorderBrush=Brushes.SlateGray;b.last="";b.Refresh();}
 }
 ContextMenu Menu(){var menu=new ContextMenu();void Item(string key,Action a){var m=new MenuItem{Header=host.T(key)};m.Click+=(_,_)=>a();menu.Items.Add(m);}
  Item("Collapse group to edge",()=>host.CollapseGroup(R));
  var dock=new MenuItem{Header=host.T("Collapse group to edge")+"…"};foreach(string edge in DockGeometry.Edges){string chosen=edge;var item=new MenuItem{Header=host.T("Dock "+edge)};item.Click+=(_,_)=>host.CollapseGroup(R,chosen);dock.Items.Add(item);}menu.Items.Add(dock);
  Item("Manage cities",()=>host.Manager.Reveal());Item("Add city to group",()=>host.Add(R));Item("Add separate city",()=>host.Add(null));Item("Detach this clock",()=>{Layout.Detach(host.S,R);R.X+=40;host.Sync();host.Save();});Item("Split group here",()=>{var tail=Layout.DragSelection(host.S,R,true);Layout.SplitForDrag(host.S,tail);foreach(var r in tail)r.X+=40;host.Sync();host.Save();});Item("Move up",()=>host.Reorder(R,-1));Item("Move down",()=>host.Reorder(R,1));Item("Name and width",()=>host.Rename(R));Item("Settings",host.Settings);Item("Remove",()=>host.Remove(R));Item("Exit WorldTime",host.Quit);return menu;
 }
  string last="";
 public void Refresh(DateTimeOffset? utc=null){var s=host.S;var t=Catalog.Local(R.Zone,utc??DateTimeOffset.UtcNow);string text=Rules.Text(t,s);string key=t.ToString("yyyyMMddHHmm")+text+host.Language+s.Twelve+s.Opacity+s.Font+R.Name+R.Group+R.Order;if(key==last)return;last=key;
  Opacity=1;border.Background=new SolidColorBrush(Color.FromArgb((byte)Math.Round(255*s.Opacity),60,80,104));time.Text=t.ToString(s.Twelve?"hh:mm":"HH:mm",CultureInfo.InvariantCulture);date.Text=t.ToString("MM/dd")+(s.Twelve?"\n"+t.ToString("tt",CultureInfo.InvariantCulture):"");city.Text=L10n.CityName(R,s.Language);city.ToolTip=city.Text;displayTime=t;hint.ToolTip=text;FitHint();handle.Text=Layout.Group(s,R).First().Id==R.Id?"⠿":"⋮";handle.ToolTip=host.T(Layout.Group(s,R).First().Id==R.Id?"Drag the whole stack":"Drag this clock and those below");fold.ToolTip=host.T("Collapse group to edge");
  string part=DayPart.Key(t.Hour);period.Text=host.T(part);badge.ToolTip=host.T("Local clock period; not a sunrise or sunset forecast");
  bool light=DayPart.IsDay(t.Hour);badge.Background=new SolidColorBrush(light?Color.FromArgb((byte)(255*Math.Min(.65,s.Opacity+.18)),242,208,133):Color.FromArgb((byte)(255*Math.Min(.65,s.Opacity+.18)),85,106,160));period.Foreground=light?new SolidColorBrush(Color.FromRgb(45,38,24)):Brushes.White;
  ToolTip=$"{t:yyyy-MM-dd HH:mm zzz}\n{R.Zone}\n"+host.T("Top clock: move stack · Lower header: split from here · Edge: auto-fold")+"\n"+host.T("Typical schedules only; holidays and personal calendars are unknown.");
  Layer.Invalidate();
 }
 bool Fits(string value,double width,TextBlock? target=null){target??=hint;return new FormattedText(value,CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,new Typeface(target.FontFamily,target.FontStyle,target.FontWeight,target.FontStretch),target.FontSize,Brushes.White,VisualTreeHelper.GetDpi(this).PixelsPerDip).WidthIncludingTrailingWhitespace<=width+.5;}
 void FitHint(){
  hint.FontSize=host.S.Font;string value=Rules.Brief(displayTime,host.S);
  if(hint.ActualWidth>0&&!Fits(value,hint.ActualWidth))value=Rules.Short(displayTime,host.S);
  if(hint.ActualWidth>0&&!Fits(value,hint.ActualWidth))value=Rules.Micro(displayTime,host.S);
  while(hint.ActualWidth>0&&hint.FontSize>11&&!Fits(value,hint.ActualWidth))hint.FontSize--;
  hint.Text=value;
  contact.Visibility=host.S.Life&&host.S.Advice?Visibility.Visible:Visibility.Collapsed;
  string next=Rules.ContactLine(displayTime,host.S);
  if(contact.ActualWidth>0&&!Fits(next,contact.ActualWidth,contact))next=Rules.ContactLine(displayTime,host.S,2);
  contact.Text=next;contact.ToolTip=Rules.Text(displayTime,host.S);
 }
}
