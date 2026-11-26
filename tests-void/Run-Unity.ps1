param([string]$UnityExe='C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$testRoot=Join-Path $root 'build/void-test'
New-Item -ItemType Directory -Force "$testRoot/Assets/TextureFlowTests/Editor","$testRoot/Packages","$testRoot/ProjectSettings" | Out-Null
if(-not(Test-Path "$testRoot/Packages/manifest.json")){'{"dependencies":{"com.unity.modules.imgui":"1.0.0"}}' | Set-Content -Encoding UTF8 "$testRoot/Packages/manifest.json"}
if(-not(Test-Path "$testRoot/ProjectSettings/ProjectVersion.txt")){'m_EditorVersion: 2022.3.22f1' | Set-Content -Encoding UTF8 "$testRoot/ProjectSettings/ProjectVersion.txt"}
Copy-Item "$PSScriptRoot/Unity/VoidSmoke.cs" "$testRoot/Assets/TextureFlowTests/Editor/VoidSmoke.cs" -Force
$log=Join-Path $testRoot 'void-unity.log'
$proc=Start-Process -FilePath $UnityExe -ArgumentList @('-batchmode','-force-d3d11','-projectPath',('"'+$testRoot+'"'),'-executeMethod','VoidSmoke.Run','-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru -Wait
if($proc.ExitCode -ne 0){throw "Void Unity test failed; inspect $log"}
Get-Content "$testRoot/void-unity-result.txt"
