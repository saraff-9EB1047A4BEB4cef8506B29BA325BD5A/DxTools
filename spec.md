# DxTools — спецификация и план реализации

Статус: Phase 1 завершён, ожидает подтверждения перехода к Phase 2
Версия документа: 1.0 (2026-11-20)
Лицензия проекта: GNU GPL v2 (`GPL-2.0-only`)
Целевая ОС: Windows 11 (минимум 22H2, сборка 22621), x64; ARM64 — Phase 3

## 1. Продукт и границы

DxTools — утилита управления мультимониторной конфигурацией Windows 11,
функционально повторяющая ключевые возможности DisplayFusion. Реализация
ведётся независимо (clean-room): код, ресурсы и товарные знаки DisplayFusion
не используются.

### 1.1 В scope 1.0

- профили конфигурации мониторов: разрешение, частота, ориентация, primary,
  включение/выключение, авто-применение по набору подключённых мониторов;
- мультимониторные панели задач (вторичные мониторы);
- управление окнами: перенос между мониторами, снап, always-on-top,
  прозрачность, roll-up, maximize across monitors;
- профили позиций окон и правила «Window Location»;
- обои: на каждый монитор, spanning, режимы масштабирования, slideshow;
- глобальные горячие клавиши, включая цепочки и привязку к приложению;
- триггеры (событие → действие) и C#-скрипты;
- сплиты мониторов, titlebar-кнопки;
- установщик MSI, README.md, USER_MANUAL.md.

### 1.2 Вне scope 1.0

- облачная синхронизация настроек, онлайн-каталог обоев, мобильный пульт;
- драйвер ядра, служба Windows;
- Windows 10, macOS, Linux;
- автообновление и телеметрия;
- полный паритет по второстепенным функциям DisplayFusion.

### 1.3 Лицензионная политика

- Поставляемый код: только MIT / BSD / ISC. Apache-2.0 несовместим с GPLv2
  и в поставку не включается (допускается только в тестовых зависимостях).
- Каждая сторонняя зависимость фиксируется в `THIRD-PARTY-NOTICES.md`.
- В MSI и README включается текст GPLv2 и ссылка на исходный код.
- Запрещено копирование ассетов, иконок и кода DisplayFusion.

## 2. Целевая среда и инструменты

| Компонент | Назначение |
|---|---|
| .NET SDK 10.0.400 | managed-код |
| WPF | UI (Per-Monitor DPI v2) |
| C++20 / MSVC | нативные модули |
| Windows 11 SDK 10.0.26100 | Win32 API |
| WiX Toolset 6 (SDK-style) | MSI |
| xUnit | unit-тесты |
| FlaUI | UI-тесты (Phase 2+) |
| GitHub Actions | CI |

Локальная среда Phase 0: Windows 11 26200 x64, .NET SDK 10.0.400,
VS 2026 Community без C++ workload. Нативная сборка локально допускается
через portable Zig; эталонная сборка — MSVC в CI.

## 3. Архитектура

```
DxTools.exe (WPF, asInvoker, single instance, tray)
├─ DxTools.App        UI: трей, настройки, редакторы
├─ DxTools.Core       домен, настройки, профили, команды
├─ DxTools.Engine     сервисы: display, hotkeys, windows, wallpaper, taskbar
├─ DxTools.Interop    P/Invoke, загрузка native DLL
└─ DxTools.Native.dll C++: DisplayConfig, AppBar, hooks, tray, VD

Опционально: DxTools.Helper.exe (UIAccess), ShellExt.dll, DxTools.scr
```

### 3.1 Ключевые решения

- WPF, а не WinUI 3: зрелый Per-Monitor DPI v2, контроль над chrome,
  `HwndHost` для DWM-превью, отсутствие зависимости от WinAppSDK/MSIX.
- C ABI + `LibraryImport`, а не C++/CLI: стабильная граница, нет привязки
  к версии CLR, проще тестировать.
- Один процесс на MVP; нативные хуки — на отдельных потоках.
- Без инъекций в чужие процессы. Всё, что требует инъекций, выносится
  в ограничения или реализуется документированным способом.
- Native-first для рискованных API: DisplayConfig, AppBar, LL-hook,
  виртуальные десктопы, иконки.
- Feature flags и safe mode (Shift/`--safe-mode` отключает хуки).

### 3.2 Структура репозитория

```
/
  spec.md  README.md  USER_MANUAL.md  LICENSE  CHANGELOG.md
  THIRD-PARTY-NOTICES.md  global.json  Directory.Build.props
  DxTools.sln
  src/DxTools.App
  src/DxTools.Core
  src/DxTools.Engine
  src/DxTools.Interop
  native/                    # C++20, CMake
  tests/DxTools.Core.Tests
  installer/DxTools.Installer # WiX 6
  build/                     # build.ps1, generate-wix-fragment.ps1
  docs/                      # architecture, ADR
  .github/workflows/ci.yml
```

## 4. Этапы и ворота (gates)

Каждый этап завершается отчётом и явным подтверждением перехода к следующему.
Код этапа коммитится и публикуется в GitHub до запроса подтверждения.

### Phase 0 — фундамент (1–2 недели)

Статус: завершён, ожидает подтверждения. Отчёт:
`docs/reports/phase-0.md`.

Задачи:

- установка/проверка toolchain (MSVC + Windows SDK + WiX);
- git-репозиторий, лицензия, .gitignore, .editorconfig, central build props;
- solution и проекты Core / Interop / Engine / App / Tests;
- настройки JSON с атомарной записью и восстановлением после повреждения;
- логирование в файл, crash handler, single instance, safe mode;
- трей, окно настроек, RU/EN, темы, автозапуск;
- native DLL с C ABI и версионным handshake;
- MSI: установка, upgrade, удаление, ярлык;
- CI: сборка, тесты, MSI-артефакт.

Выход: устанавливаемый `DxTools-0.1.0-alpha-x64.msi`, зелёные тесты,
исходники в GitHub.

### Phase 1 — MVP 0.2 (завершён)

Статус: завершён, ожидает подтверждения перехода к Phase 2. Отчёт:
`docs/reports/phase-1.md`.

Фактически реализовано:

- модель мониторов, нативный DisplayConfig API v2, watcher `WM_DISPLAYCHANGE`,
  topology hash;
- профили дисплеев: сохранение, применение, подтверждение 15 секунд,
  автоматический откат, авто-применение по набору мониторов;
- горячие клавиши по умолчанию через `RegisterHotKey`:
  `Ctrl+Alt+Left/Right/Up`; LL-hook и редактор — Phase 2;
- перенос активного окна между мониторами с учётом DPI и topmost;
- обои per-monitor/span: композитное полотно, режимы Fill/Fit/Stretch/
  Center/Tile; slideshow — Phase 2;
- backup/restore настроек и профилей в ZIP;
- лицензионный диалог GPLv2 в MSI;
- RU/EN документация и скриншоты.

Критерии приёмки (фактически):

- профиль сохраняется и применяется на 2 мониторах, подтверждение и откат
  проверены вручную;
- перенос окна между мониторами проверен на двух мониторах; mixed DPI
  реализован, но на тестовой машине оба монитора 96 DPI;
- профили и настройки сохраняются в `%AppData%\DxTools`;
- MSI проверен административной установкой; полная установка/удаление
  требуют UAC и остаются ручной проверкой;
- 17 unit-тестов; soak-тест и бюджеты CPU/RAM — Phase 3;
- RU/EN интерфейс и документация.

Отклонения Phase 1: прозрачность/roll-up/span окон, слайдшоу обоев,
кастомизация хоткеев и mixed-DPI лабораторный прогон перенесены в Phase 2.

### Phase 2 — панель задач и автоматизация 0.5-beta (12–16 недель)

- вторичные панели задач: AppBar, кнопки окон, группировка по AUMID,
  pinned apps, часы, hover-превью, auto-hide, DPI;
- зеркалирование системного трея (UIA, после go/no-go);
- titlebar-кнопки;
- профили позиций окон и правила;
- триггеры и C#-скрипты;
- сплиты мониторов;
- CLI/IPC.

### Phase 3 — паритет и hardening 1.0 (10–14 недель)

- виртуальные десктопы (undocumented COM + адаптеры по сборкам);
- multi-monitor screensaver;
- профили иконок рабочего стола;
- переключение аудиоустройств;
- shell context menu;
- UIAccess/elevated helper, активация окон;
- ARM64, HDR, accessibility, локализация, security review;
- подпись кода, финальные README/USER_MANUAL, RC → 1.0.

## 5. Матрица функций

| Модуль | Реализация | Фаза |
|---|---|---|
| Профили дисплеев, режимы, primary, вкл/выкл | C++ DisplayConfig | 1 |
| DPI scaling | C++ (spike) | 2 |
| Сплиты мониторов | C# + C++ | 2 |
| Горячие клавиши | `RegisterHotKey` (1); C++ LL-hook и редактор (2) | 1–2 |
| Move/topmost | C# Win32 | 1 |
| Snap/transparency/roll-up/span | C# Win32 | 2 |
| Профили позиций окон | C# | 2 |
| Titlebar-кнопки | C# overlay | 2 |
| Мультимониторная панель задач | WPF + C++ AppBar | 2 |
| Зеркало трея | C++/UIA | 2 (риск) |
| Обои per-monitor/span | C# + System.Drawing, composite + span | 1 |
| Слайдшоу обоев | C# + таймер | 2 |
| Триггеры | C# | 2 |
| Скрипты | C# Roslyn | 2 |
| Виртуальные десктопы | C++ undocumented | 3 |
| Screensaver | C# | 3 |
| Иконки рабочего стола | C++ | 3 |
| Аудиоустройства | C++ CoreAudio | 3 |
| Shell extension | C++ | 3 |
| UIAccess | C++/manifest | 3 |
| ARM64 | publish + native | 3 |

## 6. Критические спайки

| # | Спайк | Время | Влияние |
|---|---|---|---|
| S1 | AppBar + DWM thumbnails + активация окон | 3 дня | go/no-go для taskbar |
| S2 | Зеркалирование трея через UIA | 2 дня | ограничение или fallback |
| S3 | DisplayConfig apply/rollback + DPI scale | 3 дня | DPI может стать best-effort |
| S4 | LL-hook + elevated-окна | 2 дня | архитектура хелпера |
| S5 | Обои per-monitor/span + Spotlight | 1 день | fallback `SPI_SETDESKWALLPAPER` |
| S6 | Titlebar overlay mixed DPI | 2 дня | ограничения |
| S7 | MSI self-contained + upgrade | 2 дня | выбор WiX/Heat |
| S8 | Virtual Desktop COM | 2 дня | Phase 3 |

Спайки S1–S7 выполняются в Phase 1 до фиксации сроков; S1/S2 — до старта
работ по панели задач.

## 7. Риски

| Риск | Митигация |
|---|---|
| Непубличные API Win11 меняются | адаптеры по сборкам, feature flags, graceful disable |
| Невозможно зеркалировать трей | S2, fallback и документирование ограничения |
| UIPI и активация окон | UIAccess-подпись, elevated helper |
| Ложные срабатывания AV на LL-hook | без инъекций, подпись, отправка в Microsoft |
| SmartScreen | OV/EV сертификат, подпись MSI/exe/dll |
| Конфликт с Snap Layouts | кастомный снап только по границам сплитов |
| GPL-несовместимые зависимости | allowlist и SBOM-аудит в CI |
| Размер self-contained MSI | сжатие WiX, опциональная framework-dependent сборка |
| HDR / mixed GPU / refresh | тест-матрица, WIC color management |

## 8. Качество

- Unit: настройки, профили, парсер CLI, горячие клавиши, геометрия.
- Native: doctest для структур и парсеров.
- UI: FlaUI (Phase 2+).
- Ручная матрица: 1–4 монитора, 100/125/150/200 %, portrait, HDR, RDP,
  док-станция, sleep/resume, elevated-приложения.
- Installer: fresh install, upgrade, repair, silent `/qn`, uninstall.
- Soak 72 ч; handle/GDI/private bytes.
- Бюджеты: старт < 1.5 с, hotkey p95 < 50 мс, idle CPU < 0.5 %,
  RAM < 200 МБ.

## 9. Сборка и релиз

`build/build.ps1 -Configuration Release -Runtime win-x64`:

1. native build (CMake + MSVC; fallback Zig для локальной проверки);
2. `dotnet publish` self-contained;
3. staging, генерация WiX-фрагмента, подпись;
4. MSI (per-machine, x64, Program Files, Start Menu, MajorUpgrade);
5. артефакты: MSI, checksums, SBOM, source tag.

Настройки пользователя: `%AppData%\DxTools`; логи: `%LocalAppData%\DxTools\logs`;
автозапуск: HKCU Run `--autostart`.

## 10. Процесс работы

1. Этап реализуется в отдельной ветке/коммитах.
2. По завершении: тесты, полная сборка MSI, отчёт.
3. Отчёт содержит: что сделано, результаты проверок, известные ограничения,
   ссылку на коммит/репозиторий, запрос подтверждения.
4. Следующий этап начинается только после подтверждения пользователя.
5. `.env` с `GITHUB_TOKEN` не коммитится; токен не попадает в git config,
   логи и артефакты.

## 11. Definition of Done для 1.0

MSI подписан и проверен на чистой VM; upgrade/uninstall; все функции матрицы
проходят приёмку; soak 72 ч; аудит лицензий; accessibility; README и
USER_MANUAL актуальны; исходники и тег доступны; known limitations
задокументированы.
