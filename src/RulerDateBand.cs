using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldTime;
// A row connects people sharing a calendar date, never an interval of global time.
public sealed record RulerDateGroup(string Date,IReadOnlyList<RulerPerson> Members,double Start,double End);
public static class RulerDateBand {
 public static IReadOnlyList<RulerDateGroup> Group(IReadOnlyList<RulerPerson> people,double width){
  if(!double.IsFinite(width)||width<=0)throw new ArgumentOutOfRangeException(nameof(width));
  return people.GroupBy(p=>p.Date).OrderBy(g=>g.Key,StringComparer.Ordinal)
   .Select(g=>new RulerDateGroup(g.Key,g.ToArray(),g.Min(p=>p.Fraction)*width,g.Max(p=>p.Fraction)*width)).ToArray();
 }
}
