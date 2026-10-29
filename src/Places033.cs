using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WorldTime;
// Loaded once on first city search (or stable-ID lookup), never on each keystroke.
public static class Places033 {
 sealed record Entry(City City,string[] Canonical,string[] Names,string[] Terms,string Text);
 sealed class Index {
  public readonly Entry[] Entries;public readonly Dictionary<string,City> ById;
  Index(Entry[] entries){Entries=entries;ById=entries.ToDictionary(e=>e.City.Id,e=>e.City,StringComparer.Ordinal);}
  public static async Task<Index> LoadAsync(){
   using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("WorldTime.places033.json.gz")??throw new InvalidDataException("Missing offline place data");using var zip=new GZipStream(stream,CompressionMode.Decompress);
   var mapping=new Dictionary<string,string>();var entries=new List<Entry>();var pool=new Dictionary<string,string>(StringComparer.Ordinal);
   string Shared(string s){if(pool.TryGetValue(s,out var cached))return cached;pool[s]=s;return s;}
   await foreach(var item in JsonSerializer.DeserializeAsyncEnumerable<City>(zip).ConfigureAwait(false)){
    if(item==null)throw new InvalidDataException("Invalid place record");var c=item;
    string key=c.Code+"|"+c.Iana;
    if(!mapping.TryGetValue(key,out string? zone)){
     zone=c.Zone;
     try{if(zone.Length>0)zone=TimeZoneInfo.FindSystemTimeZoneById(zone).Id;else if(TimeZoneInfo.TryConvertIanaIdToWindowsId(c.Iana,out string? id)&&id!=null)zone=TimeZoneInfo.FindSystemTimeZoneById(id).Id;}catch(TimeZoneNotFoundException){zone="";}catch(InvalidTimeZoneException){zone="";}
     mapping[key]=zone;
    }
    var canonical=new[]{c.En,c.Zh,c.Hant,c.Ja}.Select(Normalize).Distinct().Select(Shared).ToArray();
    var names=(c.Names?.Values.SelectMany(x=>x)??Array.Empty<string>()).Concat(new[]{c.En,c.Zh,c.Hant,c.Ja}).Select(Normalize).Where(n=>n.Length>0).Distinct().Select(Shared).ToArray();
    var terms=(c.Terms??Array.Empty<string>()).Select(Normalize).Where(n=>n.Length>0).Distinct().ToArray();
    string context=Normalize(string.Join(' ',c.Code,c.CountryEn,c.CountryZh,c.CountryHant,c.CountryJa,c.Admin,c.AdminZh,c.AdminHant,c.AdminJa,c.Code=="US"?"USA":c.Code=="GB"?"UK Britain":""));
    c=c with{Zone=zone,Names=null,Terms=null,CountryEn=Shared(c.CountryEn),CountryZh=Shared(c.CountryZh),CountryHant=Shared(c.CountryHant),CountryJa=Shared(c.CountryJa),Admin=Shared(c.Admin),AdminZh=Shared(c.AdminZh),AdminHant=Shared(c.AdminHant),AdminJa=Shared(c.AdminJa),Code=Shared(c.Code),Iana=Shared(c.Iana)};
    entries.Add(new Entry(c,canonical,names,terms,string.Join(' ',names.Concat(terms))+" "+context));
   }
   if(entries.Count==0)throw new InvalidDataException("Empty offline place data");return new Index(entries.ToArray());
  }
 }
 static readonly Lazy<Task<Index>> index=new(()=>Task.Run(Index.LoadAsync),LazyThreadSafetyMode.ExecutionAndPublication);
 public static async Task WarmAsync()=>await index.Value.ConfigureAwait(false);
 public static City? Find(string id)=>index.Value.IsCompletedSuccessfully?index.Value.Result.ById.GetValueOrDefault(id):null;
 public static async Task<CountryChoice[]> CountriesAsync(string language){var data=await index.Value.ConfigureAwait(false);return data.Entries.GroupBy(e=>e.City.Code).Select(g=>new CountryChoice(g.Key,Catalog.CountryName(g.First().City,language)+" ("+g.Key+")")).OrderBy(c=>c.Label,StringComparer.CurrentCultureIgnoreCase).ToArray();}
 public static City[] All=>index.Value.GetAwaiter().GetResult().Entries.Select(e=>e.City).ToArray();
 public static string Normalize(string value){
  var b=new StringBuilder();bool space=false;
  foreach(char c in value.Normalize(NormalizationForm.FormKD)){
   var cat=CharUnicodeInfo.GetUnicodeCategory(c);if(cat is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format)continue;
   if(char.IsWhiteSpace(c)||c is '_' or '-' or '’' or '\''){if(b.Length>0&&!space)b.Append(' ');space=true;}else{b.Append(char.ToUpperInvariant(c));space=false;}
  }
  return b.ToString().Trim().Normalize(NormalizationForm.FormC);
 }
 public static async Task<City[]> SearchAsync(string query,string country="",CancellationToken cancel=default){
  var data=await index.Value.WaitAsync(cancel).ConfigureAwait(false);return Search(data,query,country,cancel);
 }
 public static City[] Search(string query,string country="")=>Search(index.Value.GetAwaiter().GetResult(),query,country,CancellationToken.None);
 static City[] Search(Index data,string query,string country,CancellationToken cancel){
  string q=Normalize(query);var tokens=q.Split(' ',StringSplitOptions.RemoveEmptyEntries);var found=new List<(City City,int Rank)>();int i=0;
  foreach(var e in data.Entries){
   if((i++&127)==0)cancel.ThrowIfCancellationRequested();if(country.Length>0&&country!=e.City.Code)continue;
   if(!tokens.All(t=>e.Text.Contains(t,StringComparison.Ordinal)))continue;
   int rank=q.Length==0?7:e.Canonical.Any(n=>n==q)?0:e.Canonical.Any(n=>n.StartsWith(q,StringComparison.Ordinal))?1:e.Names.Any(n=>n==q)?2:e.Terms.Any(n=>n==q)?3:e.Names.Any(n=>n.StartsWith(q,StringComparison.Ordinal))?4:e.Terms.Any(n=>n.StartsWith(q,StringComparison.Ordinal))?4:e.Names.Any(n=>n.Contains(q,StringComparison.Ordinal))?5:6;
   found.Add((e.City,rank));
  }
  cancel.ThrowIfCancellationRequested();return found.OrderBy(x=>x.Rank).ThenByDescending(x=>x.City.Population).ThenBy(x=>x.City.Id,StringComparer.Ordinal).Select(x=>x.City).ToArray();
 }
 public static string AdminName(City c,string language)=>L10n.Resolve(language) switch{"zh-Hans"=>c.AdminZh,"zh-Hant"=>c.AdminHant,"ja"=>c.AdminJa,_=>c.Admin};
}
