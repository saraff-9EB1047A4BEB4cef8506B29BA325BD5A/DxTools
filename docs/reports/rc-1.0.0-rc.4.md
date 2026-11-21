# Release candidate 1.0.0-rc.4 — отчёт

Дата: 2026-11-21
Статус: **release candidate готов, ожидает подтверждения GA**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `6b4be0d` (`Release 1.0.0-rc.4: UIAccess helper for elevated windows`)
CI: GitHub Actions run `44926367171` — success

## 1. Артефакты

| Артефакт | Размер | SHA-256 |
|---|---:|---|
| `artifacts\DxTools-1.0.0-x64.msi` | 152 210 635 | `949BBAC0E1BBB1CCFE09BADC5302768B8F57A629C90EDB6DC98186CA78BF1506` |
| `artifacts\DxTools-1.0.0-arm64.msi` | 140 897 358 | `4453A66AB9920C111E1BC5F1C2B8D74E2B8F3B4AE252B576883E44A6EB86F4C1` |

## 2. UIAccess

`DxTools.Helper.exe` — отдельный self-contained WinExe:

- манифест `asInvoker/uiAccess=true`;
- запускается основным приложением через `UIAccessClient`;
- named pipe `DxTools.UIAccess.<pid>.<nonce>` с `PipeOptions.CurrentUserOnly`;
- проверяет PID клиента и путь родительского `DxTools.exe`;
- команды: `move`, `topmost`, `activate`; только top-level HWND;
- при отказе прямого Win32-вызова WindowManager/WindowPlacementService и
  активация из панели задач используют helper;
- при отсутствии/неподписанном helper приложение автоматически работает без
  UIAccess.

Подпись и упаковка:

- `build.ps1 -SignThumbprint` подписывает helper/app/native/shell extension и
  screensaver **до** генерации WiX fragment, затем подписывает MSI;
- helper устанавливается в `Program Files\DxTools`;
- UIAccess активируется Windows после доверия сертификату и согласия
  пользователя при первом запуске.

## 3. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build -c Release` | success, 0 warnings, 0 errors |
| `dotnet test -c Release` | 35/35 passed |
| Native API | api=4, DLL 0.6.0 |
| Helper x64 | PE `0x8664`, 100 156 103 байт, `uiAccess="true"` |
| Helper ARM64 | PE `0xAA64`, 109 155 027 байт, `uiAccess="true"` |
| IPC-тест helper (asInvoker) | `move` и `topmost` — `ok:true` |
| IPC-тест `activate` без UIAccess | `ok:false`, foreground не получен |
| x64 MSI administrative install | exit 0, 289 файлов, helper и shell extension присутствуют |
| ARM64 MSI administrative install | exit 0, 288 файлов, helper и shell extension присутствуют |
| CI | success: run `44926367171` |

CI-артефакты: `DxTools-msi` 151 254 222 байт, `DxTools-publish` 190 484 930
байт. Тест helper выполнялся с тестовым манифестом `uiAccess="false"`: move и topmost
работают, activate без UIAccess ожидаемо не получает foreground. Штатный helper
с `uiAccess="true"` без подписи Windows запускать отказывается — это ожидаемо;
приложение в таком случае использует fallback.

## 4. Известные ограничения

1. UIAccess **не активируется без подписанного helper**; текущие артефакты и
   CI не подписаны.
2. Нужен доверенный code-signing сертификат и согласие пользователя на
   UIAccess prompt.
3. `DxTools.Helper.exe` — self-contained single-file, около 100 МБ; MSI вырос
   до ~152 МБ (x64).
4. ARM64 MSI проверен административной установкой, но не запускался на
   ARM64 hardware.
5. UIAccess/elevated-сценарии требуют проверки на чистой VM с elevated app.
6. Остаются ограничения shell extension, подписи, mixed DPI и сторонних API.

## 5. Release checklist

См. `docs/release-checklist.md`. Не закрыты: подпись,
UIAccess/elevated-тест на чистой VM, UAC-инсталляция, runtime-тест ARM64,
независимый security review и публикация GitHub Release.

## 6. Запрос подтверждения

RC 1.0.0-rc.4 готов. Требуется решение:
- принять RC как 1.0 GA с текущими ограничениями;
- или продолжить 1.0.x: подпись с UIAccess, elevated-тест на чистой VM,
  runtime-тест ARM64 и независимый security review.

Рекомендуется собрать подписанный пакет с `-SignThumbprint`, установить его на
чистой VM, разрешить UIAccess prompt и проверить перенос/topmost/активацию
elevated-окна.
