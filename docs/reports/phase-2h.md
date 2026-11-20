# Phase 2H — отчёт (ядро)

Дата: 2026-11-20
Статус: **ядро завершено, ожидает подтверждения**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `03e1c1c` (`Phase 2H: two-step hotkey chains`)
CI: GitHub Actions run `44826491221` — **success**
MSI: `artifacts\DxTools-0.10.0-x64.msi`, 64 817 625 байт
SHA-256: `37CB49FCA991B0E36BE74656CE9578A788FDB8D59AADC9CC5F7876B94BF78BD3`

## 1. Что сделано

### Двухшаговые цепочки hotkeys

- native binding расширен prefix-modifiers/prefix-virtual-key;
- hook хранит pending-id и таймаут 1,5 секунды;
- модификаторные keydown не сбрасывают ожидание;
- prefix и основное сочетание подавляются;
- managed `HotkeySetting`/`HotkeyInfo` хранят и отображают prefix;
- диалог захвата префикса;
- кнопки «Префикс»/«Сбросить префикс» на вкладке hotkeys;
- проверено: `Ctrl+Alt+F9`, затем `Ctrl+Alt+F10` переместил окно.

### Сохранено из предыдущих фаз

- titlebar-overlay, LL-hook, панель задач, DWM-превью, автоскрытие,
  pinned apps, профили, триггеры, C#-скрипты, CLI;
- 25 unit-тестов, MSI 0.10.0.

## 2. Проверки

| Проверка | Result |
|---|---|
| `dotnet build` | success, 0 warnings |
| `dotnet test` | 25/25 passed |
| Native API | api=3, DLL 0.5.0, ping OK |
| Hotkey chain | F9→F10 переместил foreground-окно |
| Log | `Moved window to monitor ...` |
| MSI administrative install | exit 0, 286 файлов |
| CI | success: MSVC native, 25 тестов, MSI, upload |

## 3. Ограничения и отклонения

1. **Системный трей и сплиты мониторов** не реализованы — Phase 2I/3.
2. Поддерживаются только двухшаговые цепочки.
3. Таймаут между шагами фиксирован — 1,5 секунды.
4. Нет fallback на `RegisterHotKey`, если native hook не установился.
5. Hook обрабатывает программно введённые клавиши.
6. Mixed DPI не проверен; MSI не подписан; полная установка с UAC не
   выполнялась.

## 4. Следующий шаг

Phase 2I/3: системный трей, сплиты мониторов; затем Phase 3 (виртуальные
десктопы, screensaver, иконки, аудио, UIAccess, ARM64, подпись).

## 5. Запрос подтверждения

Phase 2H (ядро) завершена. Для продолжения требуется подтверждение Phase 2I
или Phase 3. Рекомендуется вручную установить MSI с UAC и проверить цепочки
hotkeys и удаление.
