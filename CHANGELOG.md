# Changelog

Все заметные изменения проекта. Формат — Keep a Changelog, версии — SemVer.

## [Unreleased]

### Added

- меню кнопки Dx на панелях задач дополнительных мониторов: список мониторов,
  раскрывающийся в окна этого монитора, с командами активации, переноса на любой
  монитор, возврата на текущий монитор и переключения «поверх всех окон»;
  внизу меню — команда открытия настроек DxTools.

### Fixed

- кнопки «Обновить» на вкладках «Аудио» и «Рабочий стол» прижаты к правому краю,
  как на остальных вкладках;
- кнопки titlebar-overlay больше не показывают прямоугольники вместо глифов
  (векторные иконки вместо символов PUA-шрифта) и подсвечиваются сразу при
  наведении, без задержки;
- окно настроек надёжно выводится на передний план при вызове из панели задач.

## [1.0.0-rc.7] - 2026-11-21

### Fixed

- подсказки кнопок панели задач, трея и titlebar-overlay больше не теряют текст
  в тёмной теме: добавлен тематический стиль `ToolTip` с фоном `SurfaceAltBrush`
  и рамкой `BorderBrush`;
- для закреплённых приложений без окон подсказка показывает имя приложения,
  а не пустую строку.

## [1.0.0-rc.6] - 2026-11-21

### Added

- контекстное меню кнопок панелей задач DxTools (вторичные мониторы и сплиты):
  перенос окна на любой монитор, возврат на текущий монитор, выбор окна в группе;
- команда трея «Перенести активное окно на монитор»;
- опциональная интеграция с основной панелью задач Windows: Ctrl+правый клик
  по кнопке открывает меню DxTools (UI Automation + LL-mouse-hook, без инъекций);
- настройки `EnableTaskbarContextMenu` и `EnablePrimaryTaskbarMenu`;
- C ABI v5: `DxTools_MouseHook_*`;
- 6 unit-тестов для сопоставления окон и настроек (всего 41).

### Changed

- `WindowManager` получил перенос окна на заданный монитор
  (`MoveWindowToMonitor`) с сохранением DPI/topmost/maximized;
- обновлены spec, architecture, USER_MANUAL и отчёт rc.6.

## [1.0.0-rc.5] - 2026-11-21

### Changed

- кнопки titlebar-overlay оформлены как стандартные caption-кнопки Windows
  (46×32, Segoe Fluent Icons, без рамки и скруглений);
- при наведении и нажатии используется зелёный (lime) фон;
- цвет глифов подстраивается под светлую/тёмную тему целевого окна;
- overlay выровнен вплотную к системным кнопкам с учётом DPI.

## [1.0.0-rc.4] - 2026-11-21

### Added

- UIAccess helper `DxTools.Helper.exe` с манифестом `uiAccess="true"`;
- маршрутизация move/snap/topmost/activate и профилей окон через helper
  при отказе прямого Win32-вызова;
- локальный named-pipe протокол с проверкой PID и пути родительского
  `DxTools.exe`;
- опциональная подпись helper/app/native/shell extension/MSI в `build.ps1`.

### Changed

- версия приложения 1.0.0-rc.4; helper включён в MSI x64/ARM64;
- обновлены README, USER_MANUAL, spec, architecture, security review,
  signing и release checklist.

## [1.0.0-rc.3] - 2026-11-21

### Added

- shell extension `DxTools.ShellExt.dll`: контекстное меню рабочего стола,
  папок и изображений;
- CLI `--wallpaper <path>` и `--monitor <device>`;
- IPC-передача команд запущенному экземпляру;
- 8 новых unit-тестов (всего 35).

### Changed

- версия приложения 1.0.0-rc.3; shell extension включён в MSI x64/ARM64;
- README, USER_MANUAL, spec, architecture и security review обновлены.

## [1.0.0-rc.2] - 2026-11-21

### Added

- ARM64 cross-build: native DLL, self-contained publish и
  `DxTools-1.0.0-arm64.msi`;
- проверка ARM64 MSI административной установкой;
- docs/arm64.md и docs/signing.md.

### Changed

- версия приложения 1.0.0-rc.2; x64 и ARM64 MSI;
- README, USER_MANUAL, spec и release checklist обновлены для rc.2.

## [1.0.0-rc.1] - 2026-11-20

### Added

- release candidate: screensaver, audio, desktop icons, split taskbars,
  tray mirror, VD hotkeys;
- `SECURITY.md`, `docs/security-review.md`, `docs/release-checklist.md`,
  `build/sign.ps1`;
- MSI 1.0.0, self-contained publish;
- 27 unit-тестов.

## [0.17.0-alpha.1] - 2026-11-20

### Added

- hotkeys навигации и переноса окон между виртуальными рабочими столами;
- `VirtualDesktopService` через системные сочетания и `SendInput`;
- `SECURITY.md`;
- `build/sign.ps1` для Authenticode-подписи;
- 27 unit-тестов.

### Changed

- версия приложения 0.17.0-alpha.1; native DLL 0.6.0, API v4;
- MSI 0.17.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 3D.

## [0.16.0-alpha.1] - 2026-11-20

### Added

- проект `DxTools.Screensaver` и `.scr` в MSI;
- multi-monitor screensaver с изображениями из папки слайдшоу;
- режимы `/s`, `/c`, `/p`;
- 27 unit-тестов.

### Changed

- версия приложения 0.16.0-alpha.1; native DLL 0.6.0, API v4;
- MSI 0.16.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 3C.

## [0.15.0-alpha.1] - 2026-11-20

### Added

- native API v4: перечисление аудиоустройств и смена default endpoint;
- CoreAudio + `IPolicyConfig`;
- вкладка «Аудио» с кнопкой «Сделать основным»;
- 27 unit-тестов.

### Changed

- версия приложения 0.15.0-alpha.1; native DLL 0.6.0, API v4;
- MSI 0.15.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 3B.

## [0.14.0-alpha.1] - 2026-11-20

### Added

- профили позиций иконок рабочего стола (save/apply/delete);
- cross-process доступ к `SysListView32` Explorer;
- вкладка «Рабочий стол»;
- 2 новых unit-теста (всего 27).

### Changed

- версия приложения 0.14.0-alpha.1; native DLL 0.5.0, API v3;
- MSI 0.14.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 3A.

## [0.13.0-alpha.1] - 2026-11-20

### Added

- отдельная overlay-панель задач для каждой ячейки сплита;
- фильтрация окон по bounds ячейки;
- live-обновление popup трея раз в 2 секунды;
- 25 unit-тестов.

### Changed

- версия приложения 0.13.0-alpha.1; native DLL 0.5.0, API v3;
- MSI 0.13.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 2K.

## [0.12.0-alpha.1] - 2026-11-20

### Added

- кнопка трея и popup-зеркало области уведомлений Windows;
- UIA-сканирование иконок, снимки и передача клика исходному элементу;
- 25 unit-тестов.

### Changed

- версия приложения 0.12.0-alpha.1; native DLL 0.5.0, API v3;
- MSI 0.12.0;
- обновлены README, USER_MANUAL, spec, architecture, отчёт Phase 2J.

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
