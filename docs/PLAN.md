# CommentsService — Development plan

## Status and version rules

The approved sequence starts at `CommentsService/v_1.1.0.1_Init`.
Current milestone: Init only. All subsequent iterations below remain planned.
Keep w=1, x=1, y=0; increment only revision z. Configuration extraction is deferred.
Each completed iteration is pushed to its own branch with current read.me,
README and change.log. README.md is English, README.RU.md Russian, README.UA.md
Ukrainian. Other bilingual supporting documents use English first, Russian second.
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
| `CommentsService/v_1.1.0.2_Docker` | Container builds and PostgreSQL, persistent storage, readiness checks. Bootstrap the development/test databases once and preserve them afterwards. |
| `CommentsService/v_1.1.0.3_DataModel` | OOP entities, EF mappings, initial migration, diagrams and migration tracking. Establish previous-state recovery and retained-data upgrade checks. |
| `CommentsService/v_1.1.0.4_CommentsApi` | Create/read plain-text comments, author/client metadata, timestamps and server validation. Integration tests use the persistent test DB with owned fixtures/rollback. |
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

Relevant checks accompany every step. If a separate fix is necessary, it takes
the next revision number and later planned revisions shift accordingly.
This retains the Junior feature scope; the large data set adds persistence and
query checks, not the brokers/NoSQL/cloud architecture of higher assignment levels.

## Evidence required before publishing a version

- Working implementation of the declared scope and accurate test results.
- For database changes: migration IDs, supported previous state, successful upgrade
  on retained data and recovery procedure. No automatic data-loss migration.
- For data-set checks: stable seed/dataset identity and no routine regeneration.
- Current read.me, README and change.log for that branch, English before Russian.
- Verified remote commit; keep earlier iteration branches available.
- Local workstation instructions remain ignored and do not enter Git/Docker context.

---

# CommentsService — План разработки

## Статус и правила версий

Согласованная последовательность начинается с `CommentsService/v_1.1.0.1_Init`.
Текущий этап — только Init. Все следующие итерации ниже пока остаются планом.
w=1, x=1, y=0 сохраняются, увеличивается только ревизия z. Вынос в конфиг отложен.
Каждая завершённая итерация отправляется в свою ветку с актуальными read.me,
README и change.log. README.md — английский, README.RU.md — русский, README.UA.md —
украинский. В остальных двуязычных документах сначала английский, затем русский.
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
| **1.1.0.2 Docker** | Контейнеры, PostgreSQL, постоянные тома и готовность. БД разработки/тестов создаются один раз и сохраняются. |
| **1.1.0.3 DataModel** | Сущности, EF, первая миграция, схемы и учёт миграций. Закладываем предыдущую восстановительную копию и проверки обновления на данных. |
| **1.1.0.4 CommentsApi** | Запись/чтение текста, данные автора/клиента, время и валидация. Проверки в постоянной тестовой БД с откатом или собственными фикстурами. |
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

Необходимые проверки сопровождают каждый шаг. Отдельное исправление занимает
следующий номер ревизии и сдвигает последующие номера плана. Функциональный объём
остаётся Junior: большой набор добавляет проверки сохранности и запросов,
а не брокеры/NoSQL/облачную архитектуру старших уровней задания.

## Что подтверждаем перед публикацией версии

- Работает заявленная функциональность, результаты проверок записаны достоверно.
- Для изменения БД указаны миграции, предыдущая схема, проверка обновления на данных
  и процедура восстановления. Автоматические миграции с потерей данных не допускаются.
- Для большого набора сохраняется идентификатор/версия; регулярной перегенерации нет.
- read.me, README и change.log актуальны для ветки; сначала английский, затем русский.
- Удалённый коммит проверен, предыдущие ветки итераций доступны.
- Локальные инструкции компьютера остаются вне Git и Docker context.
