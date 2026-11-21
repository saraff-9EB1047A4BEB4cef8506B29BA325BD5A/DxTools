# Release candidate 1.0.0-rc.3 — отчёт

Дата: 2026-11-21
Статус: **release candidate готов, ожидает подтверждения GA**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммиты: `67ef588` (shell extension), `41e9d76` (исправление MSVC linkage)
CI: GitHub Actions run `44921787335` — success

## 1. Артефакты

| Артефакт | Размер | SHA-256 |
|---|---:|---|
| `artifacts\DxTools-1.0.0-x64.msi` | 120 794 201 | `8BA64A12748B4BC3DF5DA5AD4D82F590F8B79E108F81B702D42AEDFF419A4D05` |
| `artifacts\DxTools-1.0.0-arm64.msi` | 111 254 492 | `6830016CEFF31E5E4B16A1DFC7BBA801CDC04E50D81F37CA86BFD1C1B4D37B67` |

## 2. Shell extension

`DxTools.ShellExt.dll` — C++ COM in-process расширение Explorer:

- CLSID `{C7B775ED-1A48-4F52-9B91-6FE8B9B22097}`;
- `IShellExtInit` + `IContextMenu`;
- фон рабочего стола/папок: «Открыть настройки», «Применить обои»;
- изображение: «Сделать обоями»; при пустых настройках — все мониторы,
  иначе монитор под курсором;
- RU/EN по языку Windows;
- регистрация в MSI: `HKLM\Software\Classes\...\ContextMenuHandlers\DxTools`,
  `InprocServer32` с `ThreadingModel=Apartment`, компонент 64-bit;
- `regsvr32` для ручной проверки пишет в `HKCU`.

CLI/IPC:

- `--wallpaper <path>` — назначить изображение обоями;
- `--monitor <device>` — выбрать монитор;
- работающий экземпляр получает команду через named pipe (`cmd:` + base64);
- команды из shell extension не открывают окно настроек.

## 3. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build -c Release` | success, 0 warnings, 0 errors |
| `dotnet test -c Release` | 35/35 passed |
| Native API | api=4, DLL 0.6.0 |
| Shell extension x64 | PE `0x8664`, 554 496 байт |
| Shell extension ARM64 | PE `0xAA64`, 505 344 байт |
| COM-тест x64 | `CoCreateInstance` OK, `Initialize` OK, 2 пункта меню |
| x64 MSI administrative install | exit 0, 288 файлов, shell extension присутствует |
| ARM64 MSI administrative install | exit 0, 287 файлов, shell extension присутствует |
| MSI Registry / Component | 4 ключа shell extension; `Attributes=256` (64-bit) |
| CI | success: run `44921787335` |

CI-артефакты: `DxTools-msi` 119 916 219 байт, `DxTools-publish` 150 646 585
байт. Первый запуск rc.3 (`44921565910`) завершился ошибкой MSVC linkage;
исправление `41e9d76` проверено повторным запуском.

COM-тест вернул пункты: «DxTools: Открыть настройки» и
«DxTools: Применить обои». Меню для изображения проверено кодом и сборкой,
без автоматического COM-теста с `IDataObject`.

## 4. Известные ограничения

1. MSI, бинарники и shell extension **не подписаны**.
2. После установки может потребоваться перезапуск Explorer.
3. «Сделать обоями» поддерживает PNG/JPEG/BMP/GIF/TIFF; при пустых настройках
   файл применяется ко всем мониторам, иначе к монитору под курсором.
4. ARM64 MSI проверен административной установкой, но не запускался на
   ARM64 hardware.
5. UIAccess/elevated helper не реализован.
6. Нет независимого security review и GA soak.
7. Виртуальные десктопы — только hotkeys; screensaver без `/p` и видео.
8. Mixed DPI не проверен на hardware; возможны вопросы AV к cross-process
   доступу Explorer.

## 5. Release checklist

См. `docs/release-checklist.md`. Не закрыты: подпись, UAC-инсталляция на
чистой VM, runtime-тест ARM64, UIAccess, независимый security review и
публикация GitHub Release.

## 6. Запрос подтверждения

RC 1.0.0-rc.3 готов. Требуется решение:
- принять RC как 1.0 GA с текущими ограничениями;
- или продолжить 1.0.x: подпись, UIAccess, runtime-тест ARM64 и независимый
  security review.

Рекомендуется установить x64 MSI с UAC на чистой VM, проверить контекстное
меню и основные сценарии, затем подписать бинарники.
