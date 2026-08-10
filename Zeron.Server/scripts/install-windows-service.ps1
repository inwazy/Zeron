#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Install Zeron.Server as a Windows Service.

.PARAMETER InstallPath
  Directory containing Zeron.Server.exe (default: script folder or current directory).

.PARAMETER ServiceName
  SCM service name (default: Zeron.Server).

.PARAMETER DisplayName
  Services MMC display name.

.PARAMETER Urls
  ASPNETCORE_URLS for the service (default: http://0.0.0.0:5000).
#>
param(
    [string]$InstallPath = "",
    [string]$ServiceName = "Zeron.Server",
    [string]$DisplayName = "Zeron Server",
    [string]$Description = "Zeron central management server (Dashboard, API, agent command PUB)",
    [string]$Urls = "http://0.0.0.0:5000"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($InstallPath)) {
    if (Test-Path (Join-Path $PSScriptRoot "Zeron.Server.exe")) {
        $InstallPath = $PSScriptRoot
    }
    elseif (Test-Path (Join-Path (Get-Location).Path "Zeron.Server.exe")) {
        $InstallPath = (Get-Location).Path
    }
    else {
        throw "Zeron.Server.exe not found. Pass -InstallPath or run from the publish folder."
    }
}

$exe = Join-Path $InstallPath "Zeron.Server.exe"
if (-not (Test-Path $exe)) {
    throw "Zeron.Server.exe not found in '$InstallPath'. Publish the server first, then run this script from the publish folder."
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -ne $existing) {
    throw "Service '$ServiceName' already exists. Run uninstall-windows-service.ps1 first."
}

$binPath = "`"$exe`""
Write-Host "Installing service '$ServiceName' from $exe"

New-Service `
    -Name $ServiceName `
    -BinaryPathName $binPath `
    -DisplayName $DisplayName `
    -Description $Description `
    -StartupType Automatic | Out-Null

$regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
$environment = @(
    "ASPNETCORE_ENVIRONMENT=Production",
    "ASPNETCORE_URLS=$Urls"
)
New-ItemProperty -Path $regPath -Name Environment -PropertyType MultiString -Value $environment -Force | Out-Null

Start-Service -Name $ServiceName
Write-Host "Service '$ServiceName' installed and started."
Write-Host "Ensure appsettings.Production.json (or env secrets) is configured under: $InstallPath"
Write-Host "Health: GET $($Urls.TrimEnd('/'))/health"
