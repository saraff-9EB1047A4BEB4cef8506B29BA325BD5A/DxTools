# Архитектура DxTools

Документ описывает архитектуру Phase 3D и границы модулей.

## Процессная модель

```
DxTools.exe (WPF, asInvoker, PerMonitorV2)
├─ Presentation  DxTools.App
├─ Domain        DxTools.Core
├─ Services      DxTools.Engine
├─ Interop       DxTools.Interop
├─ Native        DxTools.Native.dll (C++/Win32)
├─ Shell         DxTools.ShellExt.dll (C++ COM, in-process Explorer)
└─ Helper        DxTools.Helper.exe (C#, uiAccess=true, отдельный процесс)
```

Один экземпляр приложения на сессию пользователя. Повторный запуск
сигнализирует первому экземпляру через named pipe и завершается.

## Слои

### DxTools.App

- WPF-окно настроек, темы, локализация RU/EN.
- Трей через `System.Windows.Forms.NotifyIcon` (временно; позже возможен
  собственный host).
- Single instance, crash handler, безопасный режим.
- `HotkeyService` на native `WH_KEYBOARD_LL` + `HwndSource`; сочетания и
  двухшаговые цепочки хранятся в настройках и меняются через
  `HotkeyCaptureWindow`.
- `DisplayChangeWatcher` на `WM_DISPLAYCHANGE` с debounce.
- `DisplayConfirmationWindow` — подтверждение/откат настроек экрана.
- `TaskbarManager`: панели задач для неосновных мониторов, опционально
  overlay-панель на каждую ячейку сплита (без AppBar), временное скрытие
  `Shell_SecondaryTrayWnd` и восстановление при выходе.
- `TaskbarWindow`: WPF-панель 48 DIP, группировка окон по exe, кнопки с
  иконками и счётчиком, pinned apps, часы, fullscreen-скрытие,
  DWM live-превью в topmost-окне, автоскрытие с управлением AppBar,
  `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`.
- `TaskbarIconCache`: извлечение и кэширование иконок процессов.
- `TrayMirrorService`: UIA-сканирование области уведомлений, снимки иконок
  и передача клика исходному элементу; popup обновляется раз в 2 секунды.
- Никакой бизнес-логики: только представление и композиция.

### DxTools.Core

- `AppSettings`, `SettingsStore`: JSON, версия схемы, атомарная запись,
  восстановление после повреждения.
- `CommandLineOptions`: разбор аргументов.
- `DxLog`: локальные логи без телеметрии.
- `SingleInstance`, `AutostartService`, `SettingsBackupService`.
- `DisplayProfile`, `DisplayProfileStore`, `MonitorInfo`, `WallpaperSettings`.
- `HotkeySetting`, `WindowPlacementProfile`, `WindowPlacementStore`, `PinnedApp`.
- Домен не зависит от WPF.

### DxTools.Engine

- `EngineHost`: native probe и владение сервисами.
- `DisplayService`: перечисление мониторов, capture/apply DisplayConfig,
  topology hash.
- `DisplayProfileService`: save/apply/rollback, auto-apply, store профилей.
- `WindowManager`: перенос активного окна с учётом DPI, topmost;
  операции по целевому HWND для titlebar-overlay; snap в ячейку сетки
  сплитов (`SplitRows`/`SplitColumns`).
- `TitleBarOverlayManager`: topmost overlay активного окна с кнопками
  «следующий монитор»/always-on-top.
- `WallpaperService`: композитное полотно и установка обоев.
- `WindowCatalog`: перечисление top-level окон, фильтрация, монитор,
  активация/сворачивание.
- `WindowPlacementService`: сохранение/восстановление позиций окон.
- `DesktopIconService`: cross-process доступ к `SysListView32` Explorer для
  сохранения/восстановления позиций иконок; `DesktopIconManager` и store.
- `TriggerService`: события, условия, действия и Roslyn-скрипты `.csx`.
- CLI-действия (`--apply-display/window/wallpaper`) выполняются при старте.
- Триггеры выполняются в `Task.Run`; новые окна/процессы отслеживаются
  таймером раз в 2 секунды.
- `AudioService`: CoreAudio enumeration и `IPolicyConfig` для default endpoint.
- `VirtualDesktopService`: навигация/перенос окон через системные Win+Ctrl
  сочетания, синтезированные `SendInput`.
- `DxTools.Screensaver`: отдельный WPF `.scr`, изображения из папки
  слайдшоу, одно окно на монитор.
- CLI-команды и IPC-сообщения `cmd:` выполняются на UI-потоке; команды
  от shell extension применяются к уже запущенному экземпляру.
- В 1.0: UIAccess, подпись, security review.

### DxTools.Interop

- Загрузка `DxTools.Native.dll` из каталога приложения.
- C ABI, версия API, ping/handshake.
- `LibraryImport`, ошибки преобразуются в `NativeProbeResult`.

### DxTools.Native

- C++20, Win32, без сторонних runtime-зависимостей.
- C ABI v4: версия/ ping, DisplayConfig API, LL-hook, prefix-цепочки,
  `DxTools_Audio_GetDeviceCount/GetDevices/SetDefault`.
- DisplayConfig: `QueryDisplayConfig`, `SetDisplayConfig`, DPI, EDID-friendly
  имена, Win32-коды ошибок.
- В следующих фазах: splits, виртуальные десктопы.

### DxTools.ShellExt

- C++20 COM DLL, загружается Explorer в in-process apartment.
- `IShellExtInit` + `IContextMenu`: меню фона рабочего стола/папок и
  изображений; команды запускают `DxTools.exe` с `--show`,
  `--apply-wallpaper` или `--wallpaper <path> --monitor <device>`.
- Регистрация: `HKLM\Software\Classes\...\ContextMenuHandlers\DxTools`,
  `InprocServer32` с `ThreadingModel=Apartment`; компонент MSI 64-bit.
- Ручная регистрация `regsvr32` пишет в `HKCU`; MSI регистрирует per-machine.
- Не содержит бизнес-логики и сетевых операций.

### DxTools.Helper

- Отдельный WinExe с манифестом `asInvoker/uiAccess=true`; запускается
  основным приложением в фоне.
- Named pipe `DxTools.UIAccess.<pid>.<nonce>`; helper проверяет PID клиента
  и путь родительского `DxTools.exe`.
- Команды: `move`, `topmost`, `activate`; только top-level HWND, без запуска
  процессов и сетевых операций.
- `UIAccessClient` в Engine пробует прямой Win32-вызов, при отказе отправляет
  команду helper; при отсутствии/неподписанном helper работает fallback.
- UIAccess активируется Windows только для подписанного helper в Program Files
  и после согласия пользователя.

## Потоки и отказоустойчивость

- UI-поток WPF: представление, команды, горячие клавиши и watcher.
- Apply DisplayConfig и композиция обоев выполняются в `Task.Run`, чтобы
  не блокировать UI; подтверждение отката показывает модальное окно.
- Native LL-hook работает на выделенном потоке с message loop и передаёт
  события в UI через `PostMessage`; prefix-цепочка хранит pending-id с
  таймаутом 1,5 с.
- Titlebar-overlay опрашивает foreground-окно каждые 250 мс, позиционируется
  по DWM extended frame bounds и не активируется.
- Панель задач обновляет окна раз в секунду; auto-hide опрашивает курсор
  каждые 250 мс и снимает/возвращает AppBar; скрытая панель Windows
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
| Профили окон | `%AppData%\DxTools\window-profiles.json` |
| Триггеры | `%AppData%\DxTools\triggers.json` |
| C#-скрипты | `%AppData%\DxTools\scripts\*.csx` |
| Композит обоев | `%LocalAppData%\DxTools\wallpaper\` |
| Логи | `%LocalAppData%\DxTools\logs\DxTools-YYYYMMDD.log` |
| Автозапуск | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| Установка | `%ProgramFiles%\DxTools` |

## Сборка

- Managed: `dotnet publish -r win-x64 --self-contained`.
- Native: CMake + MSVC; локальный fallback — portable Zig.
- MSI: WiX 6, per-machine, x64, сгенерированный фрагмент компонентов.
- CI: GitHub Actions, артефакты MSI и publish.
