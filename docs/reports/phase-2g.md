# Phase 2G — отчёт (ядро)

Дата: 2026-11-20
Статус: **ядро завершено, ожидает подтверждения**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `908e350` (`Phase 2G: titlebar overlay buttons`)
CI: GitHub Actions run `44822243495` — **success**
MSI: `artifacts\DxTools-0.9.0-x64.msi`, 64 895 447 байт
SHA-256: `F1DCD6D78A08256ABFFAF2D8DA0FAC8EC72D62CEB44B766354F3D652CB55BC27`

## 1. Что сделано

### Titlebar overlay

- topmost overlay активного окна с кнопками «→» (следующий монитор) и
  «T» (always-on-top);
- позиционирование по `DWMWA_EXTENDED_FRAME_BOUNDS` с учётом DPI;
- `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE`, работа без инъекций;
- кнопки работают по целевому HWND через `WindowManager.MoveWindow` и
  `ToggleTopmost(IntPtr)`;
- overlay обновляется каждые 250 мс и следует за foreground-окном;
- скрывается для собственных окон, shell, свёрнутых/полноэкранных и
  нестандартных окон без caption;
- настройка «Кнопки в заголовке окон» в разделе «Общие»;
- включение/выключение без перезапуска.

### Сохранено из предыдущих фаз

- LL-hook hotkeys, панель задач, DWM-превью, автоскрытие, pinned apps,
  профили, триггеры, C#-скрипты, CLI;
- 25 unit-тестов, MSI 0.9.0.

## 2. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build` | success, 0 warnings |
| `dotnet test` | 25/25 passed |
| Overlay | отображается в заголовке активного окна |
| Move button | клик переместил окно, лог `Moved window to monitor` |
| Topmost button | обработчик вызывает `ToggleTopmost` для target HWND |
| Настройка | включена через `enableTitleBarButtons` |
| MSI administrative install | exit 0, 286 файлов |
| CI | success: MSVC native, 25 тестов, MSI, upload |
| UI | скриншот overlay |

## 3. Ограничения и отклонения

1. **Системный трей, сплиты мониторов, многошаговые цепочки** не
   реализованы — Phase 2H/3.
2. Overlay показывает две фиксированные кнопки, без настройки набора.
3. Позиция вычисляется по стандартной ширине caption-кнопок; для
   нестандартных тем возможен сдвиг.
4. Поддержка только окон со стандартным caption; UWP/кастомные заголовки
   могут не получать overlay.
5. Overlay не проверялся на mixed DPI.
6. MSI не подписан; полная установка с UAC не выполнялась.

## 4. Следующий шаг

Phase 2H/3: системный трей, сплиты мониторов, цепочки hotkeys; затем Phase 3
(виртуальные десктопы, screensaver, иконки, аудио, UIAccess, ARM64, подпись).

## 5. Запрос подтверждения

Phase 2G (ядро) завершена. Для продолжения требуется подтверждение Phase 2H
или Phase 3. Рекомендуется вручную установить MSI с UAC и проверить overlay,
hotkeys и удаление.
