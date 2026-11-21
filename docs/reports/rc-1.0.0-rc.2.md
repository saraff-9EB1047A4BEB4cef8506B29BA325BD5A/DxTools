# Release candidate 1.0.0-rc.2 — отчёт

Дата: 2026-11-21
Статус: **release candidate готов, ожидает подтверждения GA**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `b201f3f` (`Release 1.0.0-rc.2: ARM64 cross-build and release metadata`)
CI: GitHub Actions run `44917494274` — success

## 1. Артефакты

| Артефакт | Размер | SHA-256 |
|---|---:|---|
| `artifacts\DxTools-1.0.0-x64.msi` | 120 691 253 | `7769688C909CFC154B1F0A3EB42AA51A4DF1E6056D4AB45353BB479C9B696A88` |
| `artifacts\DxTools-1.0.0-arm64.msi` | 111 163 832 | `A46CA3AC41C7703B289FA4A4A066D223498B75396AEF9BFB67E217E46BA3AF47` |

ARM64 MSI собран в отдельной копии рабочей копии, чтобы не перезаписывать
x64 `artifacts\publish`; оба MSI сохранены в `artifacts`.

## 2. Состав rc.2

- версия приложения 1.0.0-rc.2, native API v4/0.6.0;
- ARM64 cross-build: native DLL, self-contained publish, screensaver и MSI;
- параметризация платформы в `build/build.ps1` и WiX-проекте;
- `docs/arm64.md`, `docs/signing.md`;
- актуализированы README, USER_MANUAL, spec, CHANGELOG и release checklist;
- исправлены кодировка и переводы строк в изменённых документах.

Код приложения и native API относительно rc.1 не менялись.

## 3. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build -c Release` | success, 0 warnings, 0 errors |
| `dotnet test -c Release` | 27/27 passed |
| Native API | api=4, DLL 0.6.0 |
| x64 PE | `0x8664` (exe, native DLL, coreclr, scr) |
| ARM64 PE | `0xAA64` (exe, native DLL, coreclr, scr) |
| x64 MSI administrative install | exit 0, 287 файлов, `.scr` присутствует |
| ARM64 MSI administrative install | exit 0, 286 файлов, `.scr` присутствует |
| CI | success: run `44917494274` |

CI-артефакты: `DxTools-msi` 119 861 833 байт, `DxTools-publish` 150 642 652
байт. Локальная native-сборка выполнена portable Zig 0.17.1, поэтому локальные
MSI отличаются по размеру и SHA-256 от CI-сборки MSVC.

## 4. Известные ограничения

1. MSI и бинарники **не подписаны**; SmartScreen может предупреждать.
2. **UIAccess/elevated helper** не реализован; elevated-окна могут не
   управляться.
3. **Shell context menu** не реализован.
4. ARM64 MSI собран и проверен административной установкой, но **не запускался
   на ARM64 hardware**.
5. Виртуальные десктопы — только hotkeys через системные сочетания, без
   списка и профилей.
6. Screensaver не поддерживает `/p` preview и видео.
7. Cross-process доступ к Explorer (иконки) и UIPI могут вызывать вопросы AV.
8. Mixed DPI не проверен на hardware.
9. Нет независимого security review и GA soak.
10. При аварийном kill панель Windows может остаться скрытой.

## 5. Release checklist

См. `docs/release-checklist.md`. Не закрыты: подпись, UAC-инсталляция на
чистой VM, runtime-тест ARM64, shell extension, UIAccess, независимый security
review и публикация GitHub Release.

## 6. Запрос подтверждения

RC 1.0.0-rc.2 готов. Требуется решение:
- принять RC как 1.0 GA с текущими ограничениями;
- или продолжить 1.0.x: подпись, UIAccess, shell extension, runtime-тест
  ARM64 и независимый security review.

Рекомендуется сначала установить x64 MSI с UAC на чистой VM, проверить основные
сценарии и подписать бинарники.
