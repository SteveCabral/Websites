param(
  [string]$AppDir = ".\\out",
  [string]$Urls = "http://192.168.1.208:5000"
)

$ErrorActionPreference = "Stop"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Push-Location $here

try {
  $exe = Join-Path $AppDir "FamilyGameServer.exe"
  if (-not (Test-Path $exe)) {
    throw "Expected $exe. Run publish.ps1 first, or point -AppDir to the published folder."
  }

  Write-Host "Starting: $exe --urls $Urls"
  & $exe --urls $Urls
}
finally {
  Pop-Location
}
