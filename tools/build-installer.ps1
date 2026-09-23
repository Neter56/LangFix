<#
.SYNOPSIS
    Publishes LangFix and packages it into a per-machine MSI installer.

.DESCRIPTION
    Produces artifacts/LangFix-<version>-x64.msi. The payload is published self-contained,
    so the installed app needs no .NET runtime on the target machine.

    Requires the WiX 5 CLI: dotnet tool install --global wix

.EXAMPLE
    pwsh -File tools/build-installer.ps1
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'artifacts' }

$project = Join-Path $repoRoot 'src\LangFix\LangFix.csproj'
$wxs     = Join-Path $repoRoot 'installer\LangFix.wxs'
$icon    = Join-Path $repoRoot 'src\LangFix\app.ico'
$license = Join-Path $repoRoot 'installer\License.rtf'

if (-not (Get-Command wix -ErrorAction SilentlyContinue)) {
    throw "The WiX CLI was not found. Install it with: dotnet tool install --global wix"
}

[xml]$projectXml = Get-Content -LiteralPath $project
$version = $projectXml.SelectSingleNode('//Version').InnerText.Trim()
Write-Host "Packaging LangFix $version" -ForegroundColor Cyan

$publishDir = Join-Path $OutputDirectory 'publish'
if (Test-Path $publishDir) { Remove-Item -LiteralPath $publishDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

dotnet publish $project -c $Configuration -o $publishDir --self-contained `
    -p:EnableCompressionInSingleFile=true --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE." }

$payload = Join-Path $publishDir 'LangFix.exe'
if (-not (Test-Path $payload)) { throw "Expected payload not found: $payload" }

# The UI and Util extensions must match the installed WiX CLI version.
$wixVersion = ((& wix --version) -split '\+')[0].Trim()
$installedExtensions = (& wix extension list --global) -join "`n"
foreach ($extension in 'WixToolset.UI.wixext', 'WixToolset.Util.wixext') {
    if ($installedExtensions -notmatch [regex]::Escape($extension)) {
        Write-Host "Adding WiX extension $extension/$wixVersion" -ForegroundColor Cyan
        & wix extension add --global "$extension/$wixVersion"
        if ($LASTEXITCODE -ne 0) { throw "Failed to add WiX extension $extension." }
    }
}

$msi = Join-Path $OutputDirectory "LangFix-$version-x64.msi"
& wix build $wxs -arch x64 `
    -ext WixToolset.UI.wixext -ext WixToolset.Util.wixext `
    -d ProductVersion=$version `
    -d PayloadExe=$payload `
    -d IconFile=$icon `
    -d LicenseRtf=$license `
    -o $msi
if ($LASTEXITCODE -ne 0) { throw "wix build failed with exit code $LASTEXITCODE." }

$sizeMb = [math]::Round((Get-Item -LiteralPath $msi).Length / 1MB, 1)
Write-Host "Built $msi ($sizeMb MB)" -ForegroundColor Green
