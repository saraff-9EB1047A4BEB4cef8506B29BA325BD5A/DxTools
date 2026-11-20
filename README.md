# DxTools

DxTools — открытая утилита для Windows 11, повторяющая ключевые возможности
DisplayFusion: профили мониторов, мультимониторные панели задач, управление
окнами, обои, горячие клавиши, триггеры и скрипты.

- Лицензия: GNU GPL v2 (`GPL-2.0-only`)
- Платформа: Windows 11 22H2+ (x64), .NET 10
- Статус: Phase 1 завершён, ожидает подтверждения перехода к Phase 2

![DxTools, профили мониторов, русский интерфейс](docs/images/phase1-monitors.png)

## Возможности текущей версии (0.2.0-alpha)

- установщик MSI (per-machine, x64) с лицензионным диалогом GPLv2;
- приложение в системном трее, окно настроек;
- профили мониторов: сохранение текущей конфигурации, применение,
  подтверждение с автоматическим откатом, авто-применение по набору мониторов;
- перенос активного окна между мониторами с учётом DPI и always-on-top;
- глобальные горячие клавиши `Ctrl+Alt+Left/Right/Up`;
- обои на каждый монитор: композитное полотно, режимы Fill/Fit/Stretch/
  Center/Tile;
- экспорт и импорт резервной копии настроек и профилей;
- JSON-настройки с атомарной записью и восстановлением после повреждения;
- автозапуск при входе в систему;
- русский и английский интерфейс (переключение языка — после перезапуска);
- светлая/тёмная/системная тема;
- единственный экземпляр приложения, команда открытия настроек из второй копии;
- файловые логи и crash handler;
- безопасный режим (`--safe-mode`);
- нативная библиотека `DxTools.Native.dll` с C ABI v2, DisplayConfig и handshake.

Панель задач, сплиты мониторов, триггеры, скрипты и кастомизация горячих
клавиш — Phase 2. Подробный план: [spec.md](spec.md).

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
`artifacts/DxTools-0.2.0-x64.msi`.

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
- [USER_MANUAL.md](USER_MANUAL.md) — руководство пользователя;
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
