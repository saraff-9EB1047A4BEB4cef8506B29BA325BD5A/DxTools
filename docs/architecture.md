# Архитектура DxTools

Документ описывает архитектуру Phase 2 и границы модулей.

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
- `HotkeyService` на `RegisterHotKey` + `HwndSource`; по умолчанию
  `Ctrl+Alt+Left/Right/Up`.
- `DisplayChangeWatcher` на `WM_DISPLAYCHANGE` с debounce.
- `DisplayConfirmationWindow` — подтверждение/откат настроек экрана.
- `TaskbarManager`: панели задач для неосновных мониторов, AppBar,
  временное скрытие `Shell_SecondaryTrayWnd` и восстановление при выходе.
- `TaskbarWindow`: WPF-панель 48 DIP, кнопки окон, иконки, часы,
  `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`.
- `TaskbarIconCache`: извлечение и кэширование иконок процессов.
- Никакой бизнес-логики: только представление и композиция.

### DxTools.Core

- `AppSettings`, `SettingsStore`: JSON, версия схемы, атомарная запись,
  восстановление после повреждения.
- `CommandLineOptions`: разбор аргументов.
- `DxLog`: локальные логи без телеметрии.
- `SingleInstance`, `AutostartService`, `SettingsBackupService`.
- `DisplayProfile`, `DisplayProfileStore`, `MonitorInfo`, `WallpaperSettings`.
- Домен не зависит от WPF.

### DxTools.Engine

- `EngineHost`: native probe и владение сервисами.
- `DisplayService`: перечисление мониторов, capture/apply DisplayConfig,
  topology hash.
- `DisplayProfileService`: save/apply/rollback, auto-apply, store профилей.
- `WindowManager`: перенос активного окна с учётом DPI, topmost.
- `WallpaperService`: композитное полотно и установка обоев.
- `WindowCatalog`: перечисление top-level окон, фильтрация, монитор,
  активация/сворачивание.
- В Phase 2B: LL-hook, titlebar, profile rules, triggers, scripts, splits.

### DxTools.Interop

- Загрузка `DxTools.Native.dll` из каталога приложения.
- C ABI, версия API, ping/handshake.
- `LibraryImport`, ошибки преобразуются в `NativeProbeResult`.

### DxTools.Native

- C++20, Win32, без сторонних runtime-зависимостей.
- C ABI v2: версия/ ping, `DxTools_FreeMemory`,
  `DxTools_Display_GetMonitorCount`, `DxTools_Display_GetMonitors`,
  `DxTools_Display_GetState`, `DxTools_Display_ApplyState`.
- DisplayConfig: `QueryDisplayConfig`, `SetDisplayConfig`, DPI, EDID-friendly
  имена, Win32-коды ошибок.
- В следующих фазах: AppBar, LL-hook, DWM thumbnails, виртуальные десктопы.

## Потоки и отказоустойчивость

- UI-поток WPF: представление, команды, горячие клавиши и watcher.
- Apply DisplayConfig и композиция обоев выполняются в `Task.Run`, чтобы
  не блокировать UI; подтверждение отката показывает модальное окно.
- Панель задач обновляет окна раз в секунду; скрытая панель Windows
  восстанавливается при нормальном выходе и в `ProcessExit`.
- Слайдшоу использует `DispatcherTimer` и выбирает следующее изображение
  для каждого монитора.
- Auto-apply подавляется на 4 секунды после изменения, чтобы избежать
  рекурсии от `WM_DISPLAYCHANGE`.
- Исключения логируются; необработанные исключения UI показывают сообщение
  и не завершают приложение молча.

## Хранение данных

| Данные | Путь |
|---|---|
| Настройки | `%AppData%\DxTools\settings.json` |
| Профили дисплеев | `%AppData%\DxTools\display-profiles.json` |
| Композит обоев | `%LocalAppData%\DxTools\wallpaper\` |
| Логи | `%LocalAppData%\DxTools\logs\DxTools-YYYYMMDD.log` |
| Автозапуск | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| Установка | `%ProgramFiles%\DxTools` |

## Сборка

- Managed: `dotnet publish -r win-x64 --self-contained`.
- Native: CMake + MSVC; локальный fallback — portable Zig.
- MSI: WiX 6, per-machine, x64, сгенерированный фрагмент компонентов.
- CI: GitHub Actions, артефакты MSI и publish.
