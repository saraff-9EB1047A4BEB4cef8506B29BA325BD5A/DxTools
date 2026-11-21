# Release checklist — 1.0.0-rc.6

- [x] Версия 1.0.0-rc.6, native API v5/0.7.0
- [x] `dotnet build` без предупреждений
- [x] 41 unit-тест
- [x] Native DLL x64/ARM64 (portable Zig cross-build), C ABI v5
- [x] Контекстное меню панелей задач DxTools и команды трея
- [x] Меню основной панели Windows по Ctrl+ПКМ (smoke-тест, UIA/AUMID)
- [x] README.md, USER_MANUAL.md, spec.md, architecture, security review, отчёт rc.6
- [ ] Полная сборка MSI (x64 и ARM64)
- [ ] Административная установка MSI (x64 и ARM64)
- [ ] Shell extension: регистрация в MSI
- [ ] CI для rc.6
- [ ] Подпись сертификатом, включая `DxTools.Helper.exe`
- [ ] UIAccess prompt и elevated-окна на чистой VM
- [ ] Установка/удаление с UAC на чистой VM
- [ ] Runtime-тест ARM64 на hardware
- [ ] Независимый security review
- [ ] Публикация GitHub Release
