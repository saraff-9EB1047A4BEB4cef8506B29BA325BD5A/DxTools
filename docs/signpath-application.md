# SignPath Foundation — заявка

Рабочий документ для подачи заявки на `signpath.org/apply`.
После одобрения и настройки проекта файл можно удалить или оставить как
историю.

## 1. Значения полей

| Поле формы | Значение |
|---|---|
| Project name | DxTools |
| Repository URL | `https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools` |
| License | `GPL-2.0-only` |
| License URL | `https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools/blob/main/LICENSE` |
| Download / releases URL | `https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools/releases` |
| Code signing policy URL | `https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools/blob/main/CODE_SIGNING.md` |
| Project website | `https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools` |
| Maintainer | SARAFF |
| Contact e-mail | `support.dxtools@saraff.ru` |

## 2. Описание проекта для формы

> DxTools is an open-source Windows 11 utility distributed under
> GPL-2.0-only. It provides multi-monitor window management, per-monitor
> taskbars, monitor and window profiles, wallpapers, global hotkeys with
> application scoping, triggers, a multi-monitor screensaver, and an Explorer
> context-menu extension.
>
> The project is publicly released on GitHub; the current stable release is
> v1.0.0 with an x64 MSI installer (binary built from commit `9107d1a`). It is
> actively maintained by its author, and all source code, build scripts, and CI
> configuration are in the repository.
>
> We request code signing for the x64 MSI installer
> `DxTools-<version>-x64.msi` and the PE files it contains: `DxTools.exe`,
> `DxTools.Native.dll`, `DxTools.Helper.exe`, `DxTools.scr`, and
> `DxTools.ShellExt.dll`. The MSI is built by GitHub Actions on GitHub-hosted
> `windows-latest` runners using `build/build.ps1` (CMake + MSVC for native
> code, WiX 6 from NuGet for the installer). No self-hosted runners or manual
> build overrides are used.
>
> The project contains no proprietary components. There is no commercial
> dual licensing and no closed-source edition. An optional paid Pro Support
> subscription is a services agreement (response times, prioritized fixes,
> invoices) for the same free GPL build; it does not grant different software
> rights.
>
> Signing is needed to remove Unknown Publisher warnings for the MSI channel
> and to enable UIAccess for `DxTools.Helper.exe`. A Microsoft Store/MSIX
> channel is out of scope for this request.

## 3. Ответы на вероятные вопросы ревьюеров

### Статус релиза

The latest public release is stable `v1.0.0`; its MSI is downloadable from
GitHub Releases. The binary was built from commit `9107d1a` (the same build as
the previous release candidate); only documentation changed after that commit.
The next signed release will use the same build and signing workflow.

### Коммерческая деятельность

DxTools is distributed only as a free GPL-2.0-only build. Pro Support is an
optional paid services agreement for that same public build. There is no
proprietary edition, no dual licensing, and no license checks in the code.

### Один мейнтейнер и роли

The project currently has one maintainer (SARAFF), who is Author, Reviewer and
Approver. Pull requests from non-committers are reviewed before merge. A
dedicated non-human CI user submits signing requests from GitHub Actions; the
maintainer approves each request manually. MFA is enabled for GitHub and will
be enabled for SignPath.

### Приватность и изменения системы

DxTools contains no telemetry, analytics, or automatic update checks, and it
does not open network connections. Settings and logs are stored locally under
the user profile. The MSI installs per machine, registers an uninstaller, and
can be removed through Windows Settings or `msiexec /x`. User-authored C#
scripts and programs started by the user may access the network at the user's
direction; this is outside DxTools itself.

### Что именно подписывается

The MSI is deep-signed: the five PE files listed above are signed inside the
package, then the MSI itself is signed. Third-party .NET assemblies inside the
MSI are not signed individually; their integrity is covered by the MSI
signature. Local builds, forks, and pull-request artifacts are never signed.

### Почему publisher будет SignPath Foundation

The project accepts the SignPath Foundation certificate and the publisher name
`SignPath Foundation`. A separate Store/MSIX identity is not part of this
request.

## 4. Перед отправкой

- [x] Изменения M0 (`CODE_SIGNING.md`, README) находятся в ветке `main` на
      GitHub, а не только локально.
- [x] `CODE_SIGNING.md` открывается по публичному URL и содержит точную фразу
      `Free code signing provided by SignPath.io, certificate by SignPath Foundation`.
- [x] В README есть раздел `Code signing policy` со ссылкой на политику.
- [ ] Включена 2FA на GitHub.
- [x] Настроен ruleset для `main` (запрет удаления и force-push, изменения
      через PR).
- [x] Проверены ссылки из таблицы выше (открываются без 404).
- [x] Issues/Discussions включены и доступны публично.
- [x] Опубликован стабильный релиз `v1.0.0` с MSI.

## 5. Порядок подачи

1. Открыть `https://signpath.org/apply`.
2. Заполнить поля из раздела 1, вставить описание из раздела 2.
3. Отправить заявку и сохранить подтверждение.
4. Отвечать на уточнения ревьюеров по e-mail; типовые ответы — в разделе 3.
5. После одобрения принять приглашение в организацию SignPath.io и включить
   MFA в SignPath.
6. Перейти к M2: проект, trusted build system, signing policy и artifact
   configuration.

## 6. Если заявку отклонили

Зафиксировать точную причину из письма. Типовые варианты:

- нужен более свежий стабильный билд — выпустить новый патч-релиз из `main`
  и подать повторно;
- вопросы к коммерческой деятельности — приложить формулировку из раздела 3
  и `docs/commercial.md`;
- недостаточно публичной информации о проекте — дополнить README и Release
  notes;
- не устроила модель ролей — описать CI-пользователя и ручное одобрение.

Если отказ принципиальный, резервный путь — Certum Open Source Code Signing
(платно, publisher — `Open Source Developer <имя>`).
