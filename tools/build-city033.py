"""Reproducible GeoNames cities5000/alternateNamesV2 + CLDR + Unihan subset.
No network at build/runtime; inputs must have been downloaded and licensed.
"""
import collections, gzip, hashlib, io, json, pathlib, sys, tarfile, unicodedata, xml.etree.ElementTree as ET, zipfile
source,cldr,out=map(pathlib.Path,sys.argv[1:4]);out.mkdir(parents=True,exist_ok=True)
def lines(name):
    z=zipfile.ZipFile(source/name)
    return io.TextIOWrapper(z.open(next(n for n in z.namelist() if n.startswith(name[:-4]))),encoding='utf-8')
admin={};countries={}
for l in (source/'admin1CodesASCII.txt').read_text(encoding='utf-8').splitlines():
    a=l.split('\t');admin[a[0]]=a
for l in (source/'countryInfo.txt').read_text(encoding='utf-8').splitlines():
    if l and not l.startswith('#'):
        a=l.split('\t');countries[a[0]]=a[4]
docs={lang:ET.parse(cldr/(lang+'.xml')) for lang in ['en','zh','zh_Hant','ja']}
zone_map={}
for m in ET.parse(cldr/'windowsZones.xml').findall('.//mapZone'):
    for zone in m.attrib['type'].split():zone_map[(m.attrib['territory'],zone)]=m.attrib['other']
links={}
with tarfile.open(source/'tzdata2026e.tar.gz') as tz:
    for l in tz.extractfile('backward').read().decode().splitlines():
        a=l.split()
        if len(a)>=3 and a[0]=='Link':links[a[2]]=a[1]
def windows(code,iana):
    candidates=[iana]+[old for old,canonical in links.items() if canonical==iana]
    return next((zone_map[(code,z)] for z in candidates if (code,z) in zone_map),'')
def country(lang,code):
    t=docs[lang].find("./localeDisplayNames/territories/territory[@type='%s']"%code)
    return t.text if t is not None and t.text!='↑↑↑' else countries.get(code,code)
places={};compare=[]
for file in ['cities15000.zip','cities5000.zip']:
    rows=[l.rstrip('\n').split('\t') for l in lines(file)]
    compare.append(dict(dataset=file,records=len(rows),zipBytes=(source/file).stat().st_size,rawBytes=sum(len(('\t'.join(a)+'\n').encode()) for a in rows)))
    if file=='cities5000.zip':places={a[0]:a for a in rows}
targets=set(places)|{a[3] for a in admin.values()}
names=collections.defaultdict(lambda:collections.defaultdict(list))
for l in lines('alternateNamesV2.zip'):
    a=l.rstrip('\n').split('\t')
    if len(a)<8 or a[1] not in targets or a[2] in ['link','post','iata','icao','faac','wkdt'] or not a[2]:continue
    # Keep historical names searchable; prefer contemporary official display names.
    lang=a[2];text=a[3]
    names[a[1]][lang].append((text,a[4]=='1',a[7]=='1'))
mandarin={}
z=zipfile.ZipFile(source/'Unihan-17.0.0.zip')
for l in z.read('Unihan_Readings.txt').decode().splitlines():
    if '\tkMandarin\t' in l:
        cp,_,value=l.split('\t');value=value.split()[0]
        mandarin[chr(int(cp[2:],16))]=''.join(c for c in unicodedata.normalize('NFD',value) if unicodedata.category(c)!='Mn').lower()
overrides=json.loads((pathlib.Path(__file__).parent/'city033-alias-overrides.json').read_text(encoding='utf-8'))
def display(gid,langs,fallback):
    for lang in langs:
        ns=names[gid].get(lang,[])
        live=[n for n in ns if not n[2]]
        if live:return sorted(live,key=lambda n:not n[1])[0][0]
    return fallback
results=[];coverage=collections.Counter();missing=[]
for gid,a in places.items():
    if not a[17]:missing.append(gid);continue
    en=display(gid,['en'],a[1]);zh=display(gid,['zh-CN','zh-Hans','zh'],en);hant=display(gid,['zh-TW','zh-Hant','zh-HK'],zh);ja=display(gid,['ja'],en)
    # Prefer an attested Chinese name whose Mandarin matches the canonical
    # international spelling over short poetic nicknames (e.g. 成都 vs 天府).
    if a[8]=='CN':
        match=[n[0] for lang in ['zh-CN','zh-Hans','zh'] for n in names[gid].get(lang,[]) if all(ch in mandarin for ch in n[0]) and ''.join(mandarin[ch] for ch in n[0]).lower()==en.replace(' ','').lower()]
        if match:zh=match[0]
    ns={lang:list(dict.fromkeys(v[0] for v in values)) for lang,values in names[gid].items()}
    for override in overrides:
        if override['id']==gid:ns[override['language']]=list(dict.fromkeys(ns.get(override['language'],[])+override['names']))
    for lang in ns:coverage[lang]+=1
    extra=[a[1],a[2]]
    # Unihan is character-based pronunciation, not a full place-name romanizer.
    # Chinese canonical international names remain separate higher-quality terms.
    if zh!=en and all(ch in mandarin for ch in zh):
        syll=[mandarin[ch] for ch in zh];extra+=[''.join(syll),''.join(x[0] for x in syll),' '.join(syll)]
    for lang,variants in ns.items():
        if not lang.startswith('zh'):continue
        for variant in variants:
            if variant and all(ch in mandarin for ch in variant):
                syll=[mandarin[ch] for ch in variant];extra+=[''.join(syll),''.join(x[0] for x in syll),' '.join(syll)]
    ad=admin.get(a[8]+'.'+a[10]);adname=ad[1] if ad else a[10]
    adnames={lang:display(ad[3],[lang],adname) if ad else adname for lang in ['zh','zh-Hant','ja']}
    results.append(dict(Id='geonames:'+gid,Zh=zh,En=en,Code=a[8],Zone=windows(a[8],a[17]),Hant=hant,Ja=ja,CountryEn=country('en',a[8]),CountryZh=country('zh',a[8]),CountryHant=country('zh_Hant',a[8]),CountryJa=country('ja',a[8]),Iana=a[17],Admin=adname,AdminCode=a[10],AdminZh=adnames['zh'],AdminHant=adnames['zh-Hant'],AdminJa=adnames['ja'],Latitude=float(a[4]),Longitude=float(a[5]),Population=int(a[14]),Feature=a[7],Names=ns,Terms=list(dict.fromkeys(extra))))
raw=json.dumps(results,ensure_ascii=False,separators=(',',':')).encode()
with open(out/'places033.json.gz','wb') as f:
    with gzip.GzipFile(fileobj=f,mode='wb',mtime=0) as g:g.write(raw)
manifest=dict(source='GeoNames cities5000 + alternateNamesV2',downloadDate='2026-10-26',dataVersion='2026-10-26 snapshot',license='CC-BY-4.0',modified=True,records=len(results),countries=len({r['Code'] for r in results}),languageCoverage=dict(coverage.most_common()),withoutTimeZone=missing,comparison=compare,rawBytes=len(raw),gzipBytes=(out/'places033.json.gz').stat().st_size,sha256=hashlib.sha256((out/'places033.json.gz').read_bytes()).hexdigest(),unihan=dict(version='17.0.0',sha256=hashlib.sha256((source/'Unihan-17.0.0.zip').read_bytes()).hexdigest(),url='https://www.unicode.org/Public/17.0.0/ucd/Unihan.zip',license='Unicode-3.0',purpose='Build-time character Mandarin to untoned pinyin and initials; not polyphone disambiguation'))
(out/'places033-provenance.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
manifest['curatedAliases']=overrides
manifest['cldrVersion']='release-48-2'
manifest['ianaLinksVersion']='2026e'
manifest['inputs']=[dict(name=p.name,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),bytes=p.stat().st_size) for p in [source/'cities5000.zip',source/'alternateNamesV2.zip',source/'admin1CodesASCII.txt',source/'countryInfo.txt',cldr/'windowsZones.xml',source/'tzdata2026e.tar.gz']]
(out/'places033-provenance.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({k:v for k,v in manifest.items() if k!='languageCoverage'},ensure_ascii=True))
