# Phase 3B — отчёт (ядро)

Дата: 2026-11-20
Статус: **ядро завершено, ожидает подтверждения**
Репозиторий: https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools
Коммит: `c5114bb` (`Phase 3B: audio device switching`)
CI: GitHub Actions run `44835975601` — **success**
MSI: `artifacts\DxTools-0.15.0-x64.msi`, 64 850 393 байт
SHA-256: `8F3A5769A898C8A13576F8E58BFDD13077D3F9ECE5068111906E733C9A551D4C`

## 1. Что сделано

### Переключение аудиоустройств

- native API v4: `DxTools_Audio_GetDeviceCount/GetDevices/SetDefault`;
- CoreAudio enumeration активных render-устройств, friendly names, default flag;
- `IPolicyConfig::SetDefaultEndpoint` для eConsole/eMultimedia/eCommunications;
- вкладка «Аудио» с устройствами и кнопкой «Сделать основным»;
- проверено: 3 активных устройства найдены;
- safe mode отключает изменения.

### Сохранено из предыдущих фаз

- desktop icon profiles, split taskbars, live tray, tray mirror, сплиты,
  hotkey-цепочки, titlebar-overlay, LL-hook, панель задач, DWM-превью,
  автоскрытие, профили, триггеры, C#-скрипты, CLI;
- 27 unit-тестов, MSI 0.15.0.

## 2. Проверки

| Проверка | Результат |
|---|---|
| `dotnet build` | success, 0 warnings |
| `dotnet test` | 27/27 passed |
| Native API | api=4, DLL 0.6.0 |
| Audio enumeration | 3 устройства |
| Set default | реализован, не переключался вручную |
| MSI administrative install | exit 0, 286 файлов |
| CI | success: MSVC native, 27 тестов, MSI, upload |

## 3. Ограничения и отклонения

1. **Виртуальные десктопы, screensaver, shell extension, UIAccess, ARM64,
   подпись** не реализованы — Phase 3C/1.0.
2. `IPolicyConfig` — непубличный API; возможны изменения в будущих сборках
   Windows 11.
3. Смена default endpoint не проверялась вручную, чтобы не менять аудио
   на рабочей машине.
4. Нет per-app аудио и hotkey-действия для смены устройства.
5. Mixed DPI не проверен; MSI не подписан; полная установка с UAC не
   выполнялась.

## 4. Следующий шаг

Phase 3C/1.0: VD, screensaver, shell extension, UIAccess, ARM64, подпись,
security review и финальный RC.

## 5. Запрос подтверждения

Phase 3B (ядро) завершена. Для продолжения требуется подтверждение Phase 3C.
Рекомендуется вручную установить MSI с UAC и проверить смену аудиоустройства.
