using System;
namespace WorldTime;
// [-1,25] is visual cyclic padding, never a 26-hour absolute chronology.
public static class RulerAxis {
 public static double CoreWidth(double fullWidth)=>fullWidth*24/26;
 public static double Padding(double fullWidth)=>fullWidth/26;
 public static double Hour(double x,double fullWidth){if(!double.IsFinite(fullWidth)||fullWidth<=0)throw new ArgumentOutOfRangeException(nameof(fullWidth));return x/fullWidth*26-1;}
 public static int ReferenceMinute(double x,double fullWidth)=>(int)((Math.Round(Hour(x,fullWidth)*60)%1440+1440)%1440);
}
