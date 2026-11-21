# Release candidate 1.0.0-rc.1 — отчёт

Дата: 2026-11-20
Статус: **release candidate готов, ожидает подтверждения GA**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `2821734` (`Release 1.0.0-rc.1: hardening and release metadata`)
CI: GitHub Actions run `44840736869` — **success**
MSI: `artifacts\DxTools-1.0.0-x64.msi`, 120 597 045 байт
SHA-256: `0826B6B44B3D383FFC9EE06CC310C276128D8ADE67A9C6DB5B885A8EEAF5BE9B`

## 1. Состав RC

- профили дисплеев и мониторов, topology hash, auto-apply, откат;
- панель задач на вторичных мониторах: AppBar, группировка, pinned apps,
  DWM-превью, автоскрытие, taskbar для сплитов, tray mirror с live-обновлением;
- профили позиций окон, titlebar-overlay, сплиты и snap-hotkeys;
- глобальные hotkeys на LL-hook: редактор, Win-сочетания, двухшаговые цепочки;
- VD hotkeys (навигация/перенос через системные сочетания);
- обои per-monitor/span, slideshow, multi-monitor screensaver;
- триггеры и C#-скрипты;
- profiles иконок рабочего стола, переключение аудиоустройств;
- CLI/IPC, backup/restore, RU/EN, темы, safe mode;
- MSI 1.0.0 с license UI, `.scr`, LICENSE/NOTICES;
- 27 unit-тестов;
- SECURITY.md, security review, release checklist, sign.ps1.

## 2. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build` | success, 0 warnings |
| `dotnet test` | 27/27 passed |
| Native API | api=4, DLL 0.6.0 |
| MSI administrative install | exit 0, 287 файлов |
| `.scr` в MSI | да |
| CI | success: MSVC native, 27 тестов, MSI, upload |

## 3. Известные ограничения RC

1. MSI и бинарники **не подписаны**; SmartScreen может предупреждать.
2. **UIAccess/elevated helper** не реализован; elevated-окна могут не
   управляться.
3. **Shell context menu** не реализован.
4. **ARM64** не собран и не проверен.
5. Виртуальные десктопы — только hotkeys через системные сочетания, без
   списка/профилей.
6. Screensaver не поддерживает `/p` preview и видео.
7. Cross-process доступ к Explorer (иконки) и UIPI могут вызывать вопросы AV.
8. Mixed DPI не проверен на hardware.
9. Нет независимого security review и GA soak.
10. При аварийном kill панель Windows может остаться скрытой.

## 4. Release checklist

См. `docs/release-checklist.md`. Не закрыты: подпись, UAC-инсталляция на
чистой VM, ARM64, shell extension, UIAccess, независимый security review,
публикация GitHub Release.

## 5. Запрос подтверждения

RC 1.0.0-rc.1 готов. Требуется решение:
- принять RC как 1.0 GA с текущими ограничениями;
- или продолжить 1.0.x: подпись, UIAccess, shell extension, ARM64,
  security review.

Рекомендуется сначала установить MSI с UAC, проверить основные сценарии и
подписать бинарники.
