# Changelog

Все заметные изменения проекта. Формат — Keep a Changelog, версии — SemVer.

## [Unreleased]

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
- 6 новых unit-тестов (всего 17).

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
