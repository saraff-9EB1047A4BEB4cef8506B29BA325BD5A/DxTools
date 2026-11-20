# Архитектура DxTools

Документ описывает целевую архитектуру Phase 0 и границы модулей.

## Процессная модель

```
DxTools.exe (WPF, asInvoker, PerMonitorV2)
├─ Presentation  DxTools.App
├─ Domain        DxTools.Core
├─ Services      DxTools.Engine
├─ Interop       DxTools.Interop
└─ Native        DxTools.Native.dll (C++/Win32)
```

Один экземпляр приложения на сессию пользователя. Повторный запуск
сигнализирует первому экземпляру через named pipe и завершается.

## Слои

### DxTools.App

- WPF-окно настроек, темы, локализация RU/EN.
- Трей через `System.Windows.Forms.NotifyIcon` (временно; позже возможен
  собственный host).
- Single instance, crash handler, безопасный режим.
- Никакой бизнес-логики: только представление и композиция.

### DxTools.Core

- `AppSettings`, `SettingsStore`: JSON, версия схемы, атомарная запись,
  восстановление после повреждения.
- `CommandLineOptions`: разбор аргументов.
- `DxLog`: локальные логи без телеметрии.
- `SingleInstance`, `AutostartService`.
- Домен не зависит от WPF.

### DxTools.Engine

- Hosting фоновых сервисов. В Phase 0 — `EngineHost` и проверка native.
- В Phase 1+: display, hotkey, window, wallpaper, taskbar services.

### DxTools.Interop

- Загрузка `DxTools.Native.dll` из каталога приложения.
- C ABI, версия API, ping/handshake.
- `LibraryImport`, ошибки преобразуются в `NativeProbeResult`.

### DxTools.Native

- C++20, Win32, без сторонних runtime-зависимостей.
- C ABI: `DxTools_GetApiVersion`, `DxTools_GetVersion`, `DxTools_Ping`.
- В следующих фазах: DisplayConfig, AppBar, hooks, DWM thumbnails,
  виртуальные десктопы.

## Потоки и отказоустойчивость

- UI-поток WPF: только представление и команды.
- Native probe выполняется синхронно при старте (быстро) и по кнопке.
- В Phase 1 хуки и watchers будут работать на выделенных потоках с
  message loop; UI не блокируется.
- Исключения логируются; необработанные исключения UI показывают сообщение
  и не завершают приложение молча.

## Хранение данных

| Данные | Путь |
|---|---|
| Настройки | `%AppData%\DxTools\settings.json` |
| Логи | `%LocalAppData%\DxTools\logs\DxTools-YYYYMMDD.log` |
| Автозапуск | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| Установка | `%ProgramFiles%\DxTools` |

## Сборка

- Managed: `dotnet publish -r win-x64 --self-contained`.
- Native: CMake + MSVC; локальный fallback — portable Zig.
- MSI: WiX 6, per-machine, x64, сгенерированный фрагмент компонентов.
- CI: GitHub Actions, артефакты MSI и publish.
