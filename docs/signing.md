# Code signing and UIAccess

## Authenticode

`build/sign.ps1` signs files with a certificate from the CurrentUser store:

```powershell
$env:DXTOOLS_SIGN_THUMBPRINT = "<certificate thumbprint>"
powershell -ExecutionPolicy Bypass -File build/sign.ps1 `
  -Path artifacts\publish\DxTools.exe, `
        artifacts\publish\DxTools.Native.dll, `
        artifacts\shellext\DxTools.ShellExt.dll, `
        artifacts\DxTools-1.0.0-x64.msi
```

Sign after staging and before publishing the MSI/release. Use SHA-256 and a
timestamp server.

## UIAccess

UIAccess is not enabled in 1.0.0-rc.x. It requires:

- Authenticode signature trusted by the machine;
- binary installed under `%ProgramFiles%`;
- manifest `<requestedExecutionLevel level="asInvoker" uiAccess="true" />`.

Without a trusted certificate the OS refuses UIAccess, so the feature remains
disabled and documented as a limitation. A future helper process can host the
UIAccess manifest separately from the normal UI.
