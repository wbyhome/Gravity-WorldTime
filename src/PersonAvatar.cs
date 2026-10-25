using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
namespace WorldTime;
// Original vector portraits; appearance is based on a stable ID, never nationality.
public static class PersonAvatar {
 static readonly Dictionary<uint,DrawingImage> cache=new();
 public static int StyleFor(string id){uint h=2166136261;foreach(char c in id)h=(h^c)*16777619;return (int)(h%48);}
 public static DrawingImage For(string id)=>Preset(StyleFor(id));
 public static string SkinColorFor(int style)=>new[]{"#F1CCB0","#DFAF8D","#B98163","#EAC4AB"}[style%4];
 public static DrawingImage Preset(int index){if(index<0||index>=48)throw new ArgumentOutOfRangeException(nameof(index));uint style=(uint)index;if(cache.TryGetValue(style,out var saved))return saved;
  var group=new DrawingGroup{ClipGeometry=new RectangleGeometry(new Rect(0,0,48,48))};using(var d=group.Open()){
   Brush B(string c)=>new SolidColorBrush((Color)ColorConverter.ConvertFromString(c));
   var skin=B(new[]{"#F1CCB0","#DFAF8D","#B98163","#EAC4AB"}[style%4]);var hair=B(new[]{"#343042","#715046","#B27B46","#343C49"}[(style/4)%4]);var shirt=B(new[]{"#7E9CC6","#93B6A8","#C78F83"}[(style/16)%3]);
   d.DrawEllipse(B("#E6EDF4"),null,new Point(24,24),24,24);d.DrawEllipse(shirt,null,new Point(24,46),20,16);d.DrawRoundedRectangle(skin,null,new Rect(20,27,8,11),3,3);d.DrawEllipse(hair,null,new Point(24,18),15,15);d.DrawEllipse(skin,null,new Point(24,22),12,14);d.DrawEllipse(hair,null,new Point(style%2==0?19:27,12),12,7);if(style%3==0)d.DrawEllipse(hair,null,new Point(12,19),4,10);
   var eye=B("#393442");d.DrawEllipse(eye,null,new Point(19,23),1.25,1.6);d.DrawEllipse(eye,null,new Point(29,23),1.25,1.6);d.DrawEllipse(B("#D49083"),null,new Point(16,28),2.8,1.4);d.DrawEllipse(B("#D49083"),null,new Point(32,28),2.8,1.4);d.DrawLine(new Pen(B("#9E625E"),1.4),new Point(21,31),new Point(27,31));
   if(style%4==0){d.DrawEllipse(null,new Pen(eye,1),new Point(19,23),5,4);d.DrawEllipse(null,new Pen(eye,1),new Point(29,23),5,4);d.DrawLine(new Pen(eye,1),new Point(24,23),new Point(24.5,23));}
  }group.Freeze();var image=new DrawingImage(group);image.Freeze();cache[style]=image;return image;
 }
}
