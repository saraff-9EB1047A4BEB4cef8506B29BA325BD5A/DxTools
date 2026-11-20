# Phase 3D — отчёт (ядро)

Дата: 2026-11-20
Статус: **ядро завершено, ожидает подтверждения**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `893ffe2` (`Phase 3D: virtual desktop hotkeys and security hardening`)
CI: GitHub Actions run `44839323093` — **success**
MSI: `artifacts\DxTools-0.17.0-x64.msi`, 120 629 815 байт
SHA-256: `1E4D6CBAF3F26BC0F357374BDD76441E792C2708CA79549AD341A81918DE6803`

## 1. Что сделано

### Виртуальные рабочие столы (hotkeys)

- действия `DesktopPrevious/Next` и `MoveWindowToPrevious/NextDesktop`;
- сочетания по умолчанию: `Ctrl+Alt+PageUp/PageDown` и
  `Ctrl+Alt+Shift+PageUp/PageDown`;
- `VirtualDesktopService` синтезирует системные Win+Ctrl(+Shift)+Arrow через
  `SendInput`;
- действия доступны в редакторе hotkeys и отображаются в списке.

### Hardening и поставка

- `SECURITY.md`: модель угроз, safe mode, отчётность, ограничения;
- `build/sign.ps1`: Authenticode-подпись по thumbprint и timestamp;
- 27 unit-тестов;
- MSI 0.17.0.

### Сохранено из предыдущих фаз

- screensaver, audio, desktop icons, split taskbars, live tray, tray mirror,
  сплиты, hotkey-цепочки, titlebar-overlay, LL-hook, панель задач,
  DWM-превью, автоскрытие, профили, триггеры, C#-скрипты, CLI.

## 2. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build` | success, 0 warnings |
| `dotnet test` | 27/27 passed |
| Virtual desktop actions | зарегистрированы, SendInput реализован |
| Hotkey bindings | 11 default bindings |
| SECURITY.md | добавлен |
| build/sign.ps1 | добавлен |
| MSI administrative install | exit 0, 287 файлов |
| CI | success: MSVC native, 27 тестов, MSI, upload |

## 3. Ограничения и отклонения

1. **Shell extension, UIAccess, ARM64, подпись сертификатом и security
   review** не завершены — Phase 1.0.
2. Виртуальные десктопы поддерживают только навигацию/перенос через
   системные сочетания; нет списка, имён и профилей.
3. `SendInput` зависит от поведения оболочки Windows 11; при изменении
   системных hotkeys действие может перестать работать.
4. Переключение VD не проверялось вручную (нет тестового второго рабочего
   стола в автоматизации).
5. MSI не подписан; полная установка с UAC не выполнялась.
6. Mixed DPI не проверен.

## 4. Следующий шаг

Phase 1.0 RC: shell extension, UIAccess/elevated helper, ARM64, подпись
сертификатом, security review, финальные README/USER_MANUAL и тег релиза.

## 5. Запрос подтверждения

Phase 3D (ядро) завершена. Для перехода к финальному RC 1.0 требуется
подтверждение пользователя.
