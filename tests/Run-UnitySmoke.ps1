param([string]$UnityExe='C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$testRoot=Join-Path $projectRoot 'build/unity-apply-test'
New-Item -ItemType Directory -Force "$testRoot/Assets/TextureFlowTests/Editor","$testRoot/Packages","$testRoot/ProjectSettings" | Out-Null
if(-not(Test-Path "$testRoot/Packages/manifest.json")){'{"dependencies":{"com.unity.modules.imgui":"1.0.0"}}' | Set-Content -Encoding UTF8 "$testRoot/Packages/manifest.json"}
if(-not(Test-Path "$testRoot/ProjectSettings/ProjectVersion.txt")){'m_EditorVersion: 2022.3.22f1' | Set-Content -Encoding UTF8 "$testRoot/ProjectSettings/ProjectVersion.txt"}
Copy-Item "$PSScriptRoot/Unity/FlowSmoke.cs" "$testRoot/Assets/TextureFlowTests/Editor/FlowSmoke.cs" -Force
dotnet run --project "$PSScriptRoot/TextureFlow.Tests.csproj" -c Release -- $testRoot
if($LASTEXITCODE -ne 0){throw 'Desktop tests failed'}
$log=Join-Path $testRoot 'unity-smoke.log'
$proc=Start-Process -FilePath $UnityExe -ArgumentList @('-batchmode','-nographics','-projectPath',('"'+$testRoot+'"'),'-executeMethod','FlowSmoke.Run','-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru -Wait
if($proc.ExitCode -ne 0){throw "Unity failed; inspect $log"}
Get-Content "$testRoot/unity-smoke-result.txt"

