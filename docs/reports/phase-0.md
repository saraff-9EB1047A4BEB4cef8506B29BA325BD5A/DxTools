# Phase 0 — отчёт о фундаменте

Дата: 2026-11-20
Статус: **завершён, ожидает подтверждения перехода к Phase 1**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит кода: `623712c` (`fix: dispose tray menu and simplify tray host`)
Первый коммит этапа: `9453623` (`Phase 0: foundation, tray app, native probe, and MSI`)
CI: GitHub Actions run `44797624483` — **success**

## 1. Что сделано

### Инфраструктура

- git-репозиторий, ветка `main`, лицензия GPL-2.0, `.gitignore`, `.gitattributes`,
  `.editorconfig`, `global.json`, центральные версии пакетов;
- решение `DxTools.sln` из пяти проектов:
  `Core`, `Interop`, `Engine`, `App`, `Core.Tests`;
- GitHub Actions: restore, native build, тесты, publish, MSI, артефакты;
- воспроизводимая сборка `build/build.ps1`;
- генератор WiX-фрагмента по publish-каталогу с детерминированными GUID.

### Приложение

- WPF-приложение с треем, окном настроек, светлой/тёмной/системной темой;
- RU/EN локализация через `.resx` (переключение языка после перезапуска);
- настройки JSON: версия схемы, атомарная запись, восстановление после
  повреждения, резервная копия повреждённого файла;
- автозапуск через HKCU Run, скрытый старт `--autostart`;
- single instance через named pipe + mutex, повторный запуск открывает окно;
- закрытие окна скрывает приложение в трей;
- безопасный режим `--safe-mode` без native-проверок;
- файловые логи, crash handler, self-check в окне диагностики;
- проверка native runtime: API, версия, handshake, путь.

### Native

- `DxTools.Native.dll` с C ABI v1:
  `DxTools_GetApiVersion`, `DxTools_GetVersion`, `DxTools_Ping`;
- сборка CMake + MSVC (эталон, CI), локальный fallback Zig;
- отсутствие внешних runtime-зависимостей.

### Установщик

- WiX 6, per-machine, x64, `Program Files\DxTools`;
- ярлык в общем меню «Пуск», ARP-иконка и ссылка на проект;
- self-contained .NET 10, native DLL, RU satellite resources;
- MajorUpgrade и удаление компонентов;
- MSI: `artifacts\DxTools-0.1.0-x64.msi`, 61 189 994 байт,
  SHA-256 `215E207E522201855F999029B39783F45DC669940F6682A120FA6F1BBCF3D73E`.

### Документация

- `spec.md` — спецификация и план с gate-процессом;
- `README.md` — обзор, сборка, структура, лицензия;
- `USER_MANUAL.md` — установка, настройки, трей, safe mode, логи, удаление;
- `docs/architecture.md` — архитектура;
- скриншоты RU/EN в `docs/images/`.

## 2. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build DxTools.sln -c Release` | success, 0 warnings |
| `dotnet test DxTools.sln -c Release` | 11/11 passed |
| Полная сборка `build.ps1` | success |
| Native handshake | API 1, DLL 0.1.0, ping OK |
| UI RU + dark | проверено визуально |
| UI EN + light | проверено визуально |
| Single instance | второй процесс завершился с кодом 0, первый показал окно |
| Закрытие окна | процесс жив, окно скрыто |
| Повторный запуск | окно снова показано |
| `--autostart` | процесс жив, окно скрыто |
| `--safe-mode` | native probe отключён, приложение работает |
| MSI administrative install | exit 0, 276 файлов, payload полный |
| CI | MSVC native build, тесты, MSI, upload артефактов — success |

## 3. Отклонения и ограничения

1. **Локальный MSVC отсутствует.** Нативная библиотека локально собрана
   portable Zig 0.15.1. Эталонная сборка MSVC выполнена в CI и подтверждена
   зелёным запуском. Для дальнейшей работы рекомендуется установить workload
   «Desktop development with C++» и Windows 11 SDK.
2. **Полная установка MSI не выполнена в этой сессии.** Установщик
   per-machine требует UAC, а сессия запущена без повышения прав. MSI проверен
   административной установкой (`msiexec /a`): код 0, все файлы на месте.
   Рекомендуется вручную запустить
   `artifacts\DxTools-0.1.0-x64.msi` и проверить установку/удаление.
3. **MSI не подписан.** SmartScreen может показать предупреждение. Подпись
   и UIAccess запланированы на Phase 3.
4. **Нет UI-мастера и лицензионного диалога MSI.** Текст GPLv2 находится в
   репозитории; лицензионный RTF и WixUI — Phase 1.
5. **Функции мониторов, окон, обоев и панели задач ещё не реализованы.**
   Это содержание Phase 1–2 согласно `spec.md`.
6. **Автозапуск пишется от имени текущего пользователя** (HKCU), установка —
   per-machine. Это ожидаемое поведение.

## 4. Следующий этап — Phase 1

- модель мониторов и watcher (DisplayConfig, topology hash);
- профили дисплеев: save/apply/rollback, подтверждение, hotplug;
- горячие клавиши (native LL-hook, редактор, конфликты);
- перенос окон между мониторами с учётом DPI, always-on-top, прозрачность;
- обои per-monitor/span/slideshow, профили;
- backup/restore настроек, авто-применение профилей;
- MSI: лицензионный диалог и мастер установки;
- расширение тестов и тест-матрицы.

## 5. Запрос подтверждения

Phase 0 завершён. Для перехода к Phase 1 требуется подтверждение
пользователя. Опционально: перед подтверждением запустить MSI с правами
администратора и проверить установку/удаление на текущей машине.
