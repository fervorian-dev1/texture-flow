param([string]$UnityExe='C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$testRoot=Join-Path (Split-Path $PSScriptRoot -Parent) 'build/unity-apply-test'
if(-not(Test-Path "$testRoot/Assets/TextureFlowTests/BridgeTest.unity")){throw 'Run Run-UnitySmoke.ps1 first.'}
Copy-Item "$PSScriptRoot/Unity/FlowLive.cs" "$testRoot/Assets/TextureFlowTests/Editor/FlowLive.cs" -Force
$ready=Join-Path $testRoot 'Library/TextureFlow/live-ready.txt'
$stop=Join-Path $testRoot 'Library/TextureFlow/live-stop.txt'
foreach($file in @($ready,$stop)){if(Test-Path -LiteralPath $file){Remove-Item -LiteralPath $file}}
$log=Join-Path $testRoot 'live-unity.log'
$proc=Start-Process -FilePath $UnityExe -ArgumentList @('-batchmode','-nographics','-projectPath',('"'+$testRoot+'"'),'-executeMethod','FlowLive.Run','-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru
try{
 dotnet run --project "$PSScriptRoot/TextureFlow.Tests.csproj" -c Release -- $testRoot --live
 if($LASTEXITCODE -ne 0){throw "Live apply test failed; inspect $log and live-apply-result.txt"}
}finally{
 New-Item -ItemType Directory -Force (Split-Path $stop -Parent) | Out-Null
 'stop' | Set-Content -LiteralPath $stop
 if(-not $proc.WaitForExit(20000)){Stop-Process -Id $proc.Id}
}
Get-Content "$testRoot/live-apply-result.txt"
