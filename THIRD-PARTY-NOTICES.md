# Third-party notices

DxTools распространяется по GNU GPL v2. Ниже перечислены сторонние
компоненты и их лицензии. Поставляемый код использует только лицензии,
совместимые с GPLv2.

## Runtime

| Компонент | Лицензия | Использование |
|---|---|---|
| .NET 10 Runtime / WPF | MIT | поставляется self-contained |
| System.Drawing.Common 10 | MIT | композиция обоев |
| Microsoft.CodeAnalysis.CSharp.Scripting | MIT | выполнение C#-скриптов |
| Windows SDK headers/libs | Microsoft license | компиляция native-кода |

## Build and test only (не входят в поставку)

| Компонент | Лицензия | Использование |
|---|---|---|
| WiX Toolset 6 | MS-RL | сборка MSI |
| WixToolset.UI.wixext | MS-RL | лицензионный диалог MSI |
| xUnit | Apache-2.0 | unit-тесты |
| Microsoft.NET.Test.Sdk | MIT | unit-тесты |
| coverlet.collector | MIT | покрытие тестов |
| Zig (локальный fallback) | MIT | локальная сборка native |

## Ограничения

- Apache-2.0 зависимости допускаются только как test-only и не поставляются.
- Перед добавлением новой зависимости проверьте совместимость с GPLv2 и
  обновите этот файл.
