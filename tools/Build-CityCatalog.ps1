param([string]$SourceDirectory=(Join-Path $PSScriptRoot '../artifacts/catalog-sources-cldr48-2'))
$ErrorActionPreference='Stop'
function Read-LocalXml([string]$file){
 $settings=[Xml.XmlReaderSettings]::new();$settings.DtdProcessing=[Xml.DtdProcessing]::Ignore;$settings.XmlResolver=$null
 $reader=[Xml.XmlReader]::Create($file,$settings)
 try{$doc=[Xml.XmlDocument]::new();$doc.Load($reader);return ,$doc}finally{$reader.Dispose()}
}
$docs=@{};foreach($lang in @('en','zh','zh_Hant','ja')){$docs[$lang]=Read-LocalXml (Join-Path $SourceDirectory ($lang+'.xml'))}
function Text-At([string]$lang,[string]$xpath,[string]$fallback){
 $node=$docs[$lang].SelectSingleNode($xpath)
 if($null -ne $node -and $node.InnerText -and $node.InnerText -ne '↑↑↑'){return $node.InnerText};return $fallback
}
function Country([string]$lang,[string]$code,[string]$fallback){Text-At $lang "/ldml/localeDisplayNames/territories/territory[@type='$code' and not(@alt)]" $fallback}
function City-Name([string]$lang,[string]$iana,[string]$fallback){Text-At $lang "/ldml/dates/timeZoneNames/zone[@type='$iana']/exemplarCity" $fallback}
$zones=Read-LocalXml (Join-Path $SourceDirectory 'windowsZones.xml')
$result=[Collections.Generic.List[object]]::new();$seen=[Collections.Generic.HashSet[string]]::new()
$aliases=@{US='USA United States of America 美国 美利坚';GB='UK Britain England 英国';CN='China PRC 中国 中國';KR='South Korea Korea 韩国 韓國';AE='UAE Emirates 阿联酋 阿聯酋';HK='Hong Kong 香港';MO='Macau Macao 澳门 澳門';TW='Taiwan 台湾 台灣';RU='Russia Russian Federation 俄国 俄國 俄罗斯 俄羅斯';VN='Vietnam Viet Nam 越南'}
$generated=@($zones.supplementalData.windowsZones.mapTimezones.mapZone | Where-Object {$_.territory -notin @('001','ZZ')} | ForEach-Object {
 $map=$_;foreach($iana in ($map.type.Trim() -split '\s+')){
  $en=City-Name 'en' $iana (($iana.Split('/')[-1]).Replace('_',' '))
  $zh=City-Name 'zh' $iana $en;$hant=City-Name 'zh_Hant' $iana $zh;$ja=City-Name 'ja' $iana $en
  # Asia/Urumqi names Xinjiang local time, not the official UTC+8 civil clock.
  if($iana -eq 'Asia/Urumqi'){$zh+='（新疆地方时间）';$hant+='（新疆地方時間）';$en+=' (Xinjiang local time)';$ja+='（新疆地方時間）'}
  $countryEn=Country 'en' $map.territory $map.territory
  [ordered]@{Zh=$zh;En=$en;Code=$map.territory;Zone=$map.other;Hant=$hant;Ja=$ja;CountryEn=$countryEn;CountryZh=(Country 'zh' $map.territory $countryEn);CountryHant=(Country 'zh_Hant' $map.territory $countryEn);CountryJa=(Country 'ja' $map.territory $countryEn);Iana=$iana;Aliases=$aliases[$map.territory]??''}
 }
})
# Preserve all original city identities and translations so existing rows load
# with their familiar names, including Beijing and New Delhi.
$legacy=Get-Content (Join-Path $PSScriptRoot 'legacy-cities.json') -Raw | ConvertFrom-Json
foreach($old in $legacy){
 $matching=$generated | Where-Object {$_.Code -eq $old.Code -and $_.Zone -eq $old.Zone -and $_.Zh -eq $old.Zh} | Select-Object -First 1
 $template=$matching;if($null -eq $template){$template=$generated | Where-Object {$_.Code -eq $old.Code -and $_.Zone -eq $old.Zone} | Select-Object -First 1}
 if($null -eq $template){throw "Missing source mapping for legacy city $($old.En)"}
 $row=[ordered]@{};foreach($key in $template.Keys){$row[$key]=$template[$key]}
 foreach($key in @('Zh','En','Code','Zone','Hant','Ja')){$row[$key]=$old.$key}
 if($null -eq $matching){$row.Iana=''}
 $result.Add($row);[void]$seen.Add($row.Code+'|'+$row.Zone+'|'+$row.Zh)
}
foreach($row in ($generated | Sort-Object { $_.Code },{ $_.En })){
 if($seen.Add($row.Code+'|'+$row.Zone+'|'+$row.Zh)){$result.Add($row)}
}
$destination=Join-Path $PSScriptRoot '../src/Data/cities.json'
[IO.File]::WriteAllText([IO.Path]::GetFullPath($destination),(ConvertTo-Json -InputObject $result.ToArray() -Depth 5))
$unsupported=@($result | Where-Object {try{[void][TimeZoneInfo]::FindSystemTimeZoneById($_.Zone);$false}catch{$true}})
$report=[ordered]@{Source='Unicode CLDR release-48-2';Locations=$result.Count;CountriesAndRegions=@($result.Code | Sort-Object -Unique).Count;UnsupportedWindowsZones=@($unsupported.Zone | Sort-Object -Unique);SourceHashes=@(Get-ChildItem $SourceDirectory -File | Get-FileHash -Algorithm SHA256 | Select-Object Path,Hash)}
$report | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $PSScriptRoot '../artifacts/catalog-provenance-0.1.11.json') -Encoding UTF8
[pscustomobject]$report | Select-Object Source,Locations,CountriesAndRegions,UnsupportedWindowsZones | ConvertTo-Json
