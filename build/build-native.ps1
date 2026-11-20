param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "",
    [string]$ZigPath = "",
    [string]$Architecture = "x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\native"
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

function Resolve-Zig {
    param([string]$ExplicitPath)

    if (-not [string]::IsNullOrWhiteSpace($ExplicitPath)) {
        if (Test-Path -LiteralPath $ExplicitPath) { return (Resolve-Path -LiteralPath $ExplicitPath).Path }
        throw "Zig not found at '$ExplicitPath'."
    }

    if (-not [string]::IsNullOrWhiteSpace($env:DXTOOLS_ZIG) -and (Test-Path -LiteralPath $env:DXTOOLS_ZIG)) {
        return (Resolve-Path -LiteralPath $env:DXTOOLS_ZIG).Path
    }

    $localZig = Join-Path $repoRoot "tools\zig\zig.exe"
    if (Test-Path -LiteralPath $localZig) { return $localZig }

    $command = Get-Command zig -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    return $null
}

$cmake = Get-Command cmake -ErrorAction SilentlyContinue
$vswhere = "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"
$msvcPath = $null
if (Test-Path -LiteralPath $vswhere) {
    $msvcPath = & $vswhere -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath 2>$null
}

if ($cmake -and -not [string]::IsNullOrWhiteSpace($msvcPath)) {
    Write-Output "Building native library with CMake + MSVC."
    $buildDir = Join-Path $repoRoot "native\build"
    $cmakeArch = if ($Architecture -eq "arm64") { "ARM64" } else { "x64" }; $cmakeArgs = @("-S", (Join-Path $repoRoot "native"), "-B", $buildDir, "-A", $cmakeArch)
    if ($Configuration -eq "Release") {
        $cmakeArgs += "-DCMAKE_CONFIGURATION_TYPES=Release"
    }

    & $cmake.Source @cmakeArgs
    if ($LASTEXITCODE -ne 0) { throw "CMake configure failed with exit code $LASTEXITCODE." }

    & $cmake.Source "--build" $buildDir "--config" $Configuration
    if ($LASTEXITCODE -ne 0) { throw "CMake build failed with exit code $LASTEXITCODE." }

    $candidates = @(
        (Join-Path $buildDir "out\$Configuration\DxTools.Native.dll"),
        (Join-Path $buildDir "out\DxTools.Native.dll")
    )
    $built = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    if (-not $built) { throw "Native DLL was not produced by CMake." }

    Copy-Item -LiteralPath $built -Destination (Join-Path $OutputDirectory "DxTools.Native.dll") -Force
}
else {
    $zig = Resolve-Zig -ExplicitPath $ZigPath
    if (-not $zig) {
        throw "No C++ toolchain found. Install Visual Studio C++ workload + Windows SDK, or set -ZigPath / DXTOOLS_ZIG."
    }

    Write-Output "Building native library with Zig: $zig"
    $source = Join-Path $repoRoot "native\src\DxToolsNative.cpp"
    $include = Join-Path $repoRoot "native\include"
    $output = Join-Path $OutputDirectory "DxTools.Native.dll"
    $optimization = if ($Configuration -eq "Debug") { "-O0" } else { "-O2" }

    & $zig "c++" "-target" $(if ($Architecture -eq "arm64") { "aarch64-windows-gnu" } else { "x86_64-windows-gnu" }) "-std=c++20" $optimization "-Wall" "-Wextra" "-Werror" "-shared" `
        "-DUNICODE" "-D_UNICODE" "-DWIN32_LEAN_AND_MEAN" "-DNOMINMAX" "-I$include" $source "-o" $output "-lole32" "-loleaut32" "-lpropsys" "-luuid"
    if ($LASTEXITCODE -ne 0) { throw "Zig build failed with exit code $LASTEXITCODE." }
}

$result = Get-Item -LiteralPath (Join-Path $OutputDirectory "DxTools.Native.dll")
Write-Output "Native DLL: $($result.FullName) ($($result.Length) bytes)"
