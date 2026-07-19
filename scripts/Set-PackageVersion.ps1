[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^v\d+\.\d+(?:\.\d+)?$')]
    [string]$Tag,

    [string]$ManifestPath = (
        Join-Path $PSScriptRoot '..\Package.appxmanifest'
    )
)

$parts = $Tag.TrimStart('v').Split('.')
$major = [int]$parts[0]
$minor = [int]$parts[1]
$patch = if ($parts.Count -ge 3) { [int]$parts[2] } else { 0 }

foreach ($value in @($major, $minor, $patch)) {
    if ($value -lt 0 -or $value -gt 65535) {
        throw "Each package version component must be between 0 and 65535."
    }
}

$resolvedManifest = (Resolve-Path -LiteralPath $ManifestPath).Path
[xml]$manifest = Get-Content -LiteralPath $resolvedManifest -Raw
$manifest.Package.Identity.Version = "$major.$minor.$patch.0"

$settings = [System.Xml.XmlWriterSettings]::new()
$settings.Encoding = [System.Text.UTF8Encoding]::new($false)
$settings.Indent = $true
$settings.NewLineChars = "`r`n"

$writer = [System.Xml.XmlWriter]::Create($resolvedManifest, $settings)
try {
    $manifest.Save($writer)
}
finally {
    $writer.Dispose()
}

Write-Output "Package version set to $major.$minor.$patch.0"
