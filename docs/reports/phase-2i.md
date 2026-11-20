# Phase 2I — отчёт (ядро)

Дата: 2026-11-20
Статус: **ядро завершено, ожидает подтверждения**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `1c431a7` (`Phase 2I: monitor split grid and snap hotkeys`)
CI: GitHub Actions run `44828328205` — **success**
MSI: `artifacts\DxTools-0.11.0-x64.msi`, 64 805 337 байт
SHA-256: `CBCEE715969FB421030CF8AC28916B639AB9CA79BF2797FD5E5849DDE5BF7A20`

## 1. Что сделано

### Сплиты монитора

- настройки `SplitRows` / `SplitColumns` (1–3) в разделе «Мониторы»;
- `WindowManager.SnapForegroundWindowToSplit` вычисляет ячейку сетки
  текущего монитора и переносит окно;
- учитывается maximized-состояние: окно восстанавливается перед snap;
- hotkeys `Ctrl+Alt+1..4` привязываются к четырём ячейкам;
- проверено: окно 2128×1288 прикреплено к ячейке 2×2 как 1056×660.

### Сохранено из предыдущих фаз

- двухшаговые hotkey-цепочки, titlebar-overlay, LL-hook, панель задач,
  DWM-превью, автоскрытие, pinned apps, профили, триггеры, скрипты, CLI;
- 25 unit-тестов, MSI 0.11.0.

## 2. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build` | success, 0 warnings |
| `dotnet test` | 25/25 passed |
| Split snap | окно 2128×1288 → 1056×660 |
| Log | `Window snapped to split 1/2x2` |
| Hotkeys | 7 bindings, включая split 1–4 |
| MSI administrative install | exit 0, 286 файлов |
| CI | success: MSVC native, 25 тестов, MSI, upload |

## 3. Ограничения и отклонения

1. **Системный трей** не реализован — Phase 2J/3.
2. Отдельные панели задач для сплитов не создаются; сплиты используются
   для сетки snap-hotkeys.
3. Назначены только 4 слота; при 3×3 остальные ячейки недоступны.
4. Snap использует полный rect монитора, не work area.
5. Mixed DPI не проверен; MSI не подписан; полная установка с UAC не
   выполнялась.

## 4. Следующий шаг

Phase 2J/3: системный трей, taskbar для сплитов; затем Phase 3 (виртуальные
десктопы, screensaver, иконки, аудио, UIAccess, ARM64, подпись).

## 5. Запрос подтверждения

Phase 2I (ядро) завершена. Для продолжения требуется подтверждение Phase 2J
или Phase 3. Рекомендуется вручную установить MSI с UAC и проверить split
hotkeys и удаление.
