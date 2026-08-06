#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Stop and remove the Zeron.Server Windows Service.

.PARAMETER ServiceName
  SCM service name (default: Zeron.Server).
#>
param(
    [string]$ServiceName = "Zeron.Server"
)

$ErrorActionPreference = "Stop"

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -eq $existing) {
    Write-Host "Service '$ServiceName' is not installed."
    return
}

if ($existing.Status -ne "Stopped") {
    Write-Host "Stopping '$ServiceName'..."
    Stop-Service -Name $ServiceName -Force
}

Write-Host "Removing '$ServiceName'..."
sc.exe delete $ServiceName | Out-Null

# Wait briefly for SCM to drop the service
Start-Sleep -Seconds 2
Write-Host "Service '$ServiceName' removed."
