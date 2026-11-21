param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$ProductVersion = "1.0.0",
    [string]$ZigPath = "",
    [string]$Architecture = "x64",
    [string]$InstallerPlatform = "x64",
    [switch]$SkipTests,
    [switch]$SkipNative
)

$ErrorActionPreference = "Stop"

if ($InstallerPlatform -eq "arm64") {
    if ($Architecture -eq "x64") { $Architecture = "arm64" }
    if ($Runtime -eq "win-x64") { $Runtime = "win-arm64" }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifacts "publish"
$nativeDir = Join-Path $artifacts "native"
$wixProject = Join-Path $repoRoot "installer\DxTools.Installer\DxTools.Installer.wixproj"
$fragment = Join-Path $repoRoot "installer\DxTools.Installer\GeneratedComponents.wxs"

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

Write-Output "== DxTools build =="
Write-Output "Configuration: $Configuration"
Write-Output "Runtime:       $Runtime"
Write-Output "Architecture:  $Architecture"
Write-Output "Installer:     $InstallerPlatform"
Write-Output "Version:       $ProductVersion"

if (-not $SkipNative) {
    Write-Output "== Native =="
    & (Join-Path $PSScriptRoot "build-native.ps1") `
        -Configuration $Configuration `
        -OutputDirectory $nativeDir `
        -ZigPath $ZigPath `
        -Architecture $Architecture
}
elseif (-not (Test-Path -LiteralPath (Join-Path $nativeDir "DxTools.Native.dll"))) {
    throw "Native DLL is missing in '$nativeDir' and -SkipNative was specified."
}

if (-not $SkipTests) {
    Write-Output "== Tests =="
    & dotnet test (Join-Path $repoRoot "DxTools.sln") -c $Configuration -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed with exit code $LASTEXITCODE."
    }
}

Write-Output "== Publish =="
if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

& dotnet publish (Join-Path $repoRoot "src\DxTools.App\DxTools.App.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $publishDir `
    -p:PublishReadyToRun=false
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath (Join-Path $nativeDir "DxTools.Native.dll") `
    -Destination (Join-Path $publishDir "DxTools.Native.dll") `
    -Force

Write-Output "== Screensaver =="
$screensaverDir = Join-Path $artifacts "screensaver"
if (Test-Path -LiteralPath $screensaverDir) {
    Remove-Item -LiteralPath $screensaverDir -Recurse -Force
}

& dotnet publish (Join-Path $repoRoot "src\DxTools.Screensaver\DxTools.Screensaver.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $screensaverDir
if ($LASTEXITCODE -ne 0) {
    throw "Screensaver publish failed with exit code $LASTEXITCODE."
}

Copy-Item -LiteralPath (Join-Path $screensaverDir "DxTools.Screensaver.exe") `
    -Destination (Join-Path $publishDir "DxTools.scr") `
    -Force

Write-Output "== WiX fragment =="
& (Join-Path $PSScriptRoot "generate-wix-fragment.ps1") `
    -PublishDir $publishDir `
    -OutputFile $fragment

Write-Output "== MSI =="
& dotnet build $wixProject `
    -c $Configuration `
    "-p:ProductVersion=$ProductVersion" `
    "-p:InstallerPlatform=$InstallerPlatform"
if ($LASTEXITCODE -ne 0) {
    throw "WiX build failed with exit code $LASTEXITCODE."
}

$msiName = "DxTools-$ProductVersion-$InstallerPlatform.msi"
$msi = Get-ChildItem -LiteralPath $artifacts -Recurse -Filter $msiName -File -ErrorAction SilentlyContinue |
    Select-Object -First 1

if (-not $msi) {
    throw "MSI '$msiName' was not found under '$artifacts'."
}

$targetMsi = Join-Path $artifacts $msiName
if ($msi.FullName -ne $targetMsi) {
    Copy-Item -LiteralPath $msi.FullName -Destination $targetMsi -Force
}

Write-Output "== Done =="
Write-Output "Publish: $publishDir"
Write-Output "MSI:     $targetMsi ($((Get-Item -LiteralPath $targetMsi).Length) bytes)"
