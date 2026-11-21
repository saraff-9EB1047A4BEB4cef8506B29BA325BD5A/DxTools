# Release candidate 1.0.0-rc.5 — отчёт

Дата: 2026-11-21
Статус: **release candidate готов, ожидает подтверждения GA**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `4eead74` (`Release 1.0.0-rc.5: titlebar buttons match system caption style`)
CI: GitHub Actions run `44928485991` — success

## 1. Артефакты

| Артефакт | Размер | SHA-256 |
|---|---:|---|
| `artifacts\DxTools-1.0.0-x64.msi` | 152 186 059 | `593642CB7D71886ED2BD95BE79671D532EB710CE426AD3DB6FF3C9B785B0F315` |
| `artifacts\DxTools-1.0.0-arm64.msi` | 140 938 318 | `93E16E4BA4BFE4249776DB1C012D11BC557CA8EEAC81FFE990206BEB34CD13E0` |

## 2. Titlebar-overlay

Кнопки перенесены в стиль системных caption-кнопок Windows 11:

- размер 46×32 DIP, две кнопки вплотную друг к другу;
- прозрачный фон, без рамки и скруглений;
- шрифт `Segoe Fluent Icons` (fallback `Segoe MDL2 Assets`);
- глифы `E72A` (следующий монитор) и `E718` (always-on-top);
- при наведении — фон `#00FF00` (lime), при нажатии — `#00C400`;
- на lime-фоне глиф чёрный;
- обычный цвет глифа определяется темой целевого окна через
  `DWMWA_USE_IMMERSIVE_DARK_MODE`: белый для тёмной, чёрный для светлой;
- overlay позиционируется вплотную к системным кнопкам с учётом DPI;
- window остаётся `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE` и не забирает фокус.

Глифы проверены в `SegoeIcons.ttf` и `segmdl2.ttf`:
`E72A`, `E718`, `E8BB`, `E921`, `E922` присутствуют.

## 3. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build -c Release` | success, 0 warnings, 0 errors |
| `dotnet test -c Release` | 35/35 passed |
| Шрифты | глифы E72A/E718 есть в Segoe Fluent Icons и Segoe MDL2 Assets |
| x64 MSI administrative install | exit 0, 289 файлов, app `1.0.0-rc.5` |
| ARM64 MSI administrative install | exit 0, 288 файлов, app `1.0.0-rc.5` |
| CI | success: run `44928485991` |

CI-артефакты: `DxTools-msi` 151 190 107 байт, `DxTools-publish` 190 486 134
байт. Визуальная проверка на живом окне с light/dark темой остаётся ручной:
в текущей среде нет интерактивного рабочего стола.

## 4. Известные ограничения

1. Overlay поддерживает только окна со стандартным caption; UWP/кастомные
   заголовки могут не получать кнопки.
2. Ширина caption-кнопок предполагается стандартной (46 DIP); при
   нестандартной теме возможно смещение.
3. Mixed DPI и maximized-окна требуют ручной проверки.
4. UIAccess, shell extension и остальные ограничения rc.4 сохраняются.

## 5. Release checklist

См. `docs/release-checklist.md`. Не закрыты: подпись, UIAccess/elevated-тест,
UAC-инсталляция, runtime-тест ARM64, независимый security review и публикация
GitHub Release.

## 6. Запрос подтверждения

RC 1.0.0-rc.5 готов. Рекомендуется вручную проверить кнопки на светлом и
тёмном окне, при 100%/125%/150% DPI и на maximized-окне.
