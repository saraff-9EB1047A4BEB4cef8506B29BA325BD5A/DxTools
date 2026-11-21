# Release checklist — 1.0.0-rc.8

- [x] Версия 1.0.0-rc.8, native API v6/0.8.0
- [x] `dotnet build DxTools.sln -c Release` без предупреждений
- [x] 59 unit-тестов
- [x] Native: multi-step hotkeys, app scope, layout `DxTools_HotkeyBinding` = 176 байт
- [x] Контекстные меню иконок в зеркале трея
- [x] Screensaver `/p <HWND>` и видео из папки слайдшоу
- [x] CHANGELOG.md, spec.md, USER_MANUAL.md, README.md, отчёт rc.8
- [x] Полная сборка MSI x64: `DxTools-1.0.0-x64.msi`, 155 364 555 байт,
      SHA-256 `C5C18EFC8B33F751E73A2CDB76668C799CD5B6C4E1CCA4B769662F6ED44DA8D1`
- [x] Административная установка MSI x64: exit 0, 289 файлов
- [ ] Полная сборка и административная установка ARM64 — отложены: нет целевой
      платформы
- [x] Shell extension и `.scr` входят в MSI
- [ ] CI для rc.8
- [x] Живая проверка меню панели задач и подсказок выполнена
- [x] Security review, soak 72 ч, установка/удаление с UAC на чистой VM
      приняты по решению заказчика
- [ ] Подпись сертификатом, включая `DxTools.Helper.exe` — отложена по бюджету
- [ ] UIAccess prompt и elevated-окна на чистой VM — не входит в бюджет
- [ ] Runtime-тест ARM64 на hardware — отложен: нет целевой платформы
- [ ] Ручная матрица mixed DPI/HDR/RDP/dock/sleep/elevated — не выполняется
      по бюджету
- [ ] Публикация GitHub Release — после review решения и актуализации README.md
