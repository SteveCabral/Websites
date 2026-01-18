param(
  [string]$ServiceName = "FamilyGameServer",
  [string]$DisplayName = "Family Game Server",
  [string]$AppDir = "C:\\Apps\\FamilyGameServer",
  [string]$Urls = "http://0.0.0.0:5000"
)

$ErrorActionPreference = "Stop"

function Assert-Admin {
  $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
  $principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
  if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run this script from an elevated PowerShell (Run as Administrator)."
  }
}

Assert-Admin

$exe = Join-Path $AppDir "FamilyGameServer.exe"
if (-not (Test-Path $exe)) {
  throw "Expected $exe. Copy published output to $AppDir first."
}

# Create a wrapper .cmd to avoid SC quoting issues.
$cmdPath = Join-Path $AppDir "run-service.cmd"
$cmd = "@echo off`r`n" +
       "cd /d \"$AppDir\"`r`n" +
       "\"$exe\" --urls $Urls`r`n"
Set-Content -Path $cmdPath -Value $cmd -Encoding ASCII

# If service exists, stop & delete.
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -ne $existing) {
  Write-Host "Service '$ServiceName' exists; stopping & deleting…"
  try { Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue } catch {}
  sc.exe delete $ServiceName | Out-Null
  Start-Sleep -Seconds 2
}

Write-Host "Creating service '$ServiceName'…"
sc.exe create $ServiceName binPath= "\"$cmdPath\"" start= auto DisplayName= "\"$DisplayName\"" | Out-Null

Write-Host "Starting service…"
sc.exe start $ServiceName | Out-Null

Write-Host "Service installed and started."
Write-Host "Browse: http://<this-machine-ip>:5000/host and /play"
