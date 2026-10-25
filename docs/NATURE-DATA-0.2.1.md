# 0.2.1 nature information: provenance and limits

## Reference coordinates

`src/Data/zone.tab` is an unmodified copy of [IANA tzdb 2025b zone.tab](https://data.iana.org/time-zones/tzdb-2025b/zone.tab).

SHA-256: `586b4207e6c76722de82adcda6bf49d761f668517f45a673f64da83b333eecc4`.

The file's original public-domain notice is retained. This concerns the reference data only; it grants no new license to WorldTime source code. No new third-party code or package dependency was introduced.

Exact catalog city identity must resolve to an IANA key present in this table. Coordinates are reference points of named timezone locations, not the person's actual address or GPS. A shared Windows zone, country or system locale does not supply a coordinate. No lookup is made merely from the Windows zone ID. Catalog records without a matching key (including Beijing's existing empty IANA field) and the virtual self remain unknown. No alias coordinate is guessed.

Although zone.tab is a compatibility table, it is used deliberately for its individual named reference locations; a merged time-rule region must not silently substitute another city's coordinate. Dates, DST and offsets still use Windows rules, not this pinned table. IANA city coordinates are only approximate geographical inputs.

## Offline solar estimate

Independent C# implementation of equations in [NOAA General Solar Position Calculations](https://gml.noaa.gov/grad/solcalc/solareqns.PDF): fractional year, equation of time, solar declination and geometric altitude from the same UTC instant. No NOAA JavaScript implementation is copied. UTC-based calculation keeps solar position independent of civil offset and DST representation.

Labels are estimates of daylight, night, civil-twilight vicinity, polar day/night. The model does not calculate terrain, horizon obstructions, cloud cover, atmospheric conditions or observed sunrise/set times. It is not navigation-grade. The polar classification uses the model's instantaneous declination as a daily approximation. The common background gradient is a generic lifestyle cycle and never represents any one location's actual daylight length.

Validation cross-check uses a separate formulation from [USNO Computing Approximate Solar Coordinates](https://aa.usno.navy.mil/faq/sun_approx), combined with a sidereal hour angle, for 120 vectors across dates, UTC hours and latitudes/longitudes. Maximum altitude difference observed: **0.4489°**; test bound **1.2°**. This comparison is between approximate models, not field observations, and is not a universal error guarantee. USNO's stated precision for its own algorithm must not be attributed to WorldTime's NOAA approximation.

## Calendar seasons

At reference latitudes with absolute latitude at least 30°, the UI uses an explicitly named calendar-season convention: March–May spring, June–August summer, September–November autumn, December–February winter in the north, shifted by six months in the south. This is an ambience convention, not an inference of local climate, phenology, snow or sunshine. At lower latitudes it displays “Local seasons vary”; it does not infer wet/dry seasons. Elevation and regional climate are not known. Symbols are abstract seasonal accents, not actual weather icons or measurements.

The self has no known place, hence no geographic season inference. Nature decorations can be switched off for the current preview session; that switch does not alter saved contact data.

## Future work

Curated city coordinates/altitude with provenance, explicit self-place selection, regional climate descriptions, precise daylight-length presentation and real weather are separate future work. None are advertised as implemented.

Coverage check: 398 of 419 current catalog records have an exact IANA reference key in the pinned table; 21 records fall back to unknown. This is catalog reference coverage, not coverage of real people's locations. Country-code conflicts also fall back to unknown.
