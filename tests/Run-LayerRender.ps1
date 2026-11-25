param([string]$UnityExe='C:/Program Files/Unity/Hub/Editor/2022.3.22f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$testRoot=Join-Path (Split-Path $PSScriptRoot -Parent) 'build/unity-apply-test'
Copy-Item "$PSScriptRoot/Unity/LayerRenderSmoke.cs" "$testRoot/Assets/TextureFlowTests/Editor/LayerRenderSmoke.cs" -Force
$log=Join-Path $testRoot 'layer-render.log'
$proc=Start-Process -FilePath $UnityExe -ArgumentList @('-batchmode','-force-d3d11','-projectPath',('"'+$testRoot+'"'),'-executeMethod','LayerRenderSmoke.Run','-logFile',('"'+$log+'"')) -WindowStyle Hidden -PassThru -Wait
if($proc.ExitCode -ne 0){throw "GPU render test failed; inspect $log"}
Get-Content "$testRoot/layer-render-result.txt"
