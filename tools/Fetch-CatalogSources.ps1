param([string]$Destination=(Join-Path $PSScriptRoot '../artifacts/catalog-sources-cldr48-2'))
$ErrorActionPreference='Stop'
New-Item -ItemType Directory -Path $Destination -Force | Out-Null
$files=@{
 'windowsZones.xml'='common/supplemental/windowsZones.xml'
 'en.xml'='common/main/en.xml'
 'zh.xml'='common/main/zh.xml'
 'zh_Hant.xml'='common/main/zh_Hant.xml'
 'ja.xml'='common/main/ja.xml'
 'LICENSE.txt'='LICENSE'
}
$expected=Get-Content (Join-Path $PSScriptRoot '../docs/catalog-provenance.json') -Raw | ConvertFrom-Json
foreach($file in $files.Keys){
 $path=Join-Path $Destination $file
 Invoke-WebRequest -Uri ('https://raw.githubusercontent.com/unicode-org/cldr/release-48-2/'+$files[$file]) -OutFile $path
 $hash=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
 $entry=$expected.SourceHashes | Where-Object File -eq $file
 if($null -eq $entry -or $entry.SHA256 -ne $hash){throw "Source hash mismatch: $file"}
}
Write-Output 'Pinned CLDR source files downloaded and SHA-256 verified.'
