# Release checklist — 1.0.0-rc.7

- [x] Версия 1.0.0-rc.7, native API v5/0.7.0 (native не менялся)
- [x] `dotnet build DxTools.sln -c Release` без предупреждений
- [x] 41 unit-тест
- [x] Тематический стиль `ToolTip` для тёмной и светлой темы
- [x] Подсказки закреплённых приложений без окон
- [x] CHANGELOG.md, spec.md, USER_MANUAL.md, отчёт rc.7
- [ ] Полная сборка MSI (x64 и ARM64)
- [ ] Административная установка MSI (x64 и ARM64)
- [ ] Shell extension: регистрация в MSI
- [ ] CI для rc.7
- [ ] Подпись сертификатом, включая `DxTools.Helper.exe`
- [ ] UIAccess prompt и elevated-окна на чистой VM
- [ ] Установка/удаление с UAC на чистой VM
- [ ] Runtime-тест ARM64 на hardware
- [ ] Независимый security review
- [ ] Публикация GitHub Release
