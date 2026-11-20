# Phase 3C — отчёт (ядро)

Дата: 2026-11-20
Статус: **ядро завершено, ожидает подтверждения**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `98233eb` (`Phase 3C: multi-monitor screensaver`)
CI: GitHub Actions run `44837481581` — **success**
MSI: `artifacts\DxTools-0.16.0-x64.msi`, 120 580 663 байт
SHA-256: `16BA0C7F3DBCC40E05BDE6DA133CBD2EBBE30F78207290070A0960D1F868A239`

## 1. Что сделано

### Multi-monitor screensaver

- отдельный проект `DxTools.Screensaver` (WPF WinExe);
- режим `/s` создаёт окно на каждом мониторе, `/c` открывает настройки DxTools,
  `/p` завершается;
- изображения берутся из папки слайдшоу обоев
  (`wallpaper.slideshowFolder`) или «Изображения»;
- смена кадра каждые 10 секунд;
- выход по движению мыши, клику или клавише (с задержкой 1,2 секунды);
- `.scr` публикуется single-file и включён в MSI;
- проверено: два окна screensaver 1920×1080 и 2112×1320.

### Поставка

- MSI 0.16.0 содержит `DxTools.scr`;
- WPF/.NET зависимости уже есть в папке DxTools, `.scr` использует их;
- 27 unit-тестов;
- README, USER_MANUAL, spec, architecture, CHANGELOG обновлены.

## 2. Проверки

| Проверка | Result |
|---|---|
| `dotnet build` | success, 0 warnings |
| `dotnet test` | 27/27 passed |
| Screen windows | 1920×1080 + 2112×1320 |
| Image slideshow | кадры из папки, 10 секунд |
| Dismiss | mouse/key handler |
| MSI administrative install | exit 0, 287 файлов |
| DxTools.scr в MSI | да |
| CI | success: MSVC native, 27 тестов, MSI, upload |

## 3. Ограничения и отклонения

1. **Виртуальные десктопы, shell extension, UIAccess, ARM64, подпись** не
   реализованы — Phase 3D/1.0.
2. Режим `/p` (preview) не поддерживается; окно предпросмотра пустое.
3. Нет видео и отдельных настроек интервала в UI.
4. `.scr` нельзя перемещать отдельно от папки DxTools: нужны native WPF/.NET
   файлы.
5. MSI вырос до ~120 МБ из-за self-contained screensaver.
6. Mixed DPI не проверен; MSI не подписан; полная установка с UAC не
   выполнялась.

## 4. Следующий шаг

Phase 3D/1.0: виртуальные десктопы, shell extension, UIAccess, ARM64,
подпись, security review и финальный RC.

## 5. Запрос подтверждения

Phase 3C (ядро) завершена. Для продолжения требуется подтверждение Phase 3D.
Рекомендуется вручную установить MSI с UAC и проверить screensaver через
«Параметры → Персонализация → Экран блокировки → Заставка».
