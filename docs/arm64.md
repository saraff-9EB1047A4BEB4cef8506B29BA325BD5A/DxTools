# ARM64 build

DxTools supports a cross-compiled ARM64 build.

```powershell
powershell -ExecutionPolicy Bypass -File build/build.ps1 `
  -Configuration Release `
  -ProductVersion 1.0.0 `
  -InstallerPlatform arm64 `
  -ZigPath C:\tools\zig\zig.exe `
  -SkipTests
```

`-InstallerPlatform arm64` automatically selects:

- native target `aarch64-windows-gnu` (Zig) or CMake `-A ARM64` (MSVC);
- .NET runtime `win-arm64`;
- WiX `InstallerPlatform=arm64`;
- output `artifacts\DxTools-1.0.0-arm64.msi`.

The ARM64 native DLL, apphost, `coreclr.dll` and `.scr` are PE machine
`0xAA64`. The MSI passes administrative install validation (exit 0, 288 files,
`.scr`, `DxTools.ShellExt.dll` and `DxTools.Helper.exe` present), but the RC has
not been executed on ARM64 hardware.

For CI, use an ARM64 runner for runtime tests; cross-compilation only proves
the build and package layout.
