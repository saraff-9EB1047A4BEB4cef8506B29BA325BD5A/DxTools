# Phase 2F — отчёт (ядро)

Дата: 2026-11-20
Статус: **ядро завершено, ожидает подтверждения**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `148a662` (`Phase 2F: native low-level keyboard hook`)
CI: GitHub Actions run `44820393046` — **success**
MSI: `artifacts\DxTools-0.8.0-x64.msi`, 64 813 527 байт
SHA-256: `F3BDAEC6E1DAD1C04E0F2657C3979BCEFC1500E65F3A23CCA423CCA559350DAA`

## 1. Что сделано

### Native low-level keyboard hook

- `WH_KEYBOARD_LL` в `DxTools.Native.dll`, API v3;
- выделенный поток с message loop, установка/снятие hook;
- таблица привязок `DxTools_HotkeyBinding`, обновление без перезапуска;
- проверка модификаторов (Ctrl/Alt/Shift/Win) и virtual key;
- совпавшая комбинация подавляется (`return 1`) и постится в UI через
  `WM_APP+77`;
- поддержка сочетаний с Win;
- защита от повторного запуска и корректное завершение потока.

### Managed интеграция

- `NativeHotkey` P/Invoke (`Start/Stop/SetBindings`);
- `HotkeyService` переведён с `RegisterHotKey` на native hook;
- редактор сочетаний сохраняет модификаторы/виртуальный ключ и вызывает
  перерегистрацию;
- при ошибке native hook пишется предупреждение, UI показывает hotkeys
  неактивными.

### Сохранено из предыдущих фаз

- панель задач, DWM-превью, автоскрытие, pinned apps, профили, триггеры,
  C#-скрипты, CLI;
- 25 unit-тестов, MSI 0.8.0.

## 2. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build` | success, 0 warnings |
| `dotnet test` | 25/25 passed |
| Native API | api=3, DLL 0.4.0, ping OK |
| LL-hook | `Ctrl+Alt+Right` переместил foreground-окно |
| Log | `Moved window to monitor ...` |
| MSI administrative install | exit 0, 286 файлов |
| CI | success: MSVC native, 25 тестов, MSI, upload |
| `--safe-mode` | hotkey service не создаётся |

## 3. Ограничения и отклонения

1. **Системный трей, titlebar-кнопки, сплиты мониторов** не реализованы —
   Phase 2G/3.
2. **Многошаговые цепочки** не реализованы; поддерживаются одиночные
   сочетания.
3. Нет fallback на `RegisterHotKey`, если native hook не установился.
4. Hook подавляет совпавшие комбинации; это ожидаемо, но может конфликтовать
   с другими утилитами.
5. Введённые программно клавиши также обрабатываются.
6. Mixed DPI не проверен; MSI не подписан; полная установка с UAC не
   выполнялась.

## 4. Следующий шаг

Phase 2G/3: системный трей, titlebar-кнопки, сплиты, цепочки; затем Phase 3
(виртуальные десктопы, screensaver, иконки, аудио, UIAccess, ARM64, подпись).

## 5. Запрос подтверждения

Phase 2F (ядро) завершена. Для продолжения требуется подтверждение Phase 2G
или Phase 3. Рекомендуется вручную установить MSI с UAC и проверить hotkeys
и удаление.
