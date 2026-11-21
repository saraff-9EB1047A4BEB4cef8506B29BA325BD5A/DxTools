# Отчёт 1.0.0-rc.7 — невидимый текст подсказок в тёмной теме

Дата: 2026-11-21.

## 1. Причина

В `src/DxTools.App/Themes/Controls.xaml` неявный стиль `TextBlock` принудительно
задаёт `Foreground="{DynamicResource TextPrimaryBrush}"`. Это же значение
получает `TextBlock`, который стандартный WPF `ToolTip` создаёт внутри себя.

Стандартный шаблон `ToolTip` при этом оставляет светлый системный фон. В тёмной
теме `TextPrimaryBrush` равен `#F2F5F9`, поэтому текст оказывался почти того же
цвета, что и фон.

Замер до исправления (offscreen-рендер с `Dark.xaml` + `Controls.xaml`):

- фон `ToolTip`: `#F1F2F7`;
- цвет текста: `#F2F5F9`;
- контраст около `1.02:1` — текст практически не виден.

На машине разработки `AppsUseLightTheme=0`, в `settings.json` тема `system`,
поэтому приложение применяет `Dark.xaml`. В светлой теме дефект не проявлялся.

Дефект затрагивал все WPF-подсказки:

- `TaskbarWindow.xaml`: кнопка `Dx`, кнопки окон, кнопка трея, иконки в popup;
- `TitleBarOverlayWindow.xaml`: кнопки titlebar-overlay.

Дополнительно: для закреплённого приложения без окон `ToolTipText` возвращал
пустую строку, то есть подсказка была пустой.

## 2. Реализовано

- В `Themes/Controls.xaml` добавлен неявный стиль `ToolTip`:
  - `Background="{DynamicResource SurfaceAltBrush}"`;
  - `BorderBrush="{DynamicResource BorderBrush}"`, толщина `1`;
  - `Foreground="{DynamicResource TextPrimaryBrush}"`;
  - `Padding="8,5"`, `CornerRadius="6"`, `HasDropShadow="True"`;
  - собственный `ControlTemplate` с `Border` и `ContentPresenter`;
  - `ContentTemplate` с `TextBlock`, `TextWrapping="Wrap"` и `MaxWidth="400"`,
    чтобы длинные заголовки не растягивали подсказку на весь экран.
- Все кисти заданы через `DynamicResource`, поэтому смена темы через
  `ThemeManager.Apply` обновляет подсказки без перезапуска.
- `TaskbarGroupItemViewModel.ToolTipText` для закреплённого приложения без окон
  возвращает `Title` (имя приложения), а не пустую строку.
- Обновлены версия, CHANGELOG, spec, USER_MANUAL, release checklist и этот отчёт.

## 3. Проверка

| Проверка | Результат |
|---|---|
| `dotnet build DxTools.sln -c Release` | success, 0 warnings, 0 errors |
| `dotnet test DxTools.sln -c Release --no-build` | 41/41 passed |
| Offscreen-рендер `ToolTip`, тёмная тема | фон `#1E2733`, текст `#F2F5F9`, рамка `#2B3645` |
| Offscreen-рендер `ToolTip`, светлая тема | фон `#EFF3F9`, текст `#101828`, рамка `#D9E0EA` |
| Многострочный текст | отрисован без обрезки (в offscreen-проверке) |
| Длинный однострочный текст | переносится при ширине около 400 DIP, высота растёт по строкам |

Контраст после исправления:

- тёмная тема: `#F2F5F9` на `#1E2733` — около `13:1`;
- светлая тема: `#101828` на `#EFF3F9` — около `15:1`.

## 4. Что осталось проверить вживую

- Фактические подсказки на панели задач DxTools, в трее и titlebar-overlay.
- Переключение темы на лету.
- DPI 100/125/150 % и mixed DPI.
- Длинные и многострочные заголовки окон.
- Закреплённое приложение без окон.
- Возможное наложение подсказки на DWM-превью: `ThumbnailPreviewWindow`
  показывается размером `340×200` на `origin.Y - 208`, то есть в области над
  кнопкой. Подсказка является topmost WPF popup и должна рисоваться выше, но
  это нужно подтвердить визуально. Если превью перекрывает подсказку, в
  следующем изменении добавить `ToolTipService.Placement="Top"` и
  `ToolTipService.VerticalOffset` для кнопок панели задач.

## 5. Ограничения

- Автоматического UI-теста для WPF-подсказок нет: проверка цвета выполнялась
  offscreen-рендером, поведение на реальном рабочем столе остаётся ручным.
- Стиль `ToolTip` глобальный; если в будущем понадобится другой цвет текста
  подсказки, потребуется локальный стиль или `ContentTemplate`.
