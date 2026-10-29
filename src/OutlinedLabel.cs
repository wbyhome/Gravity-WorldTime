using System.Globalization;
using System.Windows;
using System.Windows.Media;
namespace WorldTime;
// Glyph outlines, not a filled label rectangle. Works over light and dark content.
public sealed class OutlinedLabel : FrameworkElement {
 public string Text {get;init;}="";public double TextSize {get;init;}=11;public Brush Ink {get;init;}=Brushes.White;public FontFamily Family {get;init;}=new("Segoe UI");public bool Strong {get;init;}
 protected override Size MeasureOverride(Size available)=>new(double.IsFinite(Width)?Width:88,double.IsFinite(Height)?Height:17);
 protected override void OnRender(DrawingContext dc){if(ActualWidth<1||ActualHeight<1)return;var text=new FormattedText(Text,CultureInfo.CurrentUICulture,FlowDirection.LeftToRight,new Typeface(Family,FontStyles.Normal,Strong?FontWeights.SemiBold:FontWeights.Normal,FontStretches.Normal),TextSize,Ink,VisualTreeHelper.GetDpi(this).PixelsPerDip){MaxTextWidth=ActualWidth,MaxTextHeight=ActualHeight,Trimming=TextTrimming.CharacterEllipsis,TextAlignment=TextAlignment.Center};var geometry=text.BuildGeometry(new Point(0,0));var edge=new Pen(new SolidColorBrush(Color.FromArgb(220,19,30,45)),1.7){LineJoin=PenLineJoin.Round};dc.DrawGeometry(Ink,edge,geometry);dc.DrawGeometry(Ink,null,geometry);}
}
