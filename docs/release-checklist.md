# Release checklist — 1.0.0-rc.4

- [x] Версия 1.0.0-rc.4, native API v4/0.6.0
- [x] `dotnet build` без предупреждений
- [x] 35 unit-тестов
- [x] Полная сборка MSI (x64 и ARM64)
- [x] Административная установка MSI (x64 и ARM64)
- [x] Shell extension: сборка x64/ARM64, COM-меню и регистрация в MSI
- [x] UIAccess helper: сборка x64/ARM64, манифест `uiAccess=true`, IPC-тест
- [x] README.md и USER_MANUAL.md
- [x] spec.md и отчёты фаз
- [x] SECURITY.md и security review
- [ ] CI для rc.4 (запуск после коммита)
- [ ] Подпись сертификатом, включая `DxTools.Helper.exe`
- [ ] UIAccess prompt и elevated-окна на чистой VM
- [ ] Установка/удаление с UAC на чистой VM
- [ ] Runtime-тест ARM64 на hardware
- [ ] Независимый security review
- [ ] Публикация GitHub Release
