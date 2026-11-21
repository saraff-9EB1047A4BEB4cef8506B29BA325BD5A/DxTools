# Code signing and UIAccess

## Authenticode

`build/sign.ps1` signs files with a certificate from the CurrentUser store:

```powershell
$env:DXTOOLS_SIGN_THUMBPRINT = "<certificate thumbprint>"
powershell -ExecutionPolicy Bypass -File build/sign.ps1 `
  -Path artifacts\publish\DxTools.exe, `
        artifacts\publish\DxTools.Native.dll, `
        artifacts\publish\DxTools.Helper.exe, `
        artifacts\shellext\DxTools.ShellExt.dll, `
        artifacts\DxTools-1.0.0-x64.msi
```

Sign after staging and before publishing the MSI/release. Use SHA-256 and a
timestamp server.

## UIAccess

`DxTools.Helper.exe` uses the manifest
`<requestedExecutionLevel level="asInvoker" uiAccess="true" />`. For UIAccess to
be granted, all of the following are required:

- `DxTools.Helper.exe` is signed with an Authenticode certificate trusted by
  the machine (the chain must be trusted, and the certificate must allow code
  signing);
- the helper is installed under `%ProgramFiles%\DxTools` (the MSI does this);
- the user approves the Windows UIAccess prompt on first launch, or UIAccess is
  allowed by policy.

Build a signed package in one step:

```powershell
$env:DXTOOLS_SIGN_THUMBPRINT = "<certificate thumbprint>"
powershell -ExecutionPolicy Bypass -File build/build.ps1 `
  -Configuration Release `
  -ProductVersion 1.0.0 `
  -SignThumbprint $env:DXTOOLS_SIGN_THUMBPRINT
```

`build.ps1` signs the helper, app, native DLL, shell extension and screensaver
**before** generating the WiX fragment and MSI, then signs the MSI. This is
required because UIAccess checks the signature of the installed helper, not the
MSI signature alone.

If the helper is unsigned, missing or the UIAccess prompt is denied, Windows
does not grant UIAccess. DxTools detects the failure, logs it and falls back to
normal Win32 window operations.

For a self-signed test certificate, import the certificate into
`Trusted Root Certification Authorities` and `Trusted Publishers` on the test
machine; otherwise Windows will not grant UIAccess.
