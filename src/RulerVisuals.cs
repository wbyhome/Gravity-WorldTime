using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace WorldTime;
public sealed record RulerPersonDate(RulerCluster Cluster,Rect Bounds){public RulerPerson Person=>Cluster.Members[0];public string Date=>Person.Date;}
public sealed partial class RulerWindow {
 sealed record PersonInk(RulerCluster Cluster,Button Avatar,Ellipse Halo,Border Card);
 readonly List<PersonInk> personInk=new();readonly Dictionary<string,Border> dateInk=new();readonly List<Ellipse> gripDots=new();Line? gripDivider;TextBlock? resizeGlyph;
 public bool HandleHovered {get;private set;}public bool ResizeHovered {get;private set;}
 public int HandleHighlightChangeCount {get;private set;}public int HighlightChangeCount {get;private set;}
 public IReadOnlyList<RulerPersonDate> PersonDates {get;private set;}=Array.Empty<RulerPersonDate>();
 // Historical test source compatibility; no date rails are drawn or hit-tested.
 public IReadOnlyList<RulerDateRail> DateRails=>Array.Empty<RulerDateRail>();
 Point? lastPointer;public string HoveredDate {get;private set;}="";public string HoveredPersonId {get;private set;}="";
 public IReadOnlyList<string> DateHighlightedIds=>PersonDates.FirstOrDefault(d=>d.Person.Id==HoveredPersonId)?.Cluster.Members.Where(p=>p.Date==HoveredDate).Select(p=>p.Id).ToArray()??Array.Empty<string>();
 double AvatarTop=>Compact?18:34;double AvatarSize=>Compact?26:30;double LabelWidth=>Math.Min(Compact?76:82,trackWidth/2);
 static readonly DropShadowEffect inkShadow=CreateShadow();static DropShadowEffect CreateShadow(){var e=new DropShadowEffect{Color=Color.FromRgb(17,29,45),Opacity=.95,ShadowDepth=0,BlurRadius=2};e.Freeze();return e;}
 TextBlock Ink(string value,double size,string color){var t=Text(value,size,color);t.Effect=inkShadow;return t;}
 SolidColorBrush PanelBrush()=>new(Color.FromArgb((byte)Math.Round(view.BackgroundOpacity*255),29,48,72));
 void SetHandleHover(bool v){if(v==HandleHovered)return;HandleHovered=v;HandleHighlightChangeCount++;HideNativeTip();UpdateGripInk();Desktop.Refresh();}
 void SetResizeHover(bool v){if(v==ResizeHovered)return;ResizeHovered=v;HideNativeTip();UpdateGripInk();Desktop.Refresh();}
 void UpdateGripInk(){foreach(var d in gripDots)d.Fill=Brush(HandleHovered?"#ECEDF4FA":"#778AA0B7");if(gripDivider!=null)gripDivider.Stroke=Brush(HandleHovered?"#8DC5D6E8":"#3F91A8C0");if(resizeGlyph!=null)resizeGlyph.Foreground=Brush(ResizeHovered?"#F5EFF6FF":"#777E98AE");InvalidateMotionRaster();}
 RulerPersonDate? DateAt(Point local)=>view.Dates?PersonDates.FirstOrDefault(d=>d.Bounds.Contains(new Point(local.X,local.Y-DateTop))):null;
 string DateCaption(RulerPerson p,bool full=false){bool en=L10n.Resolve(host.Language)=="en";return p.Local.ToString(full?(en?"dddd, MMMM d, yyyy":"yyyy年M月d日"):trackWidth<350?"MM/dd":en?"MMM d":"M月d日",en?CultureInfo.GetCultureInfo("en-US"):CultureInfo.InvariantCulture);}
 string DateTip(RulerPersonDate d)=>d.Person.Name+" · "+DateCaption(d.Person,true)+"\n"+string.Join("\n",d.Cluster.Members.Select(p=>p.Name+" · "+p.Local.ToString("yyyy-MM-dd HH:mm zzz",CultureInfo.InvariantCulture)))+(d.Cluster.Members.Count>1?"\n"+host.T("The date belongs to the representative. Open the group for each person's date."):"");
 public void ClearDateHover()=>SetDateHover(null);
 void SetDateHover(RulerPersonDate? v){string id=v?.Person.Id??"",date=v?.Date??"";if(id==HoveredPersonId&&date==HoveredDate)return;HoveredPersonId=id;HoveredDate=date;HighlightChangeCount++;HideNativeTip();ApplyDateHighlight();Desktop.Refresh();}
 void ApplyDateHighlight(){foreach(var p in dateInk)p.Value.BorderBrush=Brush(p.Key==HoveredPersonId?"#FFE1A0":"#403B5974");foreach(var p in personInk)p.Halo.Stroke=p.Cluster.Members[0].Id==HoveredPersonId?Brush("#FFE0A0"):Brushes.Transparent;InvalidateMotionRaster();}
 void DrawFrame(){
  people.Children.Clear();scale.Children.Clear();dates.Children.Clear();PersonButtons.Clear();dateInk.Clear();personInk.Clear();gripDots.Clear();gripDivider=null;resizeGlyph=null;
  people.Width=scale.Width=dates.Width=SkyWidth+64;people.Height=AvatarTop+AvatarSize+4+AvatarGap;scale.Height=44;dates.Height=view.Dates?24:0;
  PersonDates=view.Dates?Clusters.Select(c=>new RulerPersonDate(c,new Rect(Origin+c.X-Math.Min(78,LabelWidth)/2,4,Math.Min(78,LabelWidth),18))).ToArray():Array.Empty<RulerPersonDate>();
  HandleHovered=lastPointer.HasValue&&NativeCanDrag(lastPointer.Value);ResizeHovered=lastPointer.HasValue&&NativeCanResize(lastPointer.Value);var next=lastPointer.HasValue?DateAt(lastPointer.Value):null;string id=next?.Person.Id??"",date=next?.Date??"";if(id!=HoveredPersonId||date!=HoveredDate){HighlightChangeCount++;HideNativeTip();}HoveredPersonId=id;HoveredDate=date;string tick=lastPointer.HasValue?TickTip(lastPointer.Value):"";if(tick!=hoverTick){hoverTick=tick;HideNativeTip();}
  scale.Background=Brushes.Transparent;scale.Clip=new RectangleGeometry(new Rect(BodyBounds.X,0,BodyBounds.Width,BodyBounds.Height),20,20);DrawSky();DrawHandle();DrawTicks();foreach(var c in Clusters)DrawPerson(c);foreach(var d in PersonDates)DrawDate(d);ApplyDateHighlight();
 }
 public static int TickLevel(int h)=>h is 0 or 12 or 24?2:h is 6 or 18?1:0;
 public static string TickLabel(int h,bool twelve)=>twelve?((h%12==0?12:h%12)+" "+(h%24<12?"AM":"PM")):h.ToString("00",CultureInfo.InvariantCulture);
 string TickTip(Point p){if(p.Y<ScaleTop||p.Y>ScaleTop+40||p.X<CoreOrigin-4||p.X>CoreOrigin+TrackWidth+4)return "";int h=(int)Math.Round((p.X-CoreOrigin)/TrackWidth*24);return h is 0 or 24?host.T("Midnight · 00:00 / 12 AM"):h==12?host.T("Noon · 12:00 / 12 PM"):"";}
 void DrawTicks(){for(int h=-1;h<=25;h++){double x=Origin+h/24d*trackWidth;int level=TickLevel(h);double top=level==2?12:level==1?16:20;string color=h==12?"#FFE5A8":level==2?"#E6EEFF":level==1?"#D9E9F2":"#A5BAC9";scale.Children.Add(new Line{X1=x,X2=x,Y1=top,Y2=26,Stroke=Brush("#D31D3048"),StrokeThickness=level==2?4:3,IsHitTestVisible=false});scale.Children.Add(new Line{X1=x,X2=x,Y1=top,Y2=26,Stroke=Brush(color),StrokeThickness=level==2?2:level==1?1.4:.8,IsHitTestVisible=false});if(h<0||h>24||h%3!=0||trackWidth<350&&h%6!=0||host.S.Twelve&&trackWidth<350&&level<2)continue;
   double width=host.S.Twelve?44:23;var t=Ink(TickLabel(h,host.S.Twelve),level==2?10:9,color);t.FontWeight=level>0?FontWeights.SemiBold:FontWeights.Normal;t.TextWrapping=TextWrapping.NoWrap;t.TextAlignment=TextAlignment.Center;var plate=new Border{Width=width,Child=t,CornerRadius=new CornerRadius(4),Background=PanelBrush(),IsHitTestVisible=false};Canvas.SetLeft(plate,x-width/2-(h==24?host.S.Twelve?12:4:0));Canvas.SetTop(plate,27);scale.Children.Add(plate);
  }}
 void DrawPerson(RulerCluster c){var primary=c.Members[0];double x=Origin+c.X;string accent=primary.Self?"#E3BE7C":"#B4CFDA";
  foreach(var p in c.Members){double anchor=Origin+p.Fraction*trackWidth;people.Children.Add(new Line{X1=anchor,X2=anchor,Y1=people.Height-AvatarGap-2,Y2=people.Height,Stroke=Brush(p.Self?"#E3BE7C":"#BDD3E3"),StrokeThickness=1.2,IsHitTestVisible=false});var pin=new Ellipse{Width=p.Self?4:2.5,Height=p.Self?4:2.5,Fill=Brush(p.Self?"#E3BE7C":"#BDD3E3"),IsHitTestVisible=false};Canvas.SetLeft(pin,anchor-pin.Width/2);scale.Children.Add(pin);}
  var halo=new Ellipse{Width=AvatarSize+12,Height=AvatarSize+12,Stroke=Brushes.Transparent,StrokeThickness=2,IsHitTestVisible=false};Canvas.SetLeft(halo,x-halo.Width/2);Canvas.SetTop(halo,AvatarTop-4);people.Children.Add(halo);
  var portrait=new Grid{Width=AvatarSize,Height=AvatarSize};portrait.Children.Add(new Image{Source=Avatars.For(AvatarId(primary)),Stretch=Stretch.UniformToFill,Width=AvatarSize,Height=AvatarSize,Clip=new EllipseGeometry(new Point(AvatarSize/2,AvatarSize/2),AvatarSize/2,AvatarSize/2)});
  var avatar=new Button{Width=AvatarSize+4,Height=AvatarSize+4,Content=portrait,Template=AvatarFrame(),Padding=new Thickness(1),Background=Brush("#F9FAFC"),BorderBrush=Brush(accent),BorderThickness=new Thickness(1.5),ToolTip=ClusterTip(c),Tag=c};AutomationProperties.SetName(avatar,primary.Name+(c.Members.Count>1?" +"+(c.Members.Count-1):"")+" "+primary.Time(host.S.Twelve));avatar.Click+=(_,_)=>{flyoutPoint=IsVisible?avatar.PointToScreen(new Point(0,avatar.ActualHeight)):Desktop.Position;OpenCluster(c,avatar);};Canvas.SetLeft(avatar,x-(AvatarSize+4)/2);Canvas.SetTop(avatar,AvatarTop);people.Children.Add(avatar);foreach(var p in c.Members)PersonButtons[p.Id]=avatar;
  var copy=new StackPanel();if(!Compact){var name=Ink(primary.Name+(c.Members.Count>1?" +"+(c.Members.Count-1):""),10,primary.Self?"#F2D49C":"#F0F5FB");name.FontWeight=FontWeights.SemiBold;name.TextAlignment=TextAlignment.Center;name.TextWrapping=TextWrapping.NoWrap;name.TextTrimming=TextTrimming.CharacterEllipsis;copy.Children.Add(name);}var hour=Ink(primary.Time(host.S.Twelve),Compact?10:12,"#F1F6FC");hour.FontWeight=FontWeights.SemiBold;hour.TextAlignment=TextAlignment.Center;hour.TextWrapping=TextWrapping.NoWrap;copy.Children.Add(hour);
  var card=new Border{Width=LabelWidth,CornerRadius=new CornerRadius(7),Background=PanelBrush(),BorderBrush=Brush("#403B5974"),BorderThickness=new Thickness(.6),Padding=new Thickness(3,1,3,2),Child=copy,ToolTip=ClusterTip(c)};Canvas.SetLeft(card,Origin+c.CardLeft);people.Children.Add(card);personInk.Add(new(c,avatar,halo,card));
  if(Compact&&(primary.Self||c.Members.Count>1)){var badge=Ink((primary.Self?primary.Name:"")+(c.Members.Count>1?"+"+(c.Members.Count-1):""),9,primary.Self?"#F2D49C":"#ECF3FC");badge.MaxWidth=54;badge.TextTrimming=TextTrimming.CharacterEllipsis;badge.TextWrapping=TextWrapping.NoWrap;var chip=new Border{Child=badge,CornerRadius=new CornerRadius(4),Background=PanelBrush(),Padding=new Thickness(3,0,3,0),IsHitTestVisible=false};Canvas.SetLeft(chip,x+AvatarSize/2);Canvas.SetTop(chip,AvatarTop+4);people.Children.Add(chip);}
 }
 void DrawDate(RulerPersonDate d){var t=Ink(DateCaption(d.Person),10,d.Person.Self?"#F2D49C":"#DAE8F4");t.FontWeight=FontWeights.SemiBold;t.TextAlignment=TextAlignment.Center;t.VerticalAlignment=VerticalAlignment.Center;t.TextWrapping=TextWrapping.NoWrap;var plate=new Border{Width=d.Bounds.Width,Height=d.Bounds.Height,Child=t,CornerRadius=new CornerRadius(7),Background=PanelBrush(),BorderBrush=Brush("#403B5974"),BorderThickness=new Thickness(.5),ToolTip=DateTip(d)};Canvas.SetLeft(plate,d.Bounds.X);Canvas.SetTop(plate,d.Bounds.Y);dates.Children.Add(plate);dateInk[d.Person.Id]=plate;}
 void DrawHandle(){gripDivider=new Line{X1=42,X2=42,Y1=7,Y2=33,StrokeThickness=.7,IsHitTestVisible=false};scale.Children.Add(gripDivider);for(int y=8;y<=33;y+=5){var dot=new Ellipse{Width=2.6,Height=2.6,IsHitTestVisible=false};Canvas.SetLeft(dot,24);Canvas.SetTop(dot,y-1.3);scale.Children.Add(dot);gripDots.Add(dot);}resizeGlyph=Ink("↔",11,"#777E98AE");resizeGlyph.IsHitTestVisible=false;Canvas.SetLeft(resizeGlyph,BodyBounds.Right-15);Canvas.SetTop(resizeGlyph,10);scale.Children.Add(resizeGlyph);UpdateGripInk();}
}
