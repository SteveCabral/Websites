param(
  [string]$ServiceName = "FamilyGameServer"
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

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -eq $existing) {
  Write-Host "Service '$ServiceName' not found."
  exit 0
}

Write-Host "Stopping service…"
try { Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue } catch {}

Write-Host "Deleting service…"
sc.exe delete $ServiceName | Out-Null

Write-Host "Done."
