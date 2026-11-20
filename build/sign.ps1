param(
    [Parameter(Mandatory = $true)]
    [string[]]$Path,
    [string]$Thumbprint = $env:DXTOOLS_SIGN_THUMBPRINT,
    [string]$TimestampServer = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Thumbprint)) {
    throw "Set DXTOOLS_SIGN_THUMBPRINT or pass -Thumbprint."
}

$signtool = (Get-Command signtool.exe -ErrorAction SilentlyContinue).Source
if (-not $signtool) {
    $candidates = @(
        "C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0\x64\signtool.exe",
        "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"
    )
    $signtool = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

if (-not $signtool) {
    throw "signtool.exe was not found. Install the Windows SDK signing tools."
}

foreach ($file in $Path) {
    if (-not (Test-Path -LiteralPath $file)) {
        throw "File not found: $file"
    }

    & $signtool sign /sha1 $Thumbprint /fd SHA256 /tr $TimestampServer /td SHA256 $file
    if ($LASTEXITCODE -ne 0) {
        throw "Signing failed for '$file' with exit code $LASTEXITCODE."
    }
}
