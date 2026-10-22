using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace WorldTime;
public record City(string Zh,string En,string Code,string Zone,string Hant="",string Ja="",string CountryEn="",string CountryZh="",string CountryHant="",string CountryJa="",string Iana="",string Aliases="");
public record CountryChoice(string Code,string Label);
public static class Catalog {
 public static readonly City[] All=Load();
 static readonly HashSet<string> zones=TimeZoneInfo.GetSystemTimeZones().Select(z=>z.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
 static readonly Dictionary<City,string> search=All.ToDictionary(c=>c,c=>Normalize(string.Join(" ",c.Zh,c.En,c.Hant,c.Ja,c.Code,c.CountryEn,c.CountryZh,c.CountryHant,c.CountryJa,c.Iana,c.Zone,c.Aliases)));
 public static IEnumerable<City> Supported=>All.Where(c=>zones.Contains(c.Zone));
 static City[] Load(){
  using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("WorldTime.cities.json")??throw new InvalidDataException("Missing embedded city catalog.");
  return JsonSerializer.Deserialize<City[]>(stream)??throw new InvalidDataException("Empty city catalog.");
 }
 public static City? Find(Row row)=>Array.Find(All,c=>c.Zh==row.City&&c.Zone==row.Zone&&c.Code==row.Code)??Array.Find(All,c=>c.Zh==row.City&&c.Zone==row.Zone);
 public static string CityName(City city,string language)=>L10n.Resolve(language) switch{"zh-Hans"=>city.Zh,"zh-Hant"=>city.Hant.Length>0?city.Hant:city.Zh,"ja"=>city.Ja.Length>0?city.Ja:city.En,_=>city.En};
 public static string CountryName(City city,string language)=>L10n.Resolve(language) switch{"zh-Hans"=>city.CountryZh,"zh-Hant"=>city.CountryHant,"ja"=>city.CountryJa,_=>city.CountryEn};
 public static IEnumerable<CountryChoice> Countries(string language)=>Supported.GroupBy(c=>c.Code).Select(g=>new CountryChoice(g.Key,CountryName(g.First(),language)+" ("+g.Key+")")).OrderBy(c=>c.Label,StringComparer.CurrentCultureIgnoreCase);
 public static IEnumerable<City> Search(string query,string? country=null){
  string normalized=Normalize(query.Trim());var tokens=normalized.Split(' ',StringSplitOptions.RemoveEmptyEntries);
  string code=normalized=="UK"?"GB":normalized;
  bool exactCode=code.Length==2&&All.Any(c=>c.Code==code);
  var namedCodes=normalized.Length==0?new HashSet<string>():All.Where(c=>new[]{c.CountryEn,c.CountryZh,c.CountryHant,c.CountryJa}.Any(n=>Normalize(n)==normalized)).Select(c=>c.Code).ToHashSet();
  return Supported.Where(c=>(string.IsNullOrEmpty(country)||c.Code==country)&&(exactCode?c.Code==code:namedCodes.Count>0?namedCodes.Contains(c.Code):tokens.All(t=>search[c].Contains(t,StringComparison.Ordinal))));
 }
 static string Normalize(string value){
  var b=new StringBuilder();foreach(char c in value.Normalize(NormalizationForm.FormD)){
   if(CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)b.Append(char.ToUpperInvariant(c=='_'?' ':c));
  }
  return b.ToString().Normalize(NormalizationForm.FormC);
 }
 public static DateTimeOffset Local(string zone,DateTimeOffset utc)=>TimeZoneInfo.ConvertTime(utc,TimeZoneInfo.FindSystemTimeZoneById(zone));
}
