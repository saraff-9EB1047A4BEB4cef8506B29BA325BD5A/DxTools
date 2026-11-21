# Security policy

## Supported versions

Security fixes are provided for the latest release line. Alpha builds are
provided as-is.

## Reporting a vulnerability

Do not open a public issue for a vulnerability. Report it privately to the
maintainer listed in the repository profile with:

- affected version and Windows build;
- steps to reproduce;
- expected and actual behavior;
- logs from `%LocalAppData%\DxTools\logs` with secrets removed.

## Threat model

DxTools is a local desktop utility that runs with the current user's
privileges. It does not open network listeners and does not send telemetry.

The application intentionally uses:

- a low-level keyboard hook (`WH_KEYBOARD_LL`);
- window enumeration and WinEvent hooks;
- UI Automation for the notification area;
- cross-process memory access to the Explorer desktop icon list;
- in-process COM shell extension loaded by Explorer; it only starts
  `DxTools.exe` with command-line arguments and makes no network calls;
- CoreAudio and display configuration APIs;
- user-provided C# scripts executed with full user privileges.

These capabilities are powerful. Only run trusted builds and trusted scripts.

## Safe mode

`DxTools.exe --safe-mode` disables native probes, hotkeys, taskbar, triggers,
audio changes and other native integrations. Use it to recover from a broken
configuration.

## Code signing

Release binaries should be Authenticode-signed. Unsigned local builds may
trigger SmartScreen. Signing is done with `build/sign.ps1` and the
`DXTOOLS_SIGN_THUMBPRINT` environment variable.

## Dependencies

Only GPLv2-compatible licenses are allowed in shipped code. See
`THIRD-PARTY-NOTICES.md`.
