# Changelog

Все заметные изменения проекта. Формат — Keep a Changelog, версии — SemVer.

## [Unreleased]

## [0.11.0-alpha.1] - 2026-11-20

### Added

- сплиты монитора: настройки rows/columns (1–3) и snap hotkeys
  `Ctrl+Alt+1..4`;
- WindowManager.SnapForegroundWindowToSplit с учётом монитора и DPI;
- 25 unit-тестов.

### Changed

- версия приложения 0.11.0-alpha.1; native DLL 0.5.0, API v3;
- MSI 0.11.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 2I.

## [0.10.0-alpha.1] - 2026-11-20

### Added

- двухшаговые цепочки hotkeys (prefix + main, таймаут 1,5 секунды);
- поля prefix в настройках и native binding, диалог захвата префикса;
- кнопки «Префикс»/«Сбросить префикс» и отображение цепочки в UI.

### Changed

- версия приложения 0.10.0-alpha.1; native DLL 0.5.0, API v3;
- MSI 0.10.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 2H.

## [0.9.0-alpha.1] - 2026-11-20

### Added

- titlebar-overlay активного окна с кнопками «следующий монитор» и
  always-on-top;
- настройка «Кнопки в заголовке окон»;
- позиционирование по DWM extended frame bounds и DPI;
- 25 unit-тестов (без новых тестов overlay).

### Changed

- версия приложения 0.9.0-alpha.1; native DLL 0.4.0, API v3;
- MSI 0.9.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 2G.

## [0.8.0-alpha.1] - 2026-11-20

### Added

- нативный `WH_KEYBOARD_LL` hook (API v3) с отдельным потоком и подавлением
  совпавших комбинаций;
- поддержка сочетаний с Win;
- native `Hotkey_Start/Stop/SetBindings` и managed `HotkeyService`.

### Changed

- версия приложения 0.8.0-alpha.1; native DLL 0.4.0, API v3;
- MSI 0.8.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 2F.

## [0.7.0-alpha.1] - 2026-11-20

### Added

- вкладка «Триггеры»: startup, display change, window created, process started;
- действия: display profile, window profile, wallpaper, run program, C# script;
- C#-скрипты `.csx` с API `Monitors`, `ApplyDisplayProfile`,
  `ApplyWindowProfile`, `ApplyWallpaper`, `Run`, `Log`;
- 2 новых unit-теста (всего 25).

### Changed

- версия приложения 0.7.0-alpha.1; native DLL 0.3.0, API v2;
- добавлена зависимость Microsoft.CodeAnalysis.CSharp.Scripting (MIT);
- MSI 0.7.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 2E.

## [0.6.0-alpha.1] - 2026-11-20

### Added

- DWM live-превью активного окна группы при наведении на панель задач;
- автоскрытие панели задач с показом у нижнего края и управлением AppBar;
- настройка автоскрытия без перезапуска.

### Changed

- версия приложения 0.6.0-alpha.1; native DLL 0.3.0, API v2;
- MSI 0.6.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 2D.

## [0.5.0-alpha.1] - 2026-11-20

### Added

- закреплённые приложения на панели задач (добавление/удаление, запуск);
- CLI-применение профилей дисплеев/окон и обоев;
- 2 новых unit-теста (всего 23).

### Changed

- версия приложения 0.5.0-alpha.1; native DLL 0.3.0, API v2;
- MSI 0.5.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 2C.

## [0.4.0-alpha.1] - 2026-11-20

### Added

- группировка окон на панели задач по исполняемому файлу, переключение
  окон в группе, счётчик окон;
- автоматическое скрытие панели задач для fullscreen-приложений;
- профили позиций окон: сохранение, применение, удаление;
- редактор горячих клавиш с диалогом захвата сочетания и перерегистрацией
  через `RegisterHotKey`;
- 2 новых unit-теста (всего 21).

### Changed

- версия приложения 0.4.0-alpha.1; native DLL 0.3.0, API v2;
- MSI 0.4.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 2B.

## [0.3.0-alpha.1] - 2026-11-20

### Added

- панель задач на неосновных мониторах: AppBar, работа без перекрытия
  развёрнутых окон, кнопки окон с иконками, активация и сворачивание,
  часы, горизонтальная прокрутка;
- замена `Shell_SecondaryTrayWnd` на время работы и восстановление панели
  задач Windows при выходе;
- настройка «Панель задач на дополнительных мониторах» с переключением
  без перезапуска;
- слайдшоу обоев из папки с настраиваемым интервалом;
- CLI/IPC-команда `--exit` для корректного завершения существующего
  экземпляра;
- 1 новый unit-тест (всего 19).

### Changed

- версия приложения 0.3.0-alpha.1; native DLL 0.3.0, API v2;
- MSI 0.3.0 с обновлёнными ресурсами и лицензионным диалогом;
- README, USER_MANUAL, spec, architecture и скриншоты Phase 2.

## [0.2.0-alpha.1] - 2026-11-20

### Added

- native DisplayConfig API v2: перечисление мониторов, capture/apply состояния,
  DPI и EDID-имена;
- вкладка «Мониторы»: текущая конфигурация, topology hash, сохранение,
  применение и удаление профилей;
- подтверждение применения настроек экрана с 15-секундным таймером и откатом;
- авто-применение профиля при подключении мониторов;
- горячие клавиши `Ctrl+Alt+Left/Right/Up` для переноса окна и topmost;
- перенос активного окна между мониторами с учётом DPI;
- вкладка «Обои»: изображение на каждый монитор, композитное полотно,
  режимы Fill/Fit/Stretch/Center/Tile;
- экспорт и импорт настроек и профилей в ZIP;
- лицензионный диалог GPLv2 в MSI, файлы LICENSE и THIRD-PARTY-NOTICES
  в поставке;
- 7 новых unit-тестов (всего 18).

### Changed

- версия приложения 0.2.0-alpha.1; native DLL 0.2.0, API v2;
- MSI 0.2.0, WixUI_InstallDir;
- статус Phase 1 в документации и скриншоты.

## [0.1.0-alpha.1] - 2026-11-20

### Added

- решение .NET 10: Core, Interop, Engine, WPF App, xUnit tests;
- установщик WiX 6 (per-machine, x64);
- настройки JSON с атомарной записью, резервным восстановлением и версией схемы;
- файловое логирование, crash handler, single instance через named pipe;
- трей, окно настроек, темы, RU/EN;
- автозапуск через HKCU Run;
- безопасный режим `--safe-mode`;
- нативная библиотека `DxTools.Native.dll` с C ABI v1 и handshake;
- CI workflow сборки, тестов и MSI.
