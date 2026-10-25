using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WorldTime;
// Explicit diagnostic only: an ordinary transparent observation window, NOT a ruler host.
// The ruler remains attached to Explorer through the unchanged desktop bridge.
public static class RulerDesktopProbe {
 public static void Start(Host host){
  if(!host.RulerPreviewOnly||!host.RulerDesktopRequested)throw new InvalidOperationException("Desktop check requires an isolated desktop preview.");
  var ruler=host.EnsureRuler();var area=DockScreen.AtPoint(ruler.Desktop.Position);double dpi=area.Scale;
  double width=Math.Min(1100,(area.Right-area.Left)/dpi-20),height=400;var canvas=new Canvas();
  var window=new Window{Title="WorldTime 0.2.6 · Explorer desktop check",WindowStyle=WindowStyle.None,AllowsTransparency=true,Background=Brushes.Transparent,ShowInTaskbar=true,Width=width,Height=height,ResizeMode=ResizeMode.NoResize,Content=canvas,WindowStartupLocation=WindowStartupLocation.Manual};
  var cover=new Rectangle{Width=width,Height=height-92,Fill=Brushes.Transparent};Canvas.SetTop(cover,42);canvas.Children.Add(cover);
  var top=new StackPanel{Orientation=Orientation.Horizontal,Background=new SolidColorBrush(Color.FromRgb(235,241,247)),Width=width,Height=40};canvas.Children.Add(top);
  var status=new TextBlock{Width=width,Height=50,Padding=new Thickness(10),FontSize=11,Background=new SolidColorBrush(Color.FromRgb(235,241,247)),Foreground=Brushes.DarkSlateGray};Canvas.SetTop(status,height-50);canvas.Children.Add(status);
  void Button(string label,Action action){var button=new Button{Content=label,Padding=new Thickness(8,4,8,4),Margin=new Thickness(5)};button.Click+=(_,_)=>action();top.Children.Add(button);}
  bool covered=false,dark=false;
  void Cover()=>cover.Fill=covered?new SolidColorBrush(dark?Color.FromRgb(28,41,60):Color.FromRgb(240,245,249)):Brushes.Transparent;
  Button("Cover / reveal desktop",()=>{covered=!covered;Cover();});Button("Pin / desktop",()=>ruler.SetPinned(!ruler.ActualPinned));Button("Light / dark cover",()=>{dark=!dark;Cover();});Button("Close check",window.Close);
  var initial=ruler.Desktop.Position;
  void PositionCheck(){var screen=DockScreen.AtPoint(ruler.Desktop.Position);var p=RulerDesktopBridge.Clamp(new Point(ruler.Desktop.Position.X-35*dpi,ruler.Desktop.Position.Y-65*dpi),width*dpi,height*dpi,screen);DockScreen.Place(window,new Rect(p,new Size(width*dpi,height*dpi)));}
  Button("Lower edge",()=>{status.Visibility=Visibility.Collapsed;var screen=DockScreen.AtPoint(ruler.Desktop.Position);var bounds=ruler.Desktop.Surface?.Bounds??new Rect(initial,new Size(720,180));ruler.Desktop.SetPosition(new Point(screen.Left+80*dpi,screen.Bottom-bounds.Height-6*dpi));ruler.SaveView("desktop-edge-check");PositionCheck();});
  Button("Restore position",()=>{status.Visibility=Visibility.Visible;ruler.Desktop.SetPosition(initial);ruler.SaveView("desktop-check-restore");PositionCheck();});
  var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(1)};timer.Tick+=(_,_)=>status.Text=$"Actual Explorer host; desktop: {ruler.Desktop.DesktopHosted}; pinned: {ruler.ActualPinned}; date hover: {ruler.HoveredDate}\nThis ordinary window covers the real desktop when filled. It does not render or parent the ruler.";window.Closed+=(_,_)=>timer.Stop();
  window.Show();PositionCheck();timer.Start();
 }
}
