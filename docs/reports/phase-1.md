# Phase 1 — отчёт

Дата: 2026-11-20
Статус: **завершён, ожидает подтверждения перехода к Phase 2**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит кода: `d5a88a8` (`fix: MSVC-safe native copy; test backup roundtrip`)
Основной коммит этапа: `8edcb40` (`Phase 1: monitor profiles, hotkeys, window move, wallpaper, backup, MSI UI`)
CI: GitHub Actions run `44803263819` — **success**
MSI: `artifacts\DxTools-0.2.0-x64.msi`, 61 437 396 байт,
SHA-256 `A6F946852180E4AECBBDDB497FA035DEB78A3451180FAA7BFB7555F224B7A2B9`

## 1. Что сделано

### Native DisplayConfig API v2

- перечисление мониторов: GDI-имя, EDID-путь, дружественное имя, rect,
  primary, DPI, refresh rate, bpp;
- capture/apply активной конфигурации через `QueryDisplayConfig` и
  `SetDisplayConfig`;
- стабильный бинарный формат состояния и fallback набора флагов применения;
- C ABI v2, Win32-коды ошибок, освобождение буферов;
- MSVC-сборка в CI и локальная Zig-сборка.

### Профили дисплеев

- вкладка «Мониторы» с текущей конфигурацией и topology hash;
- сохранение профиля с именем и авто-применением;
- применение профиля с 15-секундным подтверждением;
- автоматический откат при отказе от подтверждения или ошибке;
- store `%AppData%\DxTools\display-profiles.json` с атомарной записью и
  восстановлением после повреждения;
- watcher `WM_DISPLAYCHANGE` с debounce и auto-apply по совпадению набора
  мониторов;
- защита от рекурсии auto-apply (подавление на 4 секунды).

### Окна и горячие клавиши

- `Ctrl+Alt+Left` / `Ctrl+Alt+Right` — перенос активного окна между
  мониторами с сохранением относительного положения и учётом DPI;
- `Ctrl+Alt+Up` — always-on-top;
- пропуск собственного процесса, shell и невалидных окон;
- hotkey-сервис на `RegisterHotKey` + `HwndSource`;
- вкладка «Горячие клавиши» показывает сочетание, описание и доступность.

### Обои

- вкладка «Обои» с изображением для каждого монитора;
- композитное полотно размером с виртуальный рабочий стол;
- режимы Fill/Fit/Stretch/Center/Tile;
- установка wallpaper style span через `SystemParametersInfo`;
- сохранение путей в `settings.json`;
- очистка старых композитов (последние 5).

### Backup и установщик

- экспорт/импорт ZIP с настройками и профилями;
- после импорта текущая сессия не перезаписывает импортированные файлы;
- MSI с `WixUI_InstallDir`, лицензионным диалогом GPLv2;
- в поставку включены `LICENSE` и `THIRD-PARTY-NOTICES.md`;
- версия 0.2.0, native DLL 0.2.0.

### Тесты и документация

- 18 unit-тестов: настройки, CLI, профили, topology hash, wallpaper,
  backup roundtrip, native probe;
- README, USER_MANUAL, spec, architecture, CHANGELOG обновлены;
- скриншоты RU/dark: General, Monitors, Wallpaper.

## 2. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build DxTools.sln -c Release` | success, 0 warnings |
| `dotnet test` | 18/18 passed |
| Native API | api=2, version=0.2.0, ping OK |
| Мониторы | 2 обнаружены (1920×1080, 2112×1320) |
| Capture/apply DisplayConfig | capture 556 байт, apply rc=0 |
| Профиль через UI | сохранён, применён, подтверждение «Оставить» |
| Hotkeys | все 3 зарегистрированы |
| Перенос окна | log: source 2112×1320 → target 1920×1080 |
| Обои | композит 4032×1320, применён |
| Backup | ZIP export/import roundtrip test |
| MSI administrative install | exit 0, 278 файлов |
| MSI payload | exe, native, LICENSE, THIRD-PARTY, ru resources |
| CI | MSVC native, тесты, MSI, upload — success |
| UI | RU/dark и EN/light скриншоты |

## 3. Ограничения и отклонения

1. **Слайдшоу обоев не реализовано.** Есть статические обои на каждый
   монитор и композит; папка/интервал — Phase 2.
2. **Горячие клавиши фиксированы.** `RegisterHotKey` и три сочетания;
   LL-hook, цепочки и редактор — Phase 2.
3. **Прозрачность, roll-up, snap, span окон** — Phase 2.
4. **Mixed DPI не проверен на железе.** Логика масштабирования реализована,
   но тестовая машина имеет два монитора с 96 DPI.
5. **Auto-apply не проверен с физическим отключением монитора.** Watcher и
   сервис реализованы; нужен тест на hotplug.
6. **Полная установка/удаление MSI с UAC не выполнена** в этой сессии.
   MSI проверен административной установкой; лицензионный UI собран, но
   визуально не проверялся.
7. **MSI не подписан.** SmartScreen может предупреждать; подпись — Phase 3.
8. **Elevated-окна** могут не перемещаться без UIAccess; Phase 3.
9. **Нет soak/CPU/RAM бюджета** — Phase 3.
10. **Windows может сбросить обои** при смене темы/Spotlight; повторное
    применение доступно вручную.

## 4. Следующий этап — Phase 2

- вторичные панели задач: AppBar, кнопки окон, группировка, pinned apps,
  часы, DWM-превью, auto-hide;
- зеркалирование трея (UIA, go/no-go);
- titlebar-кнопки;
- профили позиций окон и правила;
- слайдшоу обоев;
- LL-hook, цепочки и редактор горячих клавиш;
- триггеры и C#-скрипты;
- сплиты мониторов, CLI/IPC.

## 5. Запрос подтверждения

Phase 1 завершён. Для перехода к Phase 2 требуется подтверждение
пользователя. Рекомендуется перед подтверждением вручную запустить
`DxTools-0.2.0-x64.msi` с UAC и проверить установку, лицензионный диалог,
применение профиля и удаление.
