# SignPath Foundation — заявка

> Статус: заявка отправлена 22.11.2026, рассмотрена 23.11.2026 и отклонена.
> Причина: недостаточно внешних сигналов публичного доверия (stars, forks,
> contributors, статьи, независимые упоминания). Повторная заявка возможна
> после роста проекта. Номер заявки не выдавался; переписка идёт по e-mail.

Рабочий документ для подачи заявки на `signpath.org/apply`.
Форма — HubSpot-iframe; поля и подсказки проверены 22.11.2026.

## 1. Значения полей формы

| Поле | Значение |
|---|---|
| Project Name* | DxTools |
| Repository URL* | `https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools` |
| Homepage URL* | `https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools` |
| Download URL | `https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools/releases/tag/v1.0.0` |
| Privacy Policy URL | `https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools/blob/main/CODE_SIGNING.md#privacy-and-system-changes` |
| Wikipedia URL | оставить пустым |
| Tagline* | `Open-source Windows 11 utility for multi-monitor taskbars, window profiles, wallpapers, and global hotkeys.` |
| Category* | `Productivity` |
| Description* | текст из раздела 2.1 |
| Reputation* | текст из раздела 2.2 |
| Maintainer Type | `Individual maintainer(s)` |
| Build System* | `GitHub Actions` |
| First Name* | ваше имя для аккаунта SignPath |
| Last Name* | ваша фамилия для аккаунта SignPath |
| Email* | `support.dxtools@saraff.ru` (замените, если нужен другой адрес) |
| Company Name | оставить пустым |
| Primary Discovery Channel* | `AI / LLM tools` (или фактический канал) |
| Please specify the exact source | `Kilo Code` или оставить пустым |
| Code of Conduct / certificate consent* | включить |
| Receive other communications | по желанию, не обязательно |
| Personal data processing consent* | включить |

Download URL должен упоминать SignPath Foundation. Страница релиза
`v1.0.0` содержит ссылку на `CODE_SIGNING.md` и строку о SignPath
Foundation, поэтому подходит.

## 2. Тексты для полей

### 2.1 Description

> DxTools is an open-source Windows desktop utility for multi-monitor
> workflows. It provides per-monitor taskbars, monitor and window profiles,
> wallpaper management, global hotkeys with application scoping, triggers, a
> multi-monitor screensaver, and Explorer integration. It is distributed as a
> free GPL-2.0-only build for Windows 11 and maintained by its author.

### 2.2 Reputation

> DxTools is a new project: its first public release was published in November
> 2026. Trust evidence: the complete source code, build scripts, and CI
> configuration are public; GitHub Actions builds and tests every change,
> including 59 automated tests; SECURITY.md and a published security review
> document the threat model; releases include SHA-256 checksums and a public
> code signing policy.
>
> Repository: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
> Releases: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools/releases
> CI: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools/actions
>
> No media coverage yet.

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

The MSI is deep-signed: `DxTools.exe`, `DxTools.Native.dll`,
`DxTools.Helper.exe`, `DxTools.scr`, and `DxTools.ShellExt.dll` are signed
inside the package, then the MSI itself is signed. Third-party .NET assemblies
inside the MSI are not signed individually; their integrity is covered by the
MSI signature. Local builds, forks, and pull-request artifacts are never
signed.

### Почему publisher будет SignPath Foundation

The project accepts the SignPath Foundation certificate and the publisher name
`SignPath Foundation`. A separate Store/MSIX identity is not part of this
request.

## 4. Перед отправкой

- [x] Изменения M0 (`CODE_SIGNING.md`, README) находятся в `main`.
- [x] `CODE_SIGNING.md` открывается по публичному URL и содержит точную фразу
      `Free code signing provided by SignPath.io, certificate by SignPath Foundation`.
- [x] В README есть раздел `Code signing policy` со ссылкой на политику.
- [x] Включена 2FA на GitHub.
- [x] Ruleset для `main`: запрет удаления, force-push, изменения через PR.
- [x] Проверены ссылки из таблицы выше (открываются без 404).
- [x] Issues/Discussions включены и доступны публично.
- [x] Опубликован стабильный релиз `v1.0.0` с MSI и упоминанием SignPath.
- [x] Заявка отправлена 22.11.2026, подтверждение формы получено.

## 5. Порядок подачи

1. Открыть `https://signpath.org/apply`.
2. Заполнить поля из раздела 1, вставить тексты из раздела 2.
3. Включить обязательные чекбоксы Code of Conduct и обработки данных.
4. Пройти reCAPTCHA и нажать Submit.
5. Сохранить письмо-подтверждение и номер заявки.
6. Отвечать на уточнения ревьюеров по e-mail; типовые ответы — в разделе 3.
7. После одобрения принять приглашение в организацию SignPath.io и включить
   MFA в SignPath.
8. Перейти к M2: проект, trusted build system, signing policy и artifact
   configuration.

## 6. Отказ 23.11.2026

Причина из письма Phillip Deng: программа Foundation рассчитана на проекты с
уже установленным уровнем публичного доверия и известности. DxTools не
показал достаточных внешних сигналов: community adoption (stars, forks,
contributors), внешние статьи, независимые упоминания или институциональная
поддержка. Отказ не связан с качеством кода.

Что делать:

- растить публичные сигналы: stars/forks/contributors, статьи, Reddit,
  YouTube, Stack Overflow, пакетные менеджеры, статистика загрузок;
- подать повторно через несколько месяцев после измеримого роста;
- либо не ждать и купить сертификат: Certum Standard OV (от €139–209/год,
  без OSS-ограничения), Certum Open Source (€49/год, только некоммерческое
  ПО), SignPath Starter + EV GlobalSign (от $500/год, для юрлица).

Важно: для заявителей из Беларуси коммерческие CA недоступны — Certum,
DigiCert, GlobalSign и Sectigo приостановили выдачу code signing
сертификатов. OV-вариант возможен только при ВНЖ/юрлице вне Беларуси.
Основной путь — повторная заявка в SignPath Foundation после роста публичной
известности (см. §14 `docs/signpath-plan.md`).

Подробное сравнение — `docs/signpath-plan.md` §13.

Если отказ принципиальный, резервный путь — Certum Open Source Code Signing
(платно, publisher — `Open Source Developer <имя>`).
