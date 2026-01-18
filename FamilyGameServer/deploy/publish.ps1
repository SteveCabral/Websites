param(
  [string]$ProjectPath = "..\\FamilyGameServer.csproj",
  [string]$OutDir = ".\\out",
  [ValidateSet("Release","Debug")]
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $here

try {
  Write-Host "Publishing to $OutDir ($Configuration)…"

  if (Test-Path $OutDir) {
    Remove-Item -Recurse -Force $OutDir
  }

  dotnet publish $ProjectPath -c $Configuration -o $OutDir

  $deployOut = Join-Path $OutDir "deploy"
  New-Item -ItemType Directory -Force -Path $deployOut | Out-Null
  Copy-Item -Force -Path (Join-Path $here "install-service.ps1") -Destination $deployOut
  Copy-Item -Force -Path (Join-Path $here "uninstall-service.ps1") -Destination $deployOut
  Copy-Item -Force -Path (Join-Path $here "run-console.ps1") -Destination $deployOut

  Write-Host "Publish complete: $OutDir"
}
finally {
  Pop-Location
}
