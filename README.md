# DxTools

DxTools — открытая утилита для Windows 11, повторяющая ключевые возможности
DisplayFusion: профили мониторов, мультимониторные панели задач, управление
окнами, обои, горячие клавиши, триггеры и скрипты.

- Лицензия: GNU GPL v2 (`GPL-2.0-only`)
- Платформа: Windows 11 22H2+ (x64; ARM64 — cross-build), .NET 10
- Статус: 1.0.0-rc.8 — review решения выполнен, GitHub Release опубликован

![DxTools, редактор триггеров](docs/images/phase2g-titlebar.png)

![DxTools, профили окон](docs/images/phase2b-windows.png)

![DxTools, профили мониторов, русский интерфейс](docs/images/phase1-monitors.png)

## Возможности текущей версии (1.0.0-rc.8)

- установщик MSI (per-machine, x64 и ARM64) с лицензионным диалогом GPLv2;
- приложение в системном трее, окно настроек;
- профили мониторов: сохранение текущей конфигурации, применение,
  подтверждение с автоматическим откатом, авто-применение по набору мониторов;
- панель задач на дополнительных мониторах: AppBar, группировка окон по
  приложению, кнопки с иконками, активация/сворачивание, часы, замена панели
  задач Windows, скрытие для fullscreen, закреплённые приложения,
  DWM live-превью при наведении, автоскрытие;
- профили позиций окон: сохранение, применение и удаление;
- перенос активного окна между мониторами с учётом DPI и always-on-top;
- UIAccess helper для перемещения, закрепления и активации elevated-окон
  (активируется только при подписанном `DxTools.Helper.exe`);
- настраиваемые горячие клавиши через низкоуровневый keyboard hook,
  включая сочетания с Win, цепочки до четырёх шагов и привязку к приложению,
  с диалогом захвата;
- CLI-применение профилей дисплеев/окон и обоев;
- триггеры: запуск, смена дисплея, создание окна/процесса → профиль,
  обои, программа или C#-скрипт;
- кнопки в заголовке активного окна в стиле системных caption-кнопок:
  перенос на следующий монитор и always-on-top, зелёный (lime) hover;
- сплиты монитора: сетка до 3×3 и snap hotkeys `Ctrl+Alt+1..4`;
- зеркало системного трея в popup на панели задач с live-обновлением и
  контекстными меню иконок по правому клику;
- отдельные overlay-панели задач для каждой ячейки сплита;
- контекстные меню кнопок панели задач: перенос окна на любой монитор
  (панели DxTools и Ctrl+ПКМ на основной панели Windows);
- профили иконок рабочего стола (save/apply/delete);
- переключение аудиоустройства по умолчанию;
- multi-monitor screensaver `DxTools.scr` с изображениями и видео из папки
  слайдшоу, включая предпросмотр в параметрах экрана блокировки;
- hotkeys навигации и переноса окон между виртуальными рабочими столами;
- контекстное меню Explorer: настройки, применение обоев и установка
  выбранного изображения обоями монитора под курсором;
- ARM64 cross-build, `DxTools-1.0.0-arm64.msi` и проверка административной
  установкой;
- SECURITY.md, `docs/signing.md` и скрипт подписи `build/sign.ps1`;
- обои на каждый монитор: композитное полотно, режимы Fill/Fit/Stretch/
  Center/Tile;
- слайдшоу обоев из папки с настраиваемым интервалом;
- `--exit` для корректного завершения работающего экземпляра;
- экспорт и импорт резервной копии настроек и профилей;
- JSON-настройки с атомарной записью и восстановлением после повреждения;
- автозапуск при входе в систему;
- русский и английский интерфейс (переключение языка — после перезапуска);
- светлая/тёмная/системная тема;
- единственный экземпляр приложения, команда открытия настроек из второй копии;
- файловые логи и crash handler;
- безопасный режим (`--safe-mode`);
- нативная библиотека `DxTools.Native.dll` с C ABI v6, DisplayConfig, LL-hook и handshake.

UIAccess требует подписанного сертификатом `DxTools.Helper.exe`, установленного
в Program Files, и согласия пользователя при первом запуске. Без подписи
приложение работает в обычном режиме. Подпись сертификатом и runtime-тест ARM64
отложены по бюджету; security review, soak-тест и UAC-проверки считаются
принятыми. Подробный план:
[spec.md](spec.md), политика безопасности: [SECURITY.md](SECURITY.md),
security review: [docs/security-review.md](docs/security-review.md),
release checklist: [docs/release-checklist.md](docs/release-checklist.md).

## Требования для сборки

- Windows 11 x64;
- .NET SDK 10.0.400+;
- Visual Studio 2022/2026 с workload «Desktop development with C++» и
  Windows 11 SDK 10.0.26100;
- CMake 3.28+ (для нативной библиотеки);
- WiX Toolset 6 восстанавливается как NuGet-пакет.

## Сборка

```powershell
dotnet build DxTools.sln -c Release
dotnet test DxTools.sln -c Release
powershell -ExecutionPolicy Bypass -File build/build.ps1 -Configuration Release
```

Результат: `artifacts/publish/` (self-contained приложение) и
`artifacts/DxTools-1.0.0-x64.msi`.

Локальная сборка нативной библиотеки без MSVC возможна через portable Zig
(см. `build/build.ps1`), но эталонной считается сборка MSVC в CI.

## Структура

```
src/DxTools.App        WPF UI, трей, настройки
src/DxTools.Core       домен, настройки, утилиты
src/DxTools.Engine     фоновые сервисы
src/DxTools.Interop    P/Invoke и загрузка native DLL
native/                C++ библиотека
tests/                 unit-тесты
installer/             WiX 6 MSI
build/                 сборочные скрипты
```

## Документация

- [spec.md](spec.md) — спецификация и план;
- [docs/reports/phase-0.md](docs/reports/phase-0.md) — отчёт Phase 0;
- [docs/reports/phase-1.md](docs/reports/phase-1.md) — отчёт Phase 1;
- [docs/reports/phase-2.md](docs/reports/phase-2.md) — отчёт Phase 2;
- [docs/reports/phase-2b.md](docs/reports/phase-2b.md) — отчёт Phase 2B;
- [docs/reports/phase-2c.md](docs/reports/phase-2c.md) — отчёт Phase 2C;
- [docs/reports/phase-2d.md](docs/reports/phase-2d.md) — отчёт Phase 2D;
- [docs/reports/phase-2e.md](docs/reports/phase-2e.md) — отчёт Phase 2E;
- [docs/reports/phase-2f.md](docs/reports/phase-2f.md) — отчёт Phase 2F;
- [docs/reports/phase-2g.md](docs/reports/phase-2g.md) — отчёт Phase 2G;
- [docs/reports/phase-2h.md](docs/reports/phase-2h.md) — отчёт Phase 2H;
- [docs/reports/phase-2i.md](docs/reports/phase-2i.md) — отчёт Phase 2I;
- [docs/reports/phase-2j.md](docs/reports/phase-2j.md) — отчёт Phase 2J;
- [docs/reports/phase-2k.md](docs/reports/phase-2k.md) — отчёт Phase 2K;
- [docs/reports/phase-3a.md](docs/reports/phase-3a.md) — отчёт Phase 3A;
- [docs/reports/phase-3b.md](docs/reports/phase-3b.md) — отчёт Phase 3B;
- [docs/reports/phase-3c.md](docs/reports/phase-3c.md) — отчёт Phase 3C;
- [docs/reports/phase-3d.md](docs/reports/phase-3d.md) — отчёт Phase 3D;
- [docs/reports/rc-1.0.0-rc.8.md](docs/reports/rc-1.0.0-rc.8.md) — отчёт RC 1.0.0-rc.8;
- [docs/reports/rc-1.0.0-rc.7.md](docs/reports/rc-1.0.0-rc.7.md) — отчёт RC 1.0.0-rc.7;
- [docs/reports/rc-1.0.0-rc.6.md](docs/reports/rc-1.0.0-rc.6.md) — отчёт RC 1.0.0-rc.6;
- [docs/reports/rc-1.0.0-rc.5.md](docs/reports/rc-1.0.0-rc.5.md) — отчёт RC 1.0.0-rc.5;
- [docs/reports/rc-1.0.0-rc.4.md](docs/reports/rc-1.0.0-rc.4.md) — отчёт RC 1.0.0-rc.4;
- [docs/reports/rc-1.0.0-rc.3.md](docs/reports/rc-1.0.0-rc.3.md) — отчёт RC 1.0.0-rc.3;
- [docs/reports/rc-1.0.0-rc.2.md](docs/reports/rc-1.0.0-rc.2.md) — отчёт RC 1.0.0-rc.2;
- [docs/reports/rc-1.0.0.md](docs/reports/rc-1.0.0.md) — отчёт RC 1.0.0-rc.1;
- [USER_MANUAL.md](USER_MANUAL.md) — руководство пользователя;
- [SECURITY.md](SECURITY.md) — политика безопасности;
- [docs/security-review.md](docs/security-review.md) — security review;
- [docs/release-checklist.md](docs/release-checklist.md) — release checklist;
- [docs/arm64.md](docs/arm64.md) — ARM64 build;
- [docs/signing.md](docs/signing.md) — подпись и UIAccess;
- [CHANGELOG.md](CHANGELOG.md) — история версий;
- [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) — сторонние компоненты.

## Участие

Проект распространяется по GPLv2. Отправляя изменения, вы соглашаетесь
лицензировать их на тех же условиях. Не добавляйте код и ресурсы,
несовместимые с GPLv2.

## Лицензия

Copyright (C) 2026 DxTools contributors.

Программа распространяется в надежде, что она будет полезной, но БЕЗ
КАКИХ-ЛИБО ГАРАНТИЙ. Полный текст — в [LICENSE](LICENSE).
