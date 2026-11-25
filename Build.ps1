param([string]$InnoCompiler,[string]$PortableDirectory)
$ErrorActionPreference='Stop'
$projectRoot=$PSScriptRoot
$output=Join-Path $projectRoot 'build'
$portable=if($PortableDirectory){if([IO.Path]::IsPathRooted($PortableDirectory)){[IO.Path]::GetFullPath($PortableDirectory)}else{[IO.Path]::GetFullPath((Join-Path $projectRoot $PortableDirectory))}}else{Join-Path $output 'portable'}
$release=Join-Path $output 'releases'
New-Item -ItemType Directory -Force $portable,$release | Out-Null
dotnet publish "$projectRoot/src/TextureFlow/TextureFlow.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $portable
if($LASTEXITCODE -ne 0){throw 'Publish failed'}
Copy-Item "$projectRoot/README.md" "$portable/README.md" -Force
if(Test-Path -LiteralPath "$projectRoot/docs"){
 New-Item -ItemType Directory -Force "$portable/docs" | Out-Null
 Copy-Item "$projectRoot/docs/*" "$portable/docs" -Recurse -Force
}
Get-ChildItem -LiteralPath $projectRoot -Filter 'T.A.R.D. Manual.*' -File | Where-Object Extension -In '.md','.html','.pdf' | Copy-Item -Destination $portable -Force
New-Item -ItemType Directory -Force "$portable/Licenses" | Out-Null
Copy-Item "$projectRoot/Licenses/*" "$portable/Licenses" -Recurse -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=Join-Path $release 'TextureFlow-0.3.0-Portable-win-x64.zip'
$temporaryArchive=$archive+'.tmp-'+[Guid]::NewGuid().ToString('N')
[IO.Compression.ZipFile]::CreateFromDirectory($portable,$temporaryArchive,[IO.Compression.CompressionLevel]::Optimal,$false)
Move-Item -LiteralPath $temporaryArchive -Destination $archive -Force
if($InnoCompiler){& $InnoCompiler "/DPortableDir=$portable" "$projectRoot/installer.iss";if($LASTEXITCODE -ne 0){throw 'Installer failed'}}
Get-ChildItem $release -File | Where-Object Extension -In '.zip','.exe' | Get-FileHash -Algorithm SHA256 | ForEach-Object { $_.Hash+'  '+(Split-Path $_.Path -Leaf) } | Set-Content -Encoding ASCII (Join-Path $release 'SHA256SUMS.txt')
Get-ChildItem $release -File | Select-Object Name,Length
