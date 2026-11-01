EN: Historical Junior acceptance report. Current installation/maintenance uses Docker/.NET, with no host Python requirement: [tools](DOCKER_TOOLS.md).

RU: Историческая приёмка Junior. Текущие установка/обслуживание — Docker/.NET, без Python хоста: [инструменты](DOCKER_TOOLS.md).

UA: Історичне приймання Junior. Поточні інсталяція/обслуговування — Docker/.NET, без Python хоста: [інструменти](DOCKER_TOOLS.md).

# Acceptance — 1.1.0.15 / Приёмка / Приймання

## English

Status: **automated local acceptance passed; MySQL Workbench GUI import remains pending**. This is not a claim that external deployment or every manual check is complete. Application 1.1.0.15 uses the same four migrations and Onion/OOP/SOLID boundaries as 1.1.0.14.

| Check | Actual result |
|---|---|
| Backend | 268 passed: 27 Domain/architecture, 84 Application, 56 Infrastructure/database, 101 HTTP |
| Frontend/builds | 26 tests; TypeScript/Vite and Docker builds passed |
| EF/export | No pending model changes; generated idempotent SQL matches shipped export except trailing whitespace |
| Schema stages | InitialDataModel → CommentThreads → ImageAttachments → TextAttachments preserved original fields, roots/replies and image bytes; all 3 populated-feature downgrade guards, safe empty-feature downs and re-upgrade passed in an owned DB |
| Existing checkpoints | All 3 restored with manifest/SHA-256/row-hash equality, upgraded and reapplied idempotently; includes retained 1,000,000 comments; original archives unchanged |
| Fresh installation | README startup sequence passed on a separate empty Compose project with 2 new isolated volumes; both databases migrated, comments_app was non-superuser/no DB/role creation |
| Current-data recovery | Fresh installation received real browser fixtures; restored all rows and 7 image/TXT attachments, served exact bytes through the application and retained them after container recreation |
| Browser | FileViewer, ResponsiveUi and Security suites passed in Edge; actual CAPTCHA, XHTML/preview, replies, JPG/TXT downloads, errors/drafts, EN/RU/UA, RTL, CSP/inline-script/frame blocking and desktop/mobile focus behavior |
| Pagination UI | 27 roots, 25 per page, LIFO, 6 sort directions, last/page jump, URL reload, draft retention, new-root reset and reply retention passed |
| Native HTTP | JSON 1 MiB known/chunked limits, total multipart 6 MiB limit, 400/413/415 and security headers passed |
| Schema projection | PostgreSQL agrees with Workbench SQL on 5 tables, 25 columns, 12 indexes, 4 FKs, 6 checks and all migration IDs |
| Corpus | All 18 cases ×3 samples passed on retained 800, 000 roots/ 200, 000 replies without reseeding |
| Preservation | All original rows/schema/history in 3 databases, corpus metadata, volumes, environment/checkpoint hashes and existing preview identity/state unchanged |
| Cleanup | Only owned fixtures, temporary restore databases, fresh project/network/2 volumes/image tag and temporary probe artifacts removed |

Browser widths were 320/360/390/640/768/1280 px. Read-only depth 15 remained readable; backend tests also cover deeper trees. The benchmark measured first/middle/last root pages for username/e-mail/date in both directions. Case medians ranged from 56.12 to 2715.08 ms; highest individual sample 3233.80 ms. These are local measurements, not a public concurrency SLA. [Machine-readable benchmark](acceptance/benchmark-1.1.0.15.json).

New portable commands: `scripts/verify-schema.py` compares catalog/projection without data writes; `scripts/verify-checkpoint.py DUMP --upgrade` hash-checks/restores into a unique temporary DB and verifies upgrade/idempotency without replacing the original dump or switching the app. A deliberately invalid manifest was rejected before DB creation. See [README](../README.md) and [recovery](database/MIGRATIONS.md).

Open item: GUI automation could not initialize and MySQL Workbench was not discoverable in the installed-app inventory. No `.mwb` GUI import is reported as passed; catalog comparison does not prove it. Open `database/workbench.sql` through File → Import → Reverse Engineer MySQL Create Script and inspect/save the diagram when Workbench is available. Manual screen-reader and Visual Studio GUI checks were not performed. External hosting/HTTPS is the next planned 1.1.0.16 stage and requires a target server. Existing anonymous/public-email and single-process CAPTCHA deployment limits remain described in [SECURITY.md](SECURITY.md).

## Русский

Статус: **автоматическая локальная приёмка прошла; GUI-импорт MySQL Workbench не подтверждён**. Это не означает завершение внешнего развёртывания или всех ручных проверок. Приложение 1.1.0.15 сохраняет четыре миграции и границы Onion/OOP/SOLID версии 1.1.0.14.

| Проверка | Фактический результат |
|---|---|
| Backend | 268 прошли: 27 Domain/архитектура, 84 Application, 56 Infrastructure/БД, 101 HTTP |
| Frontend/сборки | 26 тестов; TypeScript/Vite и Docker прошли |
| EF/экспорт | Изменений модели нет; идемпотентный SQL совпал с экспортом, кроме завершающих пробелов |
| Этапы схемы | InitialDataModel → CommentThreads → ImageAttachments → TextAttachments сохранили поля, корни/ответы и байты картинки; 3 запрета отката заполненной фичи, безопасные откаты пустых фич и повторное обновление прошли в собственной БД |
| Существующие копии | Все 3 восстановлены с проверкой manifest/SHA-256/хешей строк, обновлены и повторно применены идемпотентно; включён сохранённый 1 000 000 комментариев; исходные архивы прежние |
| Пустая установка | Команды README прошли на отдельном пустом Compose-проекте с 2 новыми изолированными томами; обе БД мигрированы, comments_app без superuser/создания БД/ролей |
| Текущее восстановление | На новой установке созданы реальные браузерные фикстуры; восстановлены строки и 7 изображений/TXT, приложение выдало точные байты и сохранило их после пересоздания контейнера |
| Браузер | FileViewer, ResponsiveUi и Security прошли в Edge; реальные CAPTCHA, XHTML/просмотр, ответы, скачивание JPG/TXT, ошибки/черновики, EN/RU/UA, RTL, CSP/запрет скриптов/фреймов и фокус desktop/mobile |
| UI пагинации | Прошли 27 корней, 25 на странице, LIFO, 6 сортировок, last/переход, URL reload, черновик, сброс нового корня и сохранение страницы ответа |
| Реальный HTTP | Прошли лимиты JSON 1 МиБ Content-Length/chunked, общего multipart 6 МиБ, 400/413/415 и заголовки |
| Проекция схемы | PostgreSQL совпал с Workbench SQL: 5 таблиц, 25 колонок, 12 индексов, 4 FK, 6 checks и ID миграций |
| Большой набор | Все 18 сценариев ×3 измерения прошли на прежних 800 000 корнях/200 000 ответах без генерации |
| Сохранность | Исходные строки/схемы/история 3 БД, метаданные набора, тома, хеши окружения/копий и ID/состояние просмотра не изменились |
| Удаление | Удалены только свои фикстуры, восстановленные БД, новый проект/сеть/2 тома/тег образа и временные проверочные файлы |

Ширины браузера: 320/360/390/640/768/1280 px. Текст на глубине 15 читался; backend-тесты покрывают более глубокие ветки. Замерены первая/средняя/последняя страницы корней имени/e-mail/даты в обе стороны. Медианы сценариев: 56.12–2715.08 мс; максимальное отдельное измерение: 3233.80 мс. Это локальные замеры, не публичный SLA одновременной нагрузки. [JSON замеров](acceptance/benchmark-1.1.0.15.json).

Новые переносимые команды: `scripts/verify-schema.py` сравнивает каталог/проекцию без записи данных; `scripts/verify-checkpoint.py DUMP --upgrade` проверяет хеш, восстанавливает только во временную БД с уникальным именем и проверяет обновление/идемпотентность, не заменяя исходную копию и подключение приложения. Намеренно неверный manifest отклонён до создания БД. См. [README](../README.RU.md) и [восстановление](database/MIGRATIONS.md).

Открытый пункт: инструменты GUI не инициализировались, MySQL Workbench не обнаружен среди установленных приложений. GUI-импорт `.mwb` не объявляется пройденным; сравнение каталога его не доказывает. Когда Workbench доступен, импортируйте `database/workbench.sql` через File → Import → Reverse Engineer MySQL Create Script, проверьте/сохраните диаграмму. Ручной экранный диктор и GUI Visual Studio не проверялись. Внешний хостинг/HTTPS — следующий плановый этап 1.1.0.16, нужен целевой сервер. Прежние ограничения анонимного приложения/публичного e-mail и CAPTCHA одного процесса описаны в [SECURITY.md](SECURITY.md).

## Українська

Статус: **автоматичне локальне приймання пройшло; GUI-імпорт MySQL Workbench не підтверджено**. Це не означає завершення зовнішнього розгортання або всіх ручних перевірок. Застосунок 1.1.0.15 зберігає чотири міграції та межі Onion/OOP/SOLID версії 1.1.0.14.

| Перевірка | Фактичний результат |
|---|---|
| Backend | 268 пройшли: 27 Domain/архітектура, 84 Application, 56 Infrastructure/БД, 101 HTTP |
| Frontend/збирання | 26 тестів; TypeScript/Vite та Docker пройшли |
| EF/експорт | Змін моделі немає; ідемпотентний SQL збігся з експортом, крім завершальних пробілів |
| Етапи схеми | InitialDataModel → CommentThreads → ImageAttachments → TextAttachments зберегли поля, корені/відповіді та байти зображення; 3 заборони відкочування заповненої фічі, безпечні відкочування порожніх фіч і повторне оновлення пройшли у власній БД |
| Наявні копії | Усі 3 відновлено з перевіркою manifest/SHA-256/хешів рядків, оновлено й повторно застосовано ідемпотентно; включено збережений 1 000 000 коментарів; початкові архіви попередні |
| Порожнє встановлення | Команди README пройшли на окремому порожньому Compose-проєкті з 2 новими ізольованими томами; обидві БД мігровано, comments_app без superuser/створення БД/ролей |
| Поточне відновлення | На новому встановленні створено реальні браузерні фікстури; відновлено рядки та 7 зображень/TXT, застосунок видав точні байти й зберіг їх після повторного створення контейнера |
| Браузер | FileViewer, ResponsiveUi та Security пройшли в Edge; справжні CAPTCHA, XHTML/перегляд, відповіді, завантаження JPG/TXT, помилки/чернетки, EN/RU/UA, RTL, CSP/заборона скриптів/фреймів та фокус desktop/mobile |
| UI пагінації | Пройшли 27 коренів, 25 на сторінці, LIFO, 6 сортувань, last/перехід, URL reload, чернетка, скидання нового кореня та збереження сторінки відповіді |
| Реальний HTTP | Пройшли межі JSON 1 МіБ Content-Length/chunked, загального multipart 6 МіБ, 400/413/415 та заголовки |
| Проєкція схеми | PostgreSQL збігся з Workbench SQL: 5 таблиць, 25 колонок, 12 індексів, 4 FK, 6 checks та ID міграцій |
| Великий набір | Усі 18 сценаріїв ×3 вимірювання пройшли на попередніх 800 000 коренях/200 000 відповідях без генерації |
| Збереження | Початкові рядки/схеми/історія 3 БД, метадані набору, томи, хеші оточення/копій та ID/стан перегляду не змінилися |
| Видалення | Видалено лише власні фікстури, відновлені БД, новий проєкт/мережу/2 томи/тег образу та тимчасові перевірочні файли |

Ширини браузера: 320/360/390/640/768/1280 px. Текст на глибині 15 читався; backend-тести покривають глибші гілки. Виміряно першу/середню/останню сторінки коренів імені/e-mail/дати в обидва боки. Медіани сценаріїв: 56.12–2715.08 мс; максимальне окреме вимірювання: 3233.80 мс. Це локальні вимірювання, не публічний SLA одночасного навантаження. [JSON вимірювань](acceptance/benchmark-1.1.0.15.json).

Нові переносимі команди: `scripts/verify-schema.py` порівнює каталог/проєкцію без запису даних; `scripts/verify-checkpoint.py DUMP --upgrade` перевіряє хеш, відновлює лише в тимчасову БД з унікальною назвою й перевіряє оновлення/ідемпотентність, не замінюючи початкову копію та підключення застосунку. Навмисно хибний manifest відхилено до створення БД. Див. [README](../README.UA.md) та [відновлення](database/MIGRATIONS.md).

Відкритий пункт: інструменти GUI не ініціалізувалися, MySQL Workbench не знайдено серед установлених застосунків. GUI-імпорт `.mwb` не оголошено пройденим; порівняння каталогу його не доводить. Коли Workbench доступний, імпортуйте `database/workbench.sql` через File → Import → Reverse Engineer MySQL Create Script, перевірте/збережіть діаграму. Ручний екранний диктор та GUI Visual Studio не перевіряли. Зовнішній хостинг/HTTPS — наступний плановий етап 1.1.0.16, потрібен цільовий сервер. Попередні обмеження анонімного застосунку/публічного e-mail та CAPTCHA одного процесу описано в [SECURITY.md](SECURITY.md).
