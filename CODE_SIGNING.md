# Code signing policy

DxTools is an open-source project licensed under GNU GPL v2
(`GPL-2.0-only`). Release artifacts are signed through
[SignPath.io](https://signpath.io/).

**Free code signing provided by SignPath.io, certificate by SignPath Foundation.**

## Publisher and key ownership

The code signing certificate is issued to **SignPath Foundation**. The private
key is generated and stored in SignPath Foundation's hardware security module
(HSM) and is never exported. DxTools maintainers do not hold the signing key.
The publisher shown by Windows is `SignPath Foundation`, not `DxTools`.

## Signed artifacts

The following artifacts are built from this repository by GitHub Actions on
GitHub-hosted runners and submitted to SignPath for signing:

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
3. The workflow submits a signing request to SignPath with a CI API token.
4. SignPath verifies the build origin (repository, branch, commit and build
   job) before accepting the request.
5. An approver reviews and manually approves every signing request.
6. The signed artifact is downloaded, verified with `signtool verify /pa /all`
   and published in a GitHub Release.

Signing requests are accepted only for the `main` branch. Builds from local
machines, self-hosted runners or forks cannot be signed.

## Roles and responsibilities

| Role | Holder | Responsibility |
|---|---|---|
| Author | SARAFF (`saraff-9EB1047A4BEB4cef8506B29BA325BD5A`) | Commits and changes to the repository |
| Reviewer | SARAFF | Reviews every pull request from a non-committer before merge |
| Approver | SARAFF | Approves or denies every signing request |
| CI submitter | Dedicated SignPath CI user | Submits artifacts from GitHub Actions; cannot approve requests |

All maintainers and signing users must use multi-factor authentication for
GitHub and SignPath access. Changes to build scripts, CI configuration and
this policy are reviewed like any other change.

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

A valid release signature has:

- signer subject containing `SignPath Foundation`;
- a valid timestamp;
- a certificate chain ending in a Windows-trusted root.

The same signature is present on the nested PE files listed above after the
MSI is installed or extracted.

## Reporting a problem

Report signing or supply-chain concerns privately as described in
[SECURITY.md](SECURITY.md). General questions: `support.dxtools@saraff.ru`.
