# Changelog

Все заметные изменения проекта. Формат — Keep a Changelog, версии — SemVer.

## [Unreleased]

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
