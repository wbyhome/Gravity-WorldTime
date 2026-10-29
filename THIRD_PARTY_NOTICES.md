# Third-party catalog data

Only the location catalog derives from Unicode CLDR release-48-2:
https://github.com/unicode-org/cldr/tree/release-48-2

Sources: common/supplemental/windowsZones.xml; common/main/en.xml,
zh.xml, zh_Hant.xml and ja.xml. Selected fields are combined and localized;
the original 18 application city names are retained. Current UTC offsets and
daylight-saving rules are calculated by the installed Windows time-zone database.

The following license covers the Unicode-derived catalog data. It does not
change the licensing of the application or of Gravity Desktop.
UNICODE LICENSE V3

COPYRIGHT AND PERMISSION NOTICE

Copyright © 2004-2026 Unicode, Inc.

NOTICE TO USER: Carefully read the following legal agreement. BY
DOWNLOADING, INSTALLING, COPYING OR OTHERWISE USING DATA FILES, AND/OR
SOFTWARE, YOU UNEQUIVOCALLY ACCEPT, AND AGREE TO BE BOUND BY, ALL OF THE
TERMS AND CONDITIONS OF THIS AGREEMENT. IF YOU DO NOT AGREE, DO NOT
DOWNLOAD, INSTALL, COPY, DISTRIBUTE OR USE THE DATA FILES OR SOFTWARE.

Permission is hereby granted, free of charge, to any person obtaining a
copy of data files and any associated documentation (the "Data Files") or
software and any associated documentation (the "Software") to deal in the
Data Files or Software without restriction, including without limitation
the rights to use, copy, modify, merge, publish, distribute, and/or sell
copies of the Data Files or Software, and to permit persons to whom the
Data Files or Software are furnished to do so, provided that either (a)
this copyright and permission notice appear with all copies of the Data
Files or Software, or (b) this copyright and permission notice appear in
associated Documentation.

THE DATA FILES AND SOFTWARE ARE PROVIDED "AS IS", WITHOUT WARRANTY OF ANY
KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF
THIRD PARTY RIGHTS.

IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN THIS NOTICE
BE LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES,
OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS,
WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION,
ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THE DATA
FILES OR SOFTWARE.

Except as contained in this notice, the name of a copyright holder shall
not be used in advertising or otherwise to promote the sale, use or other
dealings in these Data Files or Software without prior written
authorization of the copyright holder.

SPDX-License-Identifier: Unicode-3.0

## Reference coordinates added by the ruler preview

`src/Data/zone.tab` is the unmodified IANA tzdb 2025b compatibility table. Its original public-domain notice is retained in the file. See [provenance and model limitations](docs/NATURE-DATA-0.2.1.md). This data notice grants no license to WorldTime application code.

## GeoNames offline places (WorldTime 0.3.3)

Place data © GeoNames, https://www.geonames.org/ . Downloaded 2026-10-26
from https://download.geonames.org/export/dump/ : cities5000.zip,
alternateNamesV2.zip, admin1CodesASCII.txt, countryInfo.txt.
Licensed under Creative Commons Attribution 4.0 International (CC BY 4.0):
https://creativecommons.org/licenses/by/4.0/ . Full text: licenses/CC-BY-4.0.txt.
Commercial use and redistribution are permitted subject to attribution,
license notice and indication of changes. Data is provided without warranty.

Modified: selected populated places and administrative names, excluded links,
postal/airport codes, generated a compressed offline subset, normalized search
terms, and combined display countries and Windows mappings with Unicode CLDR
release-48-2. Stable GeoNames IDs, coordinates, country, admin codes, IANA time
zones, population and multilingual aliases are retained. The derived data is
src/Data/places033.json.gz; its inputs, hashes and coverage are recorded in
src/Data/places033-provenance.json. tools/build-city033.py regenerates it from
fixed local inputs. No user query/contact telemetry or runtime data download.

Unihan 17.0.0 kMandarin (Unicode License V3, licenses/Unicode-3.0.txt) supplies
build-time character pronunciation for untoned pinyin and initials; this is not
complete phrase/polyphone romanization. IANA tzdb 2026e backward links are
public-domain reference data used at build time to resolve renamed zone IDs.
The actual runtime conversion rules remain those installed on Windows; CLDR
mappings do not establish identical historical rules for all mapped locations.

A small independently checked name-fact supplement is documented in
tools/city033-alias-overrides.json with original reference URLs. Only place names
and readings are included, not the prose or illustrations of those sources.

These data notices do not grant or change the application's software license.
