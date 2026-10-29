"""Download official public datasets into an explicit review source directory."""
import concurrent.futures, hashlib, json, pathlib, sys, urllib.request
d = pathlib.Path(sys.argv[1]); d.mkdir(parents=True, exist_ok=True)
urls = {n: 'https://download.geonames.org/export/dump/'+n for n in ['cities15000.zip','cities5000.zip','alternateNamesV2.zip','admin1CodesASCII.txt','countryInfo.txt','readme.txt']}
def fetch(item):
    n,u=item; p=d/n
    if not p.exists():
        urllib.request.urlretrieve(u,p)
    return dict(name=n,url=u,bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest())
results=list(concurrent.futures.ThreadPoolExecutor(6).map(fetch,urls.items()))
(d/'downloads.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print(json.dumps(results))
