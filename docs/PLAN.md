# CommentsService — Development plan

## Status and version rules

Completed correction: `CommentsService/v_1.1.0.6_HomepageHint` clarifies homepage hints/errors in EN/RU/UA; version/schema unchanged.

The approved sequence starts at `CommentsService/v_1.1.0.1_Init`.
Current milestone: 1.1.0.12 FileViewer completed. Onion architecture is preserved. Next: 1.1.0.13 ResponsiveUi.
Keep w=1, x=1, y=0; increment only revision z. Configuration extraction is deferred.
Each completed iteration is pushed to its own branch with current read.me,
README and change.log. README.md is English, README.RU.md Russian, README.UA.md
Ukrainian. Change logs: change.log (EN), change.log.ru (RU), change.log.ua (UA).
Other shared supporting documents use English, Russian and Ukrainian, in that order.
The new change.log starts with Init; it must record actual work, not simulated history.

## Database lifecycle — applies to every iteration

1. Use persistent development and integration-test databases on separate storage.
   Keep the same database and volume across application updates. Do not drop,
   recreate or truncate the database as routine test setup/teardown.
2. Evolve the schema through versioned EF Core migrations. Record the migration
   identifier and upgrade path for each application version that changes the schema.
   Code/document-only revisions do not require a new database copy or migration.
3. Before changing the schema or existing data, create a consistent recovery copy
   of the current database and verify that it is usable. The steady-state retention
   is the current database plus one previous recovery point, not a database clone
   for every application revision. Replace the older recovery point only after
   the new copy is verified; never touch backups belonging to other projects.
4. Test the upgrade from the previous schema on retained data. Check relationships,
   root/reply links, attachments and representative records, not just row counts.
5. Keep integration-test fixtures isolated from development and large data sets.
   Use transaction rollback where the tested components can share a transaction.
   For HTTP tests using separate connections, clean up only records owned by that
   test run in foreign-key-safe order. Never truncate shared tables or delete all authors/comments.
6. Keep a persistent large-data test set separate from fixture-based correctness
   tests. Generate up to one million comments once, when pagination is available,
   then reuse that set. Version/idempotency checks must prevent reseeding on every
   test run. Apply later migrations/backfills to the existing set.
7. Reuse the same data for comparable query/pagination measurements; record the
   dataset and schema versions. Correctness tests must not mutate the benchmark baseline.
8. A blank temporary database is a separate installation check, used deliberately
   to verify initial migrations and README startup. It is not the default database
   lifecycle for development, integration tests or performance measurements.
9. Rollback must be planned for each schema change. Use a verified reverse migration
   only where it preserves data; otherwise restore the previous recovery point.
   A restore returns data to that checkpoint, not to a later post-backup state.

## Iteration branches

| Branch | Scope and database expectations |
|---|---|
| `CommentsService/v_1.1.0.1_Init` | Minimal .NET/React skeleton, English identity, build instructions and initial release documents. No comments functionality or database reset scripts. |
| `CommentsService/v_1.1.0.2_Docker` | Completed. Container builds and PostgreSQL, persistent storage, readiness checks. Bootstrap the development/test databases once and preserve them afterwards. |
| `CommentsService/v_1.1.0.3_DataModel` | Completed. OOP entities, EF mappings, initial migration, diagrams and migration tracking. Establish previous-state recovery and retained-data upgrade checks. |
| `CommentsService/v_1.1.0.3_BackendLayers` | Architecture correction for 1.1.0.3: Domain, Infrastructure and API projects; entity configurations, interfaces, domain invariants and architecture tests. Same schema and migration; next planned version stays 1.1.0.4. |
| `CommentsService/v_1.1.0.3_OnionArchitecture` | Onion correction: Application ports, Infrastructure adapters, standard Health Checks, inward dependency tests and Persistence/Migrations. Same version/schema/history; comment use cases remain in 1.1.0.4. |
| `CommentsService/v_1.1.0.4_CommentsApi` | Completed. Create/read plain-text comments, author/client metadata, timestamps and server validation. Integration tests use the persistent test DB with owned fixtures/rollback. |
| `CommentsService/v_1.1.0.5_CommentsUi` | SPA form/table, client validation, async submission and multilingual text. Browser checks use isolated fixtures and preserve unrelated data. |
| `CommentsService/v_1.1.0.6_XhtmlEditor` | XHTML allowlist, safe links, nesting, toolbar and server-validated preview. Existing comments remain intact. |
| `CommentsService/v_1.1.0.7_CommentThreads` | Replies and parent/root relationships, cascading display. Any schema additions use a data-preserving migration; verify existing roots/replies. |
| `CommentsService/v_1.1.0.8_SortingPagination` | Six sort directions, LIFO and 25 roots per page. Prepare the persistent large-data set once, then measure and reuse it rather than rebuilding it. |
| `CommentsService/v_1.1.0.9_Captcha` | Image, required code, refresh, expiry and single-use verification. Reuse databases; authentication test fixtures must not reset comments. |
| `CommentsService/v_1.1.0.10_ImageAttachments` | JPG/GIF/PNG validation, proportional 320×240 resizing and storage. Add attachment schema by migration without recreating existing comment data. |
| `CommentsService/v_1.1.0.11_TextAttachments` | UTF-8 TXT up to 100 KiB and safe text delivery. Test owned files/records without clearing the persistent corpus. |
| `CommentsService/v_1.1.0.12_FileViewer` | Image/text modal viewer, effects, keyboard controls and downloads. No database reset. |
| `CommentsService/v_1.1.0.13_ResponsiveUi` | Final CSS, mobile layout, accessibility, right-to-left text and English messages. No database reset. |
| `CommentsService/v_1.1.0.14_SecurityReview` | XSS/SQL-injection and upload checks, headers/limits and consistent validation. Run negative cases in the isolated test DB, preserving the large baseline. |
| `CommentsService/v_1.1.0.15_Acceptance` | Full checklist/browser checks; upgrades across database-changing versions on preserved data; recovery rehearsal; measurements on the retained large set; separate blank-DB installation check; final Workbench/schema verification. |
| `CommentsService/v_1.1.0.16_Deployment` | Deploy the accepted version, migrate the target database with a prior recovery point, verify HTTPS/external access/persistence and update user/deployment docs. An available server is required. |

Relevant checks accompany every step. Additional fixes use a separate branch with the same version and a different
description. Planned subsequent version numbers do not shift.
This retains the Junior feature scope; the large data set adds persistence and
query checks, not the brokers/NoSQL/cloud architecture of higher assignment levels.

## Evidence required before publishing a version

- Working implementation of the declared scope and accurate test results.
- For database changes: migration IDs, supported previous state, successful upgrade
  on retained data and recovery procedure. No automatic data-loss migration.
- For data-set checks: stable seed/dataset identity and no routine regeneration.
- Current read.me, README and change.log for that branch in English, Russian and Ukrainian.
- Verified remote commit; keep earlier iteration branches available.
- Local workstation instructions remain ignored and do not enter Git/Docker context.

---

# CommentsService — План разработки

## Статус и правила версий

Завершена доработка `CommentsService/v_1.1.0.6_HomepageHint`: понятные подсказки/ошибки сайта на EN/RU/UA; версия/схема прежние.

Согласованная последовательность начинается с `CommentsService/v_1.1.0.1_Init`.
Текущий этап 1.1.0.12 FileViewer завершён. Onion сохранён. Далее — 1.1.0.13 ResponsiveUi.
w=1, x=1, y=0 сохраняются, увеличивается только ревизия z. Вынос в конфиг отложен.
Каждая завершённая итерация отправляется в свою ветку с актуальными read.me,
README и change.log. README.md — английский, README.RU.md — русский, README.UA.md —
украинский. Журналы: change.log (EN), change.log.ru (RU), change.log.ua (UA).
В остальных общих документах сначала английский, затем русский и украинский.
Новый change.log начинается с Init и отражает реально выполненную работу.

## Жизненный цикл БД — для каждой итерации

1. Постоянные БД разработки и интеграционных тестов изолированы друг от друга.
   При обновлении приложения сохраняем БД и тома. Не используем удаление,
   пересоздание или полную очистку БД как обычную подготовку/завершение тестов.
2. Схема развивается версионированными миграциями EF Core. Для каждой версии,
   меняющей схему, фиксируем миграцию и путь обновления. Изменение только кода
   или документации не требует новой копии БД или фиктивной миграции.
3. Перед изменением схемы или существующих данных создаём согласованную
   восстановительную копию и проверяем её пригодность. Постоянно храним текущую
   БД и одну предыдущую контрольную копию, а не клон для каждой ревизии приложения.
   Старую копию заменяем только после проверки новой. Чужие backup не трогаем.
4. Обновление с предыдущей схемы проверяем на сохранённых данных: не только
   количество строк, но и связи, корни/ответы, вложения и контрольные записи.
5. Тестовые фикстуры изолированы от рабочих и больших наборов. Где компоненты
   могут использовать общую транзакцию, применяем откат. Для HTTP-тестов с разными
   соединениями удаляем только принадлежащие запуску записи в безопасном порядке
   зависимостей. Общий TRUNCATE и удаление всех авторов/комментариев запрещены.
6. Большой постоянный набор отделён от функциональных фикстур. До миллиона
   комментариев генерируем один раз, когда готова пагинация, и используем повторно.
   Идентификатор/версия набора и идемпотентная подготовка исключают повторную
   генерацию при каждом тесте. Последующие миграции применяем к существующему набору.
7. Для сравнения запросов и пагинации используем один и тот же набор и фиксируем
   версии данных/схемы. Функциональные тесты не меняют основу нагрузочных замеров.
8. Пустая временная БД — отдельная проверка установки и первоначальных миграций
   по README. Это не обычный жизненный цикл БД разработки, интеграционных тестов
   или нагрузочных замеров.
9. Для каждого изменения схемы планируем откат. Обратную миграцию используем
   только после проверки сохранности данных; иначе восстанавливаем предыдущую
   копию. Восстановление возвращает состояние на момент контрольной точки,
   а не изменения, сделанные после неё.

## Ветки итераций

Названия веток приведены в английской таблице выше; ниже — содержание по версиям.

| Версия | Содержание и работа с БД |
|---|---|
| **1.1.0.1 Init** | Каркас .NET/React, английское название, сборка и начальные документы. Комментариев и скриптов сброса БД ещё нет. |
| **1.1.0.2 Docker** | Завершено. Контейнеры, PostgreSQL, постоянные тома и готовность. БД разработки/тестов создаются один раз и сохраняются. |
| **1.1.0.3 DataModel** | Завершено. Сущности, EF, первая миграция, схемы и учёт миграций. Закладываем предыдущую восстановительную копию и проверки обновления на данных. |
| **1.1.0.3 BackendLayers** | Исправление архитектуры 1.1.0.3: проекты Domain, Infrastructure и API; конфигурации сущностей, интерфейсы, доменные инварианты и архитектурные тесты. Схема и миграция прежние; следующий этап остаётся 1.1.0.4. |
| **1.1.0.3 OnionArchitecture** | Исправление Onion: контракты Application, адаптеры Infrastructure, стандартные Health Checks, проверки зависимостей внутрь и Persistence/Migrations. Версия/схема/история прежние; сценарии комментариев — этап 1.1.0.4. |
| **1.1.0.4 CommentsApi** | Завершено. Запись/чтение текста, данные автора/клиента, время и валидация. Проверки в постоянной тестовой БД с откатом или собственными фикстурами. |
| **1.1.0.5 CommentsUi** | SPA-форма/таблица, валидация, AJAX и любой язык текста. Браузерные фикстуры не затрагивают чужие данные. |
| **1.1.0.6 XhtmlEditor** | Разрешённые теги, безопасные ссылки, вложенность, кнопки и предпросмотр. Существующие комментарии сохраняются. |
| **1.1.0.7 CommentThreads** | Ответы, корни/родители и каскад. Новые поля — миграцией без пересоздания данных; проверяем существующие связи. |
| **1.1.0.8 SortingPagination** | Все сортировки, LIFO и страницы по 25 корней. Один раз готовим большой постоянный набор, затем повторяем замеры на нём. |
| **1.1.0.9 Captcha** | Картинка, код, обновление, срок и одноразовая проверка. Фикстуры не сбрасывают комментарии или БД. |
| **1.1.0.10 ImageAttachments** | JPG/GIF/PNG, проверка, уменьшение до 320×240 и хранение. Таблицы вложений добавляются миграцией с сохранением комментариев. |
| **1.1.0.11 TextAttachments** | UTF-8 TXT до 100 КБ, безопасная выдача. Проверяем собственные записи/файлы без очистки постоянного массива. |
| **1.1.0.12 FileViewer** | Просмотр изображений/текста, эффекты, клавиатура и скачивание. БД не сбрасывается. |
| **1.1.0.13 ResponsiveUi** | CSS, мобильная вёрстка, доступность, RTL и английские сообщения. БД не сбрасывается. |
| **1.1.0.14 SecurityReview** | XSS/SQL-инъекции, файлы, заголовки, ограничения и валидация. Негативные тесты изолированы от большого набора. |
| **1.1.0.15 Acceptance** | Полная приёмка; обновление между версиями схемы на сохранённых данных; пробное восстановление; замеры большого набора; отдельно установка на пустую БД; проверка схем и Workbench. |
| **1.1.0.16 Deployment** | Развёртывание, миграция целевой БД после контрольной копии, HTTPS, внешняя проверка и документы. Нужен доступный сервер. |

Необходимые проверки сопровождают каждый шаг. Отдельная доработка выполняется в ветке той же версии с другим описанием;
номера следующих плановых итераций не сдвигаются. Функциональный объём
остаётся Junior: большой набор добавляет проверки сохранности и запросов,
а не брокеры/NoSQL/облачную архитектуру старших уровней задания.

## Что подтверждаем перед публикацией версии

- Работает заявленная функциональность, результаты проверок записаны достоверно.
- Для изменения БД указаны миграции, предыдущая схема, проверка обновления на данных
  и процедура восстановления. Автоматические миграции с потерей данных не допускаются.
- Для большого набора сохраняется идентификатор/версия; регулярной перегенерации нет.
- read.me, README и change.log актуальны для ветки на английском, русском и украинском.
- Удалённый коммит проверен, предыдущие ветки итераций доступны.
- Локальные инструкции компьютера остаются вне Git и Docker context.

---

# CommentsService — План розробки

## Статус і правила версій

Завершено доопрацювання `CommentsService/v_1.1.0.6_HomepageHint`: зрозумілі підказки/помилки сайту на EN/RU/UA; версія/схема попередні.

Погоджена послідовність починається з `CommentsService/v_1.1.0.1_Init`.
Поточний етап 1.1.0.12 FileViewer завершено. Onion збережено. Далі — 1.1.0.13 ResponsiveUi.
w=1, x=1, y=0 зберігаються, збільшується лише ревізія z. Винесення даних у конфігурацію відкладено.
Кожна завершена ітерація публікується у своїй гілці з актуальними read.me,
README і change.log. README.md — англійською, README.RU.md — російською, README.UA.md —
українською. Журнали: change.log (EN), change.log.ru (RU), change.log.ua (UA).
Решта спільних документів містить англійський, російський і український текст у цьому порядку.
Новий change.log починається з Init і відображає реально виконану роботу.

## Життєвий цикл БД — для кожної ітерації

1. Постійні БД розробки та інтеграційних тестів ізольовані одна від одної.
   Під час оновлення зберігаємо БД і томи. Не видаляємо, не створюємо заново
   та не очищуємо всю БД як звичайну підготовку або завершення тестів.
2. Схема розвивається версіонованими міграціями EF Core. Для кожної версії зі зміною
   схеми фіксуємо міграцію та шлях оновлення. Зміни лише коду або документації
   не потребують нової копії БД чи штучної міграції.
3. Перед зміною схеми або наявних даних створюємо узгоджену відновлювальну копію
   та перевіряємо її придатність. Постійно зберігаємо поточну БД і одну попередню
   контрольну копію, а не клон для кожної ревізії. Стару копію замінюємо лише після
   перевірки нової. Резервні копії інших проєктів не змінюємо.
4. Оновлення попередньої схеми перевіряємо на збережених даних: кількість рядків,
   зв’язки, кореневі коментарі/відповіді, вкладення та контрольні записи.
5. Тестові фікстури ізольовані від робочих і великих наборів. За можливості спільної
   транзакції застосовуємо відкат. Для HTTP-тестів із різними з’єднаннями видаляємо
   лише власні записи запуску в безпечному порядку залежностей. Загальний TRUNCATE
   і видалення всіх авторів/коментарів заборонені.
6. Великий постійний набір відокремлений від функціональних фікстур. До мільйона
   коментарів генеруємо один раз після реалізації пагінації та використовуємо повторно.
   Ідентифікатор/версія набору й ідемпотентна підготовка запобігають повторній генерації
   під час кожного тесту. Наступні міграції застосовуємо до наявного набору.
7. Для порівняння запитів і пагінації використовуємо той самий набір та фіксуємо
   версії даних/схеми. Функціональні тести не змінюють основу навантажувальних вимірювань.
8. Порожня тимчасова БД потрібна для окремої перевірки встановлення й початкових
   міграцій за README. Це не звичайний життєвий цикл БД розробки, інтеграційних
   тестів або навантажувальних вимірювань.
9. Для кожної зміни схеми плануємо відкат. Зворотну міграцію використовуємо лише
   після перевірки збереження даних; інакше відновлюємо попередню копію. Відновлення
   повертає стан контрольної точки, а не зміни, зроблені після неї.

## Гілки ітерацій

Назви гілок наведено в англійській таблиці вище; нижче — зміст за версіями.

| Версія | Зміст і робота з БД |
|---|---|
| **1.1.0.1 Init** | Каркас .NET/React, англомовна назва, збирання й початкові документи. Функцій коментарів і скриптів скидання БД ще немає. |
| **1.1.0.2 Docker** | Завершено. Контейнери, PostgreSQL, постійні томи та готовність. БД розробки/тестів створюються один раз і зберігаються. |
| **1.1.0.3 DataModel** | Завершено. Сутності, EF, перша міграція, схеми й облік міграцій. Попередня відновлювальна копія та перевірки оновлення на даних. |
| **1.1.0.3 BackendLayers** | Виправлення архітектури 1.1.0.3: проєкти Domain, Infrastructure та API; конфігурації сутностей, інтерфейси, доменні інваріанти й архітектурні тести. Схема й міграція попередні; наступний етап залишається 1.1.0.4. |
| **1.1.0.3 OnionArchitecture** | Виправлення Onion: контракти Application, адаптери Infrastructure, стандартні Health Checks, перевірки залежностей усередину та Persistence/Migrations. Версія/схема/історія попередні; сценарії коментарів — етап 1.1.0.4. |
| **1.1.0.4 CommentsApi** | Завершено. Запис/читання тексту, дані автора/клієнта, час і валідація. Постійна тестова БД із відкатом або власними фікстурами. |
| **1.1.0.5 CommentsUi** | SPA-форма/таблиця, валідація, AJAX і повідомлення будь-якою мовою. Браузерні фікстури не змінюють сторонні дані. |
| **1.1.0.6 XhtmlEditor** | Дозволені теги, безпечні посилання, вкладеність, кнопки та попередній перегляд. Наявні коментарі зберігаються. |
| **1.1.0.7 CommentThreads** | Відповіді, корені/батьківські записи та каскад. Нові поля додаються міграцією зі збереженням даних; зв’язки перевіряються. |
| **1.1.0.8 SortingPagination** | Усі сортування, LIFO і сторінки по 25 кореневих записів. Великий постійний набір готується один раз для повторних вимірювань. |
| **1.1.0.9 Captcha** | Зображення, код, оновлення, строк дії й одноразова перевірка. Фікстури не скидають коментарі або БД. |
| **1.1.0.10 ImageAttachments** | JPG/GIF/PNG, перевірка, зменшення до 320×240 і зберігання. Таблиці вкладень додаються міграцією зі збереженням коментарів. |
| **1.1.0.11 TextAttachments** | UTF-8 TXT до 100 КБ і безпечна видача. Перевіряємо власні записи/файли без очищення постійного набору. |
| **1.1.0.12 FileViewer** | Перегляд зображень/тексту, ефекти, клавіатура та завантаження. БД не скидається. |
| **1.1.0.13 ResponsiveUi** | CSS, мобільна верстка, доступність, RTL і англомовні повідомлення. БД не скидається. |
| **1.1.0.14 SecurityReview** | XSS/SQL-ін’єкції, файли, заголовки, обмеження та валідація. Негативні тести ізольовані від великого набору. |
| **1.1.0.15 Acceptance** | Повна перевірка; міграції між версіями на збережених даних; пробне відновлення; вимірювання великого набору; окремо порожня БД; схеми та Workbench. |
| **1.1.0.16 Deployment** | Розгортання, міграція цільової БД після контрольної копії, HTTPS, зовнішня перевірка та документи. Потрібен доступний сервер. |

Необхідні перевірки супроводжують кожен крок. Окреме доопрацювання виконується в гілці тієї самої версії з іншим описом;
номери наступних планових ітерацій не зсуваються. Функціональний обсяг залишається
Junior: великий набір додає перевірки збереження та запитів, а не брокери/NoSQL/
хмарну архітектуру старших рівнів завдання.

## Що підтверджуємо перед публікацією версії

- Заявлена функціональність працює, результати перевірок записано достовірно.
- Для змін БД указано міграції, попередню схему, оновлення на даних і процедуру
  відновлення. Автоматичні міграції з утратою даних не допускаються.
- Зберігається ідентифікатор/версія великого набору; регулярної повторної генерації немає.
- read.me, README і change.log актуальні для гілки англійською, російською та українською.
- Віддалений коміт перевірено, попередні гілки доступні.
- Локальні інструкції комп’ютера залишаються поза Git і Docker context.


## English

TXT attachments (1.1.0.11): roots and replies accept one optional attachment: an image OR a `.txt` file. TXT must contain 1..102400 bytes (100 KiB), strict UTF-8; BOM, Unicode, CR/LF and tabs are preserved byte-for-byte. Invalid encoding and control characters other than CR/LF/tab are rejected; HTML/script text is accepted as literal text. Filename/MIME do not establish content validity. Multipart uses `textFile` with the usual comment/CAPTCHA fields; never send both file parts. JSON text-only creation remains available. Invalid TXT returns 400/errors.textFile. CAPTCHA is consumed before content processing; retry with a fresh code. The localized picker keeps draft/file on errors, clears on success and permits removing/replacing a selection.

DTOs add nullable `textFile: {id,fileName,sizeBytes}`; list/page/reply projections omit file bytes. `GET /api/text-files/{id}` downloads exact bytes as `text/plain; charset=utf-8`, generated UUID `.txt` filename, attachment disposition, nosniff, no-store and sandbox CSP; missing files return 404. HTML is never rendered. FileViewer is available in 1.1.0.12.

Upgrade 1.1.0.10 → 1.1.0.11: stop all writers, restore-verify a current checkpoint (include the benchmark when present), build, explicitly migrate every used database, then restart using the existing volumes. Migration `20261030130243_TextAttachments` adds TextAttachments: Id, unique CommentId FK CASCADE, sanitized FileName varchar128, SizeBytes, Data bytea; checks enforce 1..102400 bytes and matching size. Existing authors/comments/images and migration IDs are preserved. Down refuses while TXT rows exist. Bytes and comment are committed atomically in the same PostgreSQL database/backup; no filesystem volume. Preserve verified pre-upgrade copies, accounting for changes after their checkpoint.

Onion: Domain enforces immutable ownership/defensive bytes and one attachment per comment; Application defines processing/read ports and coordinates the transaction; Infrastructure validates UTF-8 and implements EF storage/queries; API binds streams and sets download headers. No new external dependency.

TextAttachments 1.1.0.11 verification: 235 backend tests (27 Domain,84 Application,56 Infrastructure/database,68 HTTP),16 frontend tests, frontend/Docker builds, restore upgrade/downgrade round trip and populated-TXT downgrade guard passed. Edge desktop/360px passed real CAPTCHA, TXT root/reply, boundary size, exact Unicode/BOM/CRLF/HTML download, restart persistence, safe headers, rejection/draft retention/removal, exclusive selection and EN/RU/UA without JavaScript errors. All 18 retained-million-corpus paging scenarios passed without reseeding; original row fingerprints, metadata, volumes and restore-verified checkpoints were retained. GUI import in VS/Workbench is not checked.

## Русский

TXT-вложения (1.1.0.11): корень и ответ принимают одно необязательное вложение: картинку ИЛИ `.txt`. TXT — 1..102400 байт (100 КиБ), строгий UTF-8; BOM, Unicode, CR/LF и табуляция сохраняются побайтно. Неверная кодировка и управляющие символы, кроме CR/LF/tab, отклоняются; HTML/script допускается как обычный текст. Имя/MIME не доказывают корректность содержимого. Multipart использует `textFile` с обычными полями комментария/CAPTCHA; два файла запрещены. JSON без файла сохранён. Неверный TXT: 400/errors.textFile. CAPTCHA расходуется перед проверкой содержимого; повтор требует нового кода. Локализованная форма сохраняет черновик/файл при ошибках, очищает после успеха, позволяет убрать/заменить файл.

DTO добавляет nullable `textFile: {id,fileName,sizeBytes}`; списки/страницы/ответы не содержат байты. `GET /api/text-files/{id}` скачивает точные байты с `text/plain; charset=utf-8`, UUID-именем `.txt`, attachment, nosniff, no-store и sandbox CSP; отсутствующий файл — 404. HTML не отображается. Просмотр с эффектами реализован в 1.1.0.12.

Обновление 1.1.0.10 → 1.1.0.11: остановить все записи, проверить восстановление текущей копии (включая benchmark, если существует), собрать, явно мигрировать все используемые БД и запустить прежние тома. Миграция `20261030130243_TextAttachments` добавляет TextAttachments: Id, уникальный CommentId FK CASCADE, очищенное FileName varchar128, SizeBytes, Data bytea; проверки 1..102400 байт и совпадения размера. Авторы/комментарии/картинки и прежние ID миграций сохранены. Down запрещён при наличии TXT. Файл и комментарий записываются одной транзакцией в PostgreSQL и общую копию; файлового тома нет. Сохраните проверенные копии до обновления, учитывая изменения после контрольной точки.

Onion: Domain защищает принадлежность/байты и одно вложение; Application задаёт контракты обработки/чтения и транзакцию; Infrastructure проверяет UTF-8 и реализует EF; API связывает потоки и задаёт заголовки скачивания. Новых внешних зависимостей нет.

Проверки выпуска TextAttachments 1.1.0.11: 235 backend (27 Domain,84 Application,56 Infrastructure/БД,68 HTTP),16 frontend, сборки frontend/Docker, обновление/откат восстановленной копии и запрет отката с TXT прошли. Edge десктоп/360px прошёл реальную CAPTCHA, TXT корня/ответа, граничный размер, точные Unicode/BOM/CRLF/HTML-байты при скачивании и после перезапуска, заголовки, отказы/черновик/удаление, выбор одного файла и EN/RU/UA без ошибок JavaScript. Все 18 сценариев сохранённого миллиона прошли без пересоздания; отпечатки исходных строк, метаданные, тома и проверенные восстановлением копии сохранены. GUI VS/Workbench не проверены.

## Українська

TXT-вкладення (1.1.0.11): корінь і відповідь приймають одне необов’язкове вкладення: зображення АБО `.txt`. TXT — 1..102400 байтів (100 КіБ), строгий UTF-8; BOM, Unicode, CR/LF і табуляція зберігаються побайтово. Хибне кодування та керівні символи, крім CR/LF/tab, відхиляються; HTML/script дозволено як звичайний текст. Назва/MIME не доводять коректність вмісту. Multipart використовує `textFile` зі звичайними полями коментаря/CAPTCHA; два файли заборонено. JSON без файла збережено. Хибний TXT: 400/errors.textFile. CAPTCHA витрачається до перевірки вмісту; повтор потребує нового коду. Локалізована форма зберігає чернетку/файл після помилок, очищає після успіху, дозволяє прибрати/замінити файл.

DTO додає nullable `textFile: {id,fileName,sizeBytes}`; списки/сторінки/відповіді не містять байтів. `GET /api/text-files/{id}` завантажує точні байти з `text/plain; charset=utf-8`, UUID-назвою `.txt`, attachment, nosniff, no-store та sandbox CSP; відсутній файл — 404. HTML не відображається. Перегляд з ефектами реалізовано в 1.1.0.12.

Оновлення 1.1.0.10 → 1.1.0.11: зупинити всі записи, перевірити відновлення поточної копії (включно з benchmark, якщо існує), зібрати, явно мігрувати всі використані БД і запустити попередні томи. Міграція `20261030130243_TextAttachments` додає TextAttachments: Id, унікальний CommentId FK CASCADE, очищене FileName varchar128, SizeBytes, Data bytea; перевірки 1..102400 байтів і збігу розміру. Авторів/коментарі/зображення й попередні ID міграцій збережено. Down заборонено за наявності TXT. Файл і коментар записуються однією транзакцією в PostgreSQL і спільну копію; файлового тому немає. Збережіть перевірені копії до оновлення, враховуючи зміни після контрольної точки.

Onion: Domain захищає належність/байти й одне вкладення; Application задає контракти обробки/читання та транзакцію; Infrastructure перевіряє UTF-8 і реалізує EF; API зв’язує потоки та задає заголовки завантаження. Нових зовнішніх залежностей немає.

Перевірки випуску TextAttachments 1.1.0.11: 235 backend (27 Domain,84 Application,56 Infrastructure/БД,68 HTTP),16 frontend, збирання frontend/Docker, оновлення/відкат відновленої копії та заборона відкату з TXT пройшли. Edge десктоп/360px пройшов реальну CAPTCHA, TXT кореня/відповіді, граничний розмір, точні Unicode/BOM/CRLF/HTML-байти під час завантаження й після перезапуску, заголовки, відмови/чернетку/видалення, вибір одного файла та EN/RU/UA без помилок JavaScript. Усі 18 сценаріїв збереженого мільйона пройшли без повторного створення; відбитки початкових рядків, метадані, томи та перевірені відновленням копії збережено. GUI VS/Workbench не перевірено.


## FileViewer 1.1.0.12 — EN / RU / UA

FileViewer (1.1.0.12): click an image or TXT filename to open a modal with fade/scale effects. Previous/Next and Left/Right cycle through attachments in currently visible comments, including expanded replies; hidden/unloaded replies and other pages are not fetched. Escape, Close viewer or the backdrop closes it. Focus stays inside and returns to the opener; background scrolling is locked. Reduced-motion preferences disable animations. Downloads preserve the stored normalized PNG or the original TXT bytes, using generated UUID filenames. TXT is decoded as strict UTF-8 and rendered as literal text, never HTML; a leading BOM is not displayed. Loading, failure and Retry states preserve the comment draft. Requests are bounded and canceled on navigation/close; temporary image URLs are revoked.

`GET /api/images/{id}?download=true` adds attachment disposition; without it the existing inline PNG route is unchanged. TXT continues to use `/api/text-files/{id}` with its safe headers and exact bytes. FileClient separates the viewer from HTTP; backend Domain/Application/Infrastructure boundaries and Onion/OOP/SOLID remain intact. No new dependency or migration. Upgrade 1.1.0.11 → 1.1.0.12 by rebuilding/restarting the app with the same environment, Compose overrides and volumes; retain existing databases and verified checkpoints. Latest migration remains `20261030130243_TextAttachments`.

Validation for 1.1.0.12: 235 backend tests (27/84/56/68), 23 frontend tests, frontend/Docker builds and Edge desktop/360px passed. Browser coverage includes real CAPTCHA-backed image/TXT/reply fixtures, 100 KiB, literal Unicode/HTML, exact downloads, arrows/wrap, close paths, focus, draft retention, 404/503/retry, stale requests, URL cleanup, EN/RU/UA, reduced motion and reload. All 18 paging scenarios passed on the retained million-comment corpus without reseeding. All three databases, volumes and checkpoint hashes were preserved. No migration/restore rehearsal was needed for this code-only change. VS/Workbench GUI was not checked. Next: 1.1.0.13 ResponsiveUi.

FileViewer (1.1.0.12): нажмите на изображение или имя TXT, чтобы открыть окно с плавным появлением и масштабированием. «Предыдущий/Следующий» и стрелки влево/вправо переключают вложения видимых комментариев, включая раскрытые ответы; скрытые ответы и другие страницы не загружаются. Escape, кнопка закрытия или фон закрывают окно. Фокус удерживается внутри и возвращается к исходной кнопке; прокрутка фона блокируется. Предпочтение уменьшенного движения отключает анимацию. Скачивание сохраняет нормализованный PNG или исходные байты TXT с UUID-именем. TXT декодируется строгим UTF-8 и выводится обычным текстом, без исполнения HTML; начальный BOM не отображается. Загрузка, ошибка и повтор сохраняют черновик. Запросы ограничены и отменяются при переключении/закрытии; временные URL изображений освобождаются.

`GET /api/images/{id}?download=true` добавляет attachment; прежний inline PNG без параметра сохранён. TXT использует `/api/text-files/{id}` с прежними безопасными заголовками и точными байтами. FileClient отделяет окно от HTTP; границы Domain/Application/Infrastructure и Onion/OOP/SOLID сохранены. Новых зависимостей и миграций нет. Обновление 1.1.0.11 → 1.1.0.12: пересоберите/перезапустите приложение с прежним окружением, Compose overrides и томами; сохраните БД и проверенные копии. Последняя миграция — `20261030130243_TextAttachments`.

Проверки 1.1.0.12: прошли 235 backend-тестов (27/84/56/68), 23 frontend, сборки frontend/Docker и Edge desktop/360px. Браузер проверил реальные фикстуры изображения/TXT/ответа с CAPTCHA, 100 КиБ, Unicode/HTML как текст, точные скачивания, стрелки/цикл, способы закрытия, фокус, черновик, 404/503/повтор, устаревшие запросы, освобождение URL, EN/RU/UA, уменьшенное движение и перезагрузку. Все 18 сценариев пагинации прошли на сохранённом миллионе без повторной генерации. Три БД, тома и хеши копий сохранены. Для изменения только кода новая миграция/проверка восстановления не требовалась. GUI VS/Workbench не проверялся. Далее — 1.1.0.13 ResponsiveUi.

FileViewer (1.1.0.12): натисніть зображення або назву TXT, щоб відкрити вікно з плавною появою та масштабуванням. «Попередній/Наступний» і стрілки ліворуч/праворуч перемикають вкладення видимих коментарів, включно з розгорнутими відповідями; приховані відповіді та інші сторінки не завантажуються. Escape, кнопка закриття або тло закривають вікно. Фокус утримується всередині й повертається до початкової кнопки; прокручування тла блокується. Налаштування зменшеного руху вимикає анімацію. Завантаження зберігає нормалізований PNG або початкові байти TXT з UUID-назвою. TXT декодується суворим UTF-8 і виводиться звичайним текстом, без виконання HTML; початковий BOM не відображається. Завантаження, помилка та повтор зберігають чернетку. Запити обмежено й скасовано під час перемикання/закриття; тимчасові URL зображень звільняються.

`GET /api/images/{id}?download=true` додає attachment; попередній inline PNG без параметра збережено. TXT використовує `/api/text-files/{id}` з попередніми безпечними заголовками та точними байтами. FileClient відокремлює вікно від HTTP; межі Domain/Application/Infrastructure та Onion/OOP/SOLID збережено. Нових залежностей і міграцій немає. Оновлення 1.1.0.11 → 1.1.0.12: перебудуйте/перезапустіть застосунок із попереднім оточенням, Compose overrides і томами; збережіть БД та перевірені копії. Остання міграція — `20261030130243_TextAttachments`.

Перевірки 1.1.0.12: пройшли 235 backend-тестів (27/84/56/68), 23 frontend, збирання frontend/Docker та Edge desktop/360px. Браузер перевірив реальні фікстури зображення/TXT/відповіді з CAPTCHA, 100 КіБ, Unicode/HTML як текст, точні завантаження, стрілки/цикл, способи закриття, фокус, чернетку, 404/503/повтор, застарілі запити, звільнення URL, EN/RU/UA, зменшений рух і перезавантаження. Усі 18 сценаріїв пагінації пройшли на збереженому мільйоні без повторної генерації. Три БД, томи та хеші копій збережено. Для зміни лише коду нова міграція/перевірка відновлення не була потрібна. GUI VS/Workbench не перевіряли. Далі — 1.1.0.13 ResponsiveUi.
