# Phase 3A — отчёт (ядро)

Дата: 2026-11-20
Статус: **ядро завершено, ожидает подтверждения**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `d37c9b2` (`Phase 3A: desktop icon profiles`)
CI: GitHub Actions run `44834055515` — **success**
MSI: `artifacts\DxTools-0.14.0-x64.msi`, 64 805 337 байт
SHA-256: `221469F80E088B585A4C4B79E417FB24E6323CE19978717F974BE6FE0B8761B0`

## 1. Что сделано

### Профили иконок рабочего стола

- поиск `SHELLDLL_DefView` / `SysListView32` в Explorer, включая fallback через
  `WorkerW`;
- cross-process чтение позиций через `VirtualAllocEx`/`ReadProcessMemory`;
- сохранение позиций по индексу иконки;
- восстановление через `LVM_SETITEMPOSITION32` и `WriteProcessMemory`;
- профили в `%AppData%\DxTools\desktop-icons.json`;
- вкладка «Рабочий стол»: количество иконок, save/apply/delete;
- проверено: 31 иконка сохранена в профиль (2580 байт JSON).

### Сохранено из предыдущих фаз

- split taskbars, live tray, tray mirror, сплиты, hotkey-цепочки,
  titlebar-overlay, LL-hook, панель задач, DWM-превью, автоскрытие,
  профили, триггеры, C#-скрипты, CLI;
- 27 unit-тестов, MSI 0.14.0.

## 2. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build` | success, 0 warnings |
| `dotnet test` | 27/27 passed |
| Desktop icon capture | 31 иконка |
| Profile JSON | 2580 байт |
| Desktop profile store tests | roundtrip + corrupt backup |
| MSI administrative install | exit 0, 286 файлов |
| CI | success: MSVC native, 27 тестов, MSI, upload |

## 3. Ограничения и отклонения

1. **Виртуальные десктопы, screensaver, аудио, shell extension, UIAccess,
   ARM64 и подпись** не реализованы — Phase 3B.
2. Позиции сопоставляются по индексу; при изменении порядка иконок профиль
   может примениться неверно.
3. Автоматическое упорядочивание Windows может изменить результат.
4. Используется cross-process доступ к Explorer; антивирус может реагировать.
5. Mixed DPI не проверен; MSI не подписан; полная установка с UAC не
   выполнялась.

## 4. Следующий шаг

Phase 3B: виртуальные десктопы, screensaver, аудио, shell extension,
UIAccess, ARM64, подпись и hardening.

## 5. Запрос подтверждения

Phase 3A (ядро) завершена. Для продолжения требуется подтверждение Phase 3B.
Рекомендуется вручную установить MSI с UAC и проверить профили иконок.
