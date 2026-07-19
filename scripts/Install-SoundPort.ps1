[CmdletBinding()]
param(
    [string]$PackagePath
)

$ErrorActionPreference = 'Stop'

$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($currentIdentity)
$isAdministrator = $principal.IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdministrator) {
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    if (-not [string]::IsNullOrWhiteSpace($PackagePath)) {
        $arguments += " -PackagePath `"$PackagePath`""
    }

    $process = Start-Process `
        -FilePath 'powershell.exe' `
        -ArgumentList $arguments `
        -Verb RunAs `
        -Wait `
        -PassThru
    exit $process.ExitCode
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$certificatePath = Join-Path $scriptDirectory 'SoundPort.cer'

if (-not (Test-Path -LiteralPath $certificatePath)) {
    throw "SoundPort.cer was not found next to this script."
}

if ([string]::IsNullOrWhiteSpace($PackagePath)) {
    $package = Get-ChildItem -LiteralPath $scriptDirectory -Filter '*.msix' |
        Select-Object -First 1
    if ($null -eq $package) {
        throw "No .msix package was found next to this script."
    }
    $PackagePath = $package.FullName
}

$resolvedPackage = (Resolve-Path -LiteralPath $PackagePath).Path

Write-Host 'Installing the SoundPort signing certificate for this computer...'
Import-Certificate `
    -FilePath $certificatePath `
    -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null

Write-Host 'Installing SoundPort...'
$dependencyDirectory = Join-Path $scriptDirectory 'Dependencies'
$dependencies = @()
if (Test-Path -LiteralPath $dependencyDirectory) {
    $dependencies = @(
        Get-ChildItem -LiteralPath $dependencyDirectory -Filter '*.msix' |
            ForEach-Object { $_.FullName }
    )
}

if ($dependencies.Count -gt 0) {
    Add-AppxPackage -Path $resolvedPackage -DependencyPath $dependencies
}
else {
    Add-AppxPackage -Path $resolvedPackage
}

Write-Host 'SoundPort was installed successfully.'
