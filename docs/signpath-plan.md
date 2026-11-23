# План подписи через SignPath Foundation

> **Статус (2026-11-23): G1 не пройден.** SignPath Foundation отклонил
> заявку: недостаточно внешних сигналов публичного доверия (stars, forks,
> contributors, статьи, независимые упоминания). План приостановлен;
> повторная заявка возможна после роста проекта. Для Беларуси коммерческие
> CA (Certum, DigiCert, GlobalSign, Sectigo) недоступны, поэтому основной
> путь — повторная заявка в Foundation; альтернативы — иностранное
> юрлицо/ВНЖ или иностранный со-мейнтейнер.
>
> **Решение 23.11.2026: пауза без подписи.** Активной заявки и сертификата
> нет, релизы публикуются без подписи. Разделы 13–14 сохранены для
> будущего возврата к вопросу.

## 0. Краткий вывод

1. SignPath Foundation даёт DxTools **бесплатную подпись Authenticode** для MSI и
   PE-файлов. Сертификат выпущен на SignPath Foundation, ключ хранится в HSM,
   `.pfx` не выдаётся, подпись выполняется только из CI.
2. Проект проходит по условиям: GPL-2.0-only, нет проприетарных компонентов и
   commercial dual-licensing (Pro Support — договор услуг, а не лицензия на ПО),
   есть публичные релизы и описание продукта.
3. До заявки нужно закрыть три разрыва: публичная **code signing policy**,
   MFA и формальные роли Author/Reviewer/Approver, а также релизный workflow,
   который собирает артефакт на GitHub-hosted runner и отправляет его в SignPath.
4. Инженерная работа — **2–3 дня**, внешнее ревью заявки — от нескольких дней до
   нескольких недель зависимости от очереди. Стоимость — 0.
5. Это не заменяет идентичность Microsoft Store: для загрузки MSIX в Store нужен
   self-signed сертификат с `Publisher` из Partner Center. SignPath-подпись
   используется для GitHub/MSI и сайдлоада, где `Publisher = CN=SignPath Foundation`.

## 1. Что даёт и чего не даёт

| Вопрос | Ответ |
|---|---|
| Кто издатель подписи | SignPath Foundation (`CN=SignPath Foundation`) |
| Что подписывается | MSI (deep signing вложенных PE), `.exe`, `.dll`, `.scr`, при необходимости `.msix` |
| Где хранится ключ | HSM SignPath; ключ и сертификат не экспортируются |
| Можно ли подписать локальную сборку | Нет. Только артефакт, собранный в CI из этого репозитория на GitHub-hosted runner |
| Нужен ли свой сертификат | Нет |
| UIAccess | Да: публично доверенная подпись + установка в `Program Files` закрывают условие для `DxTools.Helper.exe` |
| SmartScreen | Репутация накапливается на сертификат SignPath Foundation; в первое время предупреждения возможны |
| Store | Не заменяет: Microsoft переподписывает MSIX своей подписью, `Publisher` для загрузки должен совпадать с Partner Center |
| Цена | 0 для OSS |
| Ручное одобрение | Да, каждый signing request одобряется Approver |

## 2. Условия SignPath и соответствие DxTools

Источник: `signpath.org/terms`.

| Требование | Состояние DxTools | Действие |
|---|---|---|
| OSI-лицензия без commercial dual-licensing | `GPL-2.0-only`, второй лицензии нет; Pro Support — услуги, а не лицензия | Зафиксировать формулировку в заявке и `CODE_SIGNING.md` |
| Нет проприетарных компонентов | Весь код и build-скрипты в репозитории | — |
| Проект поддерживается | Регулярные коммиты, rc.8 | — |
| Уже выпущен в подписываемой форме | MSI rc.8 опубликован | При вопросе ревьюера — опубликовать стабильный 1.0.0 до заявки или подтвердить, что rc.8 является публичным релизом |
| Функциональность описана | README, USER_MANUAL, GitHub Releases | — |
| Нет malware/PUP | Утилита управления окнами и обоями | — |
| Приватность: сеть описана и отключаема | Телеметрии и сетевых вызовов нет; есть только константа URL репозитория | Описать в `CODE_SIGNING.md` |
| Свои репозиторий, бинарники и build-скрипты | Да | — |
| MFA у всех участников | Требуется включить | Включить 2FA на GitHub и SignPath |
| Роли Author / Reviewer / Approver | Фактически один мейнтейнер | Формально закрепить роли; CI-пользователь отправляет запрос, SARAFF одобряет |
| Публичная code signing policy | Отсутствует | Создать `CODE_SIGNING.md` и ссылку с заголовком «Code signing policy» |
| Есть uninstaller | MSI per-machine с удалением | — |
| Сборка на GitHub-hosted agents | `windows-latest` | Не использовать self-hosted для релизных job |
| Ручное одобрение каждой подписи | Настроить | Политика `release-signing` с 1 approval |

Обязательные формулировки на странице политики и в README:

- заголовок или ссылка **«Code signing policy»**;
- строка **«Free code signing provided by SignPath.io, certificate by SignPath Foundation»**;
- список подписываемых артефактов, роли, MFA, порядок одобрения, политика приватности.

## 3. Границы подписи

Подписываем в одном запросе:

- `DxTools-<version>-x64.msi` — deep signing;
- внутри MSI: `DxTools.exe`, `DxTools.Native.dll`, `DxTools.Helper.exe`,
  `DxTools.scr`, `DxTools.ShellExt.dll`.

Не подписываем отдельно:

- сторонние .NET-сборки внутри MSI — они покрыты подписью MSI;
- Store-вариант MSIX — у него другой `Publisher` и подпись Microsoft;
- локальные и форковые сборки — origin verification их не пропустит.

## 4. Этап 0 — подготовка репозитория

Оценка: 0.5–1 день.

1. Создать `CODE_SIGNING.md`:
   - лицензия и отсутствие commercial dual-licensing;
   - кто Author, Reviewer, Approver;
   - обязательный MFA для GitHub и SignPath;
   - каждый signing request одобряется вручную;
   - какие артефакты и из какой ветки подписываются;
   - приватность: телеметрии нет, сетевых вызовов нет, MSI удаляется штатно;
   - контакт для вопросов безопасности.
2. Добавить в `README.md` раздел **«Code signing policy»** со ссылкой на
   `CODE_SIGNING.md` и обязательной строкой про SignPath Foundation. Ссылку
   продублировать в разделе загрузки и в `docs/signing.md`.
3. Включить 2FA на GitHub; настроить ruleset/branch protection для `main`:
   запрет force-push и удаления, изменение только через PR, review от
   мейнтейнера. DCO из `CONTRIBUTING.md` сохраняется.
4. Зафиксировать роли:

| Роль | Кто | Обязанности |
|---|---|---|
| Author | SARAFF | Коммиты в репозиторий |
| Reviewer | SARAFF | Ревью внешних PR |
| Approver | SARAFF | Одобрение signing request |
| CI submitter | отдельный CI-пользователь SignPath | Только отправка артефактов, без права одобрения |

5. Убедиться, что публичный релиз и MSI доступны по постоянной ссылке.

Ворота G0: политика опубликована, ссылка в README работает, MFA включена,
роли и правила ветки зафиксированы.

## 5. Этап 1 — заявка

Оценка: 0.5 дня плюс ожидание ревью.

Подать на `signpath.org/apply`:

| Поле | Значение |
|---|---|
| Project | DxTools |
| Repository | `https://github.com/saraff-9EB1047A4BEB4cef8506B29BA325BD5A/DxTools` |
| License | `GPL-2.0-only` |
| Download page | GitHub Releases репозитория |
| Code signing policy | ссылка на `CODE_SIGNING.md` |
| Description | Утилиты Windows для нескольких мониторов: горячие клавиши, обои, панель задач, контекстное меню проводника, скринсейвер, UIAccess helper. Подписывается x64 MSI и вложенные PE |
| Maintainer | SARAFF, `support.dxtools@saraff.ru` |

В описании отдельно указать: Pro Support — платная поддержка GPL-сборки, а не
продажа лицензии, закрытая сборка или двойное лицензирование. Это снимает
вопрос по условию «no commercial dual-licensing».

Что могут уточнить ревьюеры:

- является ли rc.8 полноценным публичным релизом;
- кто владеет репозиторием и build-скриптами;
- как устроены роли при одном мейнтейнере;
- какие сетевые обращения делает приложение;
- почему подписывается MSI и какие файлы в него входят.

Ворота G1: приглашение в организацию SignPath.io. При отказе — зафиксировать
причину и переходить к Certum Open Source как резервному пути.

## 6. Этап 2 — настройка SignPath

Оценка: 1 день. Выполняется после одобрения заявки.

1. Принять приглашение, включить MFA в SignPath.
2. В организации добавить trusted build system **GitHub.com** и установить
   SignPath GitHub App с доступом к репозиторию (нужен для audit log).
3. Создать проект `dxtools`:
   - repository URL — URL репозитория;
   - description — продукт и подписываемые артефакты;
   - configurators — SARAFF.
4. Создать CI-пользователя с правом submitter; для Approver использовать
   личную учётную запись SARAFF. Разделение «отправляет CI, одобряет человек»
   даёт корректное разделение обязанностей.
5. Создать signing policy `release-signing`:
   - purpose: release;
   - certificate: сертификат SignPath Foundation;
   - submitters: только CI-пользователь;
   - approval: 1, approver SARAFF;
   - trusted build system: GitHub.com;
   - verify origin: включено;
   - allowed branch: `main`.
6. При необходимости создать вторую политику `manual-signing` с 2 approval и
   без ограничения ветки — только для исключительных случаев.
7. Создать artifact configuration `dx-tools-msi`. Проще всего загрузить
   unsigned MSI как sample и взять сгенерированную конфигурацию за основу,
   затем проверить пути внутри MSI. Черновик:

```xml
<artifact-configuration xmlns="http://signpath.io/artifact-configuration/v1">
  <zip-file>
    <msi-file path="DxTools-*-x64.msi">
      <pe-file path="DxTools.exe">
        <authenticode-sign />
      </pe-file>
      <pe-file path="DxTools.Native.dll">
        <authenticode-sign />
      </pe-file>
      <pe-file path="DxTools.Helper.exe">
        <authenticode-sign />
      </pe-file>
      <pe-file path="DxTools.scr">
        <authenticode-sign />
      </pe-file>
      <pe-file path="DxTools.ShellExt.dll">
        <authenticode-sign />
      </pe-file>
      <authenticode-sign />
    </msi-file>
  </zip-file>
</artifact-configuration>
```

Пути внутри MSI зависят от directory table: если sample покажет вложенную
папку `DxTools\`, пути корректируются на `DxTools\DxTools.exe` и т. д.
Конфигурацию хранить в репозитории, например
`build/signpath/artifact-configuration.xml`, и указывать её slug в CI.

8. Записать значения для CI: organization id, project slug, policy slug,
   artifact configuration slug.

Ворота G2: проект, политика и конфигурация сохранены; пробный signing request
создаётся и отклоняется/одобряется без ошибок конфигурации.

## 7. Этап 3 — релизный workflow

Оценка: 1 день.

Добавить `.github/workflows/release-sign.yml`, запуск `workflow_dispatch` с
параметром версии. Сборка — тот же `build/build.ps1`, без `-SignThumbprint`.

```yaml
name: Release signing

on:
  workflow_dispatch:
    inputs:
      version:
        required: true
        type: string

permissions:
  actions: read
  contents: read

jobs:
  sign:
    runs-on: windows-latest
    env:
      DOTNET_CLI_TELEMETRY_OPTOUT: "1"
      DOTNET_NOLOGO: "1"
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x
      - name: Build unsigned MSI
        shell: powershell
        run: .\build\build.ps1 -Configuration Release -ProductVersion ${{ inputs.version }}
      - name: Upload unsigned artifact
        id: upload
        uses: actions/upload-artifact@v4
        with:
          name: DxTools-msi-unsigned
          path: artifacts/DxTools-${{ inputs.version }}-x64.msi
          if-no-files-found: error
      - name: Submit signing request
        uses: signpath/github-action-submit-signing-request@v3
        with:
          api-token: ${{ secrets.SIGNPATH_API_TOKEN }}
          organization-id: ${{ vars.SIGNPATH_ORGANIZATION_ID }}
          project-slug: ${{ vars.SIGNPATH_PROJECT_SLUG }}
          signing-policy-slug: ${{ vars.SIGNPATH_POLICY_SLUG }}
          artifact-configuration-slug: ${{ vars.SIGNPATH_ARTIFACT_CONFIG_SLUG }}
          github-artifact-id: ${{ steps.upload.outputs.artifact-id }}
          wait-for-completion: true
          wait-for-completion-timeout-in-seconds: 3600
          output-artifact-directory: signed
      - name: Verify signature
        shell: powershell
        run: |
          $msi = Get-ChildItem signed -Filter *.msi -Recurse | Select-Object -First 1
          & signtool verify /pa /all $msi.FullName
          Get-AuthenticodeSignature $msi.FullName | Format-List
```

Правила:

- артефакт обязательно загружается в GitHub artifact до отправки в SignPath;
- все job релизного workflow — только на GitHub-hosted runners;
- `wait-for-completion` по умолчанию 600 секунд, ручное одобрение в это время
  не укладывается — увеличить таймаут или поставить `false` и забирать
  результат отдельным шагом/вебхуком;
- API token хранить только в GitHub Secrets, у CI-пользователя — минимальные
  права submitter;
- для публикации в Release добавить отдельный job с `contents: write`, чтобы
  не расширять права job, который общается с SignPath;
- подписанный MSI проверить `signtool verify /pa /all`, subject должен
  содержать `SignPath Foundation`, timestamp должен присутствовать.

Ворота G3: CI создаёт подписанный MSI, проверка подписи проходит, SHA-256
фиксируется в артефактах релиза.

## 8. Этап 4 — приёмка

Оценка: 0.5–1 день.

1. Проверить подпись `DxTools.exe`, `DxTools.Helper.exe`, `DxTools.scr`,
   `DxTools.Native.dll`, `DxTools.ShellExt.dll` внутри MSI и самого MSI.
2. Установить MSI на чистую VM, выполнить административную установку.
3. Проверить UIAccess: подтвердить запрос при первом запуске helper, переместить,
   закрепить и активировать elevated-окно; при отказе — убедиться в fallback.
4. Проверить контекстное меню проводника, скринсейвер, удаление и обновление
   поверх предыдущей версии.
5. Сравнить поведение с rc.8; зафиксировать отличия в `docs/release-checklist.md`.
6. Обновить `docs/signing.md` (реальный порядок подписи через SignPath вместо
   локального thumbprint), `README.md` и `docs/msix-store-plan.md` §7.2–7.3.
7. Опубликовать первый подписанный релиз и проверить SmartScreen на чистой VM.

Ворота G4: UIAccess подтверждён, чек-лист релиза обновлён, подписанный MSI
опубликован.

## 9. Этапы и оценки

| Этап | Работа | Дни | Ворота |
|---|---|---|---|
| M0 | `CODE_SIGNING.md`, README, MFA, роли, branch protection | 0.5–1 | G0: политика и роли готовы |
| M1 | Заявка и ответы ревью | 0.5 + ожидание | G1: отказ 23.11.2026 |
| M2 | Организация, проект, политика, artifact configuration | 1 | G2: конфигурация проходит пробный запрос |
| M3 | Релизный workflow, секреты, проверка подписи | 1 | G3: CI выдаёт подписанный MSI |
| M4 | Чистая VM, UIAccess, документация, релиз | 0.5–1 | G4: подпись и UIAccess приняты |

Инженерная оценка: **2.5–3.5 дня**. Календарный срок определяется внешним
ревью заявки; критический путь — G1.

## 10. Риски

| Риск | Вероятность | Влияние | Митигация |
|---|---|---|---|
| Отказ из-за prerelease-статуса | средняя | нет подписи | Опубликовать стабильный 1.0.0 до заявки |
| Вопросы к Pro Support как коммерческой деятельности | средняя | отказ | Явно указать: услуги поддержки, ПО бесплатно под GPL, закрытой сборки нет |
| Один мейнтейнер не устраивает ревьюеров | низкая | задержка | Формальные роли, CI-пользователь как submitter, Approver — человек |
| Ручное одобрение ломает автоматический релиз | высокая | релиз ждёт | Таймаут 1 час, дежурный Approver, позже вебхук |
| Неверные пути внутри MSI | средняя | ошибка конфигурации | Генерировать конфигурацию из sample MSI, проверять на пробном запросе |
| Сборка не проходит origin verification | низкая | подпись недоступна | Только GitHub-hosted runners, сборка из `main`, build-скрипты в репозитории |
| Утечка API token | низкая | компрометация подписи | Отдельный CI-пользователь, минимальные права, секрет GitHub, ротация |
| Подпись не даёт UIAccess | низкая | helper работает без UIAccess | Проверка на чистой VM; подпись — только одно из условий, второе — `Program Files` |
| Зависимость от SignPath Foundation | средняя | отзыв при нарушении правил | Соблюдать политику, резервный путь — Certum Open Source |
| Отказ по публичной известности | **реализовался 23.11.2026** | нет бесплатной подписи | Растить stars/forks/contributors, публикации и упоминания; подать повторно; OV-сертификат доступен только вне Беларуси |

## 11. Чек-лист

- [x] `CODE_SIGNING.md` создан, содержит обязательные формулировки.
- [x] В README есть раздел «Code signing policy» со ссылкой.
- [x] 2FA включена на GitHub; ruleset для `main` настроен.
- [x] Роли Author/Reviewer/Approver и signing submitter зафиксированы.
- [x] Заявка на `signpath.org/apply` отправлена 22.11.2026; 23.11.2026
      получен отказ по недостаточной публичной известности.
- [ ] Организация, проект, trusted build system и GitHub App настроены.
- [ ] Политика `release-signing`: approval, origin verification, ветка `main`.
- [ ] Artifact configuration проверена на sample MSI.
- [ ] Релизный workflow и секреты добавлены.
- [ ] `signtool verify /pa /all` проходит для MSI.
- [ ] UIAccess проверен на чистой VM.
- [ ] `docs/signing.md`, `docs/release-checklist.md`, `docs/msix-store-plan.md`
      обновлены.
- [ ] Первый подписанный релиз опубликован.

## 12. Источники

- Условия OSS-программы — `signpath.org/terms`, заявка — `signpath.org/apply`.
- GitHub Actions и origin verification — `docs.signpath.io/trusted-build-systems/github`,
  `docs.signpath.io/origin-verification`.
- Проекты, signing policies, deep signing — `docs.signpath.io/projects`,
  `docs.signpath.io/artifact-configuration/examples`.
- Требования к MSI/PE и форматы — `docs.signpath.io/artifact-configuration/reference`.
- Варианты подписи для Windows — `learn.microsoft.com/windows/apps/package-and-deploy/code-signing-options`.

## 13. После отказа 23.11.2026

Причина отказа — не качество кода, а недостаточная публичная известность.
SignPath не публикует порог, поэтому ориентир — устойчивые внешние сигналы:

- рост GitHub stars, forks и числа внешних contributors;
- статьи и обзоры вне репозитория: Habr, Reddit, YouTube, Stack Overflow;
- попадание в пакетные менеджеры (winget, Chocolatey, Scoop) и статистика
  загрузок;
- публичные issues/discussions и участие сообщества;
- повторная заявка через несколько месяцев после измеримого роста.

Альтернативы для подписи сейчас:

| Вариант | Цена | Publisher | Ограничения |
|---|---|---|---|
| Certum Standard OV, cloud | от €209/год (код от €139, set от €169) | физлицо или юрлицо | нет OSS-ограничения; UIAccess работает |
| Certum Open Source, cloud | €49/год | `Open Source Developer <Имя>` | только некоммерческое OSS; отзыв при коммерческом распространении |
| SignPath Starter + EV GlobalSign | от $500/год | юрлицо | EV-сертификат GlobalSign; нужна регистрация организации, уточнить у sales возможность своего OV |
| Ждать и переподать в Foundation | 0 | SignPath Foundation | бесплатно, но нужна публичная известность |

Для DxTools с платным Pro Support безопаснее Standard OV: он не содержит
запрета на коммерческое распространение и не привязывает publisher к префиксу
`Open Source Developer`. Certum Open Source дешевле, но перед покупкой нужно
письменное подтверждение Certum, что платная поддержка не считается
коммерческим распространением подписанного ПО.

**Ограничение для РФ/Беларуси:** Certum приостановил продажу и выдачу
квалифицированных и неквалифицированных сертификатов гражданам и компаниям
из РФ и Беларуси (официальное уведомление Certum от 2022 года; в 2026 году
ограничение сохраняется, reseller FAQ прямо перечисляет Россию). Поэтому
Certum Open Source и Standard OV доступны только при наличии ВНЖ/резидентства
вне РФ или иностранного юрлица. Подтверждать гражданство/резидентство и
возможность оплаты нужно до покупки.

Для Беларуси это означает, что Certum, DigiCert, GlobalSign и Sectigo
недоступны. Платный SignPath с EV GlobalSign и Microsoft Store/Partner Center
тоже под вопросом. Реалистичные пути: повторная заявка в SignPath Foundation
после роста публичной известности, иностранное юрлицо/ВНЖ или иностранный
со-мейнтейнер, который может быть subscriber сертификата.

## 14. План роста для повторной заявки

Цель — к повторной заявке иметь измеримые внешние сигналы. SignPath не
публикует точный порог, поэтому ориентиры:

- 100+ stars, 10+ forks, 3–5 внешних contributors;
- 2+ независимые публикации или видео (Habr, Reddit, YouTube, Softpedia);
- 1000+ скачиваний релизов и рост month-over-month;
- присутствие в winget, Chocolatey или Scoop;
- активные issues/discussions за последние 3 месяца.

Действия:

1. Добавить GitHub topics, скриншоты/GIF и короткое demo video.
2. Опубликовать пакеты в winget-pkgs, Chocolatey, Scoop.
3. Написать статью на Habr и англоязычный пост с реальным use case.
4. Отправить на обзор в 3–5 YouTube-каналов про Windows и мультимониторы.
5. Завести CONTRIBUTING, good first issues, discussions; отвечать на issues.
6. Вести статистику скачиваний и звёзд, фиксировать внешние упоминания.
7. Через 3–6 месяцев при достижении ориентиров подать заявку повторно.

Накрутка звёзд и фиктивные упоминания не используются: SignPath оценивает
реальное adoption, а фальшивые сигналы приводят к отказу и репутационному
риску.
