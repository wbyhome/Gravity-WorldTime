using System;
using System.Globalization;
namespace WorldTime;
// Civil clock language only, independent of routine estimates and solar models.
public static class ClockPeriod {
 public static string Key(int minute){if(minute<0||minute>=1440)throw new ArgumentOutOfRangeException(nameof(minute));return minute<300?"Clock: small hours":minute<480?"Clock: early morning":minute<720?"Clock: morning":minute<780?"Clock: noon":minute<1080?"Clock: afternoon":minute<1200?"Clock: evening":minute<1320?"Clock: night":"Clock: late night";}
 public static string Name(int minute,string language){string key=Key(minute);if(L10n.Resolve(language)!="en")return L10n.Text(language,key);string value=key.Substring(7);return char.ToUpperInvariant(value[0])+value.Substring(1);}
 public static string CardinalName(int hour,string language)=>L10n.Resolve(language)=="en"?(hour==0?"Night":hour==6?"Morning":hour==12?"Noon":"Evening"):Name(hour*60,language);
 public static string Caption(DateTimeOffset local,string language,bool twelve){int minute=local.Hour*60+local.Minute;string tag=L10n.Resolve(language);string time=local.ToString(twelve?"hh:mm tt":"HH:mm",CultureInfo.InvariantCulture);return tag=="en"?time+" · "+Name(minute,language):Name(minute,language)+" "+local.ToString(twelve?"h:mm":"HH:mm",CultureInfo.InvariantCulture);}
 public static string Tick(int hour,string language,bool twelve)=>Caption(new DateTimeOffset(2026,1,1,hour,0,0,TimeSpan.Zero),language,twelve);
}
