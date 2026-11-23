# Code signing policy

DxTools is an open-source project licensed under GNU GPL v2
(`GPL-2.0-only`).

## Current status

DxTools does not currently have an active code signing certificate. The
SignPath Foundation application was reviewed on 2026-11-23 and deferred until
the project shows more public adoption. Major commercial certificate
authorities do not currently issue code signing certificates to applicants in
Belarus, so a paid OV certificate is not available at this time. Release
artifacts are published unsigned.

The project will reapply to the SignPath Foundation after it has gained
broader public adoption. This policy will be updated when signing is enabled.

## Publisher and key ownership

The certificate will be issued either to the maintainer or to a registered
organization, depending on the selected certificate. The private key will be
stored in the certificate authority's cloud HSM or on a hardware token and
will not be exported. The publisher shown by Windows will be the certificate
subject, not necessarily `DxTools`.

## Artifacts to be signed

When signing is enabled, the following artifacts will be built from this
repository by GitHub Actions on GitHub-hosted runners and signed:

- `DxTools-<version>-x64.msi` — Microsoft Installer package;
- `DxTools.exe` — main application;
- `DxTools.Native.dll` — native helper library;
- `DxTools.Helper.exe` — UIAccess helper;
- `DxTools.scr` — screensaver;
- `DxTools.ShellExt.dll` — Explorer shell extension.

The MSI is deep-signed: the PE files listed above are signed first, then the
MSI itself. Third-party .NET assemblies inside the MSI are not signed
individually; their integrity is covered by the MSI signature.

Local developer builds, forks, pull-request builds and CI artifacts created
before the signing step are **not** signed.

## Release and signing process

1. A release build is produced by a GitHub Actions workflow in this repository
   using `build/build.ps1` on a GitHub-hosted Windows runner.
2. The unsigned MSI is uploaded as a GitHub Actions artifact.
3. The workflow submits a signing request through the selected signing
   provider, or signing is performed with a hardware-backed certificate.
4. The provider verifies the build origin where supported.
5. The maintainer reviews and manually approves every signing request.
6. The signed artifact is verified with `signtool verify /pa /all` and
   published in a GitHub Release.

Signing requests are accepted only for the `main` branch. Builds from local
machines, self-hosted runners or forks cannot be signed.

## Roles and responsibilities

| Role | Holder | Responsibility |
|---|---|---|
| Author | SARAFF (`saraff-9EB1047A4BEB4cef8506B29BA325BD5A`) | Commits and changes to the repository |
| Reviewer | SARAFF | Reviews every pull request from a non-committer before merge |
| Approver | SARAFF | Approves or denies every signing request |
| Signing submitter | Dedicated CI identity or the maintainer | Submits artifacts for signing; cannot approve requests |

All maintainers and signing users must use multi-factor authentication for
GitHub and the signing provider. Changes to build scripts, CI configuration
and this policy are reviewed like any other change.

## Privacy and system changes

DxTools does not contain telemetry, analytics or automatic update checks, and
it does not open network connections. Settings and logs are stored locally
under the user's profile. The MSI installs per machine, registers an
uninstaller and can be removed through Windows Settings or `msiexec /x`.

User-authored C# scripts and programs started by the user can perform network
access at the user's direction; this is outside DxTools itself and is
described in [SECURITY.md](SECURITY.md).

## Verifying a release

On Windows with the Windows SDK installed:

```powershell
signtool verify /pa /all DxTools-1.0.0-x64.msi
Get-AuthenticodeSignature .\DxTools-1.0.0-x64.msi | Format-List
```

A valid signed release has:

- a signer subject containing the certificate holder's name (individual or
  organization);
- a valid timestamp;
- a certificate chain ending in a Windows-trusted root.

The same signature is present on the nested PE files listed above after the
MSI is installed or extracted. If a release is unsigned,
`Get-AuthenticodeSignature` reports `NotSigned`.

## Reporting a problem

Report signing or supply-chain concerns privately as described in
[SECURITY.md](SECURITY.md). General questions: `support.dxtools@saraff.ru`.
