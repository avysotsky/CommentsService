# Migrations and recovery / Миграции и восстановление / Міграції та відновлення

## English

Image migration `20261030122207_ImageAttachments` adds ImageAttachments (Id, unique CommentId FK CASCADE, FileName varchar128, Width/Height, Data bytea). Dimensions are checked at 1..320/1..240 and payload at 1..524288 bytes. Existing tables/rows and both earlier migration IDs are retained. Upgrade all used databases, including the persistent benchmark. Before upgrade, stop all writers and use a restore-verified current checkpoint; Down is blocked when any image exists. Images are included in database dumps, not stored in a separate volume.

Release 1.1.0.9 stores only expiring CAPTCHA hashes in process memory. No migration/checkpoint rotation is needed; both databases, persistent benchmark, existing migrations and recovery points remain intact.

Release 1.1.0.8 adds no application migration. Retain both existing migration IDs and checkpoints. The optional persistent benchmark database is separate; use db-command.py --benchmark for migration/check, and db-checkpoint.py --include-benchmark before changing its schema/data. See BENCHMARK.md.

Release 1.1.0.7 adds `20261030101720_CommentThreads` after `20261029195935_InitialDataModel`.
Stop writes and restore-verify a current checkpoint before migrating both persistent databases.
The migration adds nullable ParentId and backfills required RootId with each existing Id;
all previous content, metadata, identities and times are retained. It adds a composite parent/root
foreign key with RESTRICT, an (Id,RootId) alternate key, consistency check and reply indexes.
Upgrading a restored copy and reversing with roots only preserves rows/schema/history. Down
explicitly refuses while replies exist and rolls back atomically; do not bypass this guard.
Restore a verified pre-upgrade checkpoint into a separate recovery database if necessary,
accounting for all changes after the checkpoint. HTTP cleanup removes only owned leaves first.

For future schema changes use `--output-dir Persistence/Migrations` on the EF add command.

OnionArchitecture moves the existing migration to Infrastructure/Persistence/Migrations and DbContext to
Infrastructure/Persistence. Its ID and generated SQL are unchanged; no new migration or
checkpoint is required for this code-only correction. See ../ARCHITECTURE.md.

Release 1.1.0.3 adds migration `20261029195935_InitialDataModel` to the previous
Docker/VisualStudio 1.1.0.2 databases, which have no application tables. It creates
schema `comments`, Authors, Comments, indexes, checks, a restricted foreign key,
and EF migration history. It neither drops existing objects nor resets data.
Application startup never migrates; operators run `scripts/db-command.py migrate`
explicitly. A repeated migration is a no-op. `check` verifies connectivity,
pending migrations and model/snapshot consistency. See README.md for the full deployment sequence.

Before a schema/data change, stop application writes and run
`python3 scripts/db-checkpoint.py`. It uses pg_dump custom archives, restores each
into a uniquely named temporary database in the corresponding PostgreSQL service,
and compares every user table's row count and content fingerprints with the source
before/after the dump. A successful candidate replaces that database's previous
checkpoint only after restore verification. Keep `*.dump` and `*.json` together,
private, and retain the same Compose project/volumes. Only verification databases
created by this run are removed; the application databases remain intact.
This is a local recovery checkpoint, not scheduled or off-host backup.
For nonempty shared databases, quiesce all writers throughout verification; the
fingerprint scan is intended for small correctness data at this milestone.
The large-data stage must revisit scan cost before a million-row checkpoint.

Rollback: InitialDataModel.Down drops Authors/Comments and loses their contents.
Do not run `database update 0` on a database containing application data.
Stop writes, preserve the current state, verify the saved checkpoint manifest/hash,
restore the dump into a separate recovery database using pg_restore with
`--exit-on-error --no-owner --no-privileges`, and compare its table fingerprints
with the manifest. Check the `comments_app` ownership/grants before switching the
application to that recovered database. Retain the current database until recovery
has been verified and accepted. Reverting to the 1.1.0.2 checkpoint loses changes
made after that checkpoint; an additive migration does not make reverse migration data-safe.

Migration generation/export requires .NET 10 and the committed tool manifest:

```bash
dotnet tool restore
dotnet ef migrations list --project src/CommentsService.Infrastructure --startup-project src/CommentsService.Api
dotnet ef migrations script --idempotent --project src/CommentsService.Infrastructure --startup-project src/CommentsService.Api --output docs/database/postgresql.sql
```

Set private `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD` first; do not put
secrets in command arguments or source. For script generation only, placeholder
values are sufficient because no connection is made. Compare generated SQL/model
with the migration snapshot before publishing.

Verified for this release: restore-checked pre-migration checkpoints for both persistent
DBs; retained control records through the first migration; retained author/comment
relationships and content through repeated migration; ten transaction-isolated tests;
separate initial installation in a uniquely named blank database and idempotent SQL export.
Only owned control records/schemas and the blank verification database were removed.
Full thread/attachment preservation checks belong to later schema versions.

## Русский

Миграция `20261030122207_ImageAttachments` добавляет ImageAttachments (Id, уникальный CommentId FK CASCADE, FileName varchar128, Width/Height, Data bytea). Проверки: размеры 1..320/1..240, байты 1..524288. Прежние таблицы/строки и оба ранних ID миграций сохранены. Обновляйте все используемые БД, включая постоянный benchmark. Перед обновлением остановите запись и проверьте восстановление текущей копии; Down запрещён при наличии картинок. Изображения входят в dump БД, отдельного тома нет.

В 1.1.0.9 хеши CAPTCHA со сроком хранятся только в памяти процесса. Миграция/замена копий не нужна; обе БД, постоянный benchmark, миграции и восстановительные точки сохраняются.

В 1.1.0.8 новой миграции приложения нет. Сохраните оба ID миграций и копии. Необязательная постоянная benchmark-БД отделена; для миграции/проверки используйте db-command.py --benchmark, перед изменением её схемы/данных — db-checkpoint.py --include-benchmark. См. BENCHMARK.md.

Выпуск 1.1.0.7 добавляет `20261030101720_CommentThreads` после `20261029195935_InitialDataModel`.
Остановите запись и проверьте восстановление текущей копии перед миграцией обеих постоянных БД.
Добавляются nullable ParentId и обязательный RootId, заполняемый прежним Id; старые тексты,
метаданные, идентификаторы и время сохранены. Добавляются составной FK родитель/корень с RESTRICT,
альтернативный ключ (Id,RootId), проверка согласованности и индексы ответов.
Обновление восстановленной копии и откат только с корнями сохраняют строки/схему/историю.
Down явно запрещён при наличии ответов и откатывается атомарно; запрет не обходить.
При необходимости восстанавливайте проверенную копию до обновления в отдельную БД,
учитывая изменения после неё. HTTP-тесты удаляют только свои записи начиная с листьев.

Для будущих изменений схемы указывайте `--output-dir Persistence/Migrations` при создании миграции EF.

OnionArchitecture переносит прежнюю миграцию в Infrastructure/Persistence/Migrations, DbContext — в
Infrastructure/Persistence. ID и SQL сохранены; для этой доработки только кода не нужны
новая миграция и контрольная копия. См. ../ARCHITECTURE.md.

Версия 1.1.0.3 добавляет миграцию `20261029195935_InitialDataModel` к БД предыдущей
1.1.0.2 Docker/VisualStudio, где ещё нет таблиц приложения. Она создаёт схему
`comments`, Authors, Comments, индексы, проверки, внешний ключ с запретом удаления
автора и историю EF. Существующие объекты не удаляются, данные не сбрасываются.
При старте приложение не мигрирует; оператор явно запускает
`scripts/db-command.py migrate`. Повторный запуск ничего не меняет. `check` проверяет
подключение, неприменённые миграции и соответствие модели снимку.
Полная последовательность развёртывания — в README.RU.md.

Перед изменением схемы/данных остановите запись приложения и выполните
`python3 scripts/db-checkpoint.py`. Скрипт создаёт архивы pg_dump custom, восстанавливает
каждый во временную БД с уникальным именем в соответствующем сервисе PostgreSQL
и сравнивает число строк и отпечатки содержимого каждой пользовательской таблицы
с исходной БД до/после дампа. Прежняя копия заменяется только после успешной проверки
восстановления новой. Храните `*.dump` и `*.json` вместе и приватно; сохраняйте
проект Compose и тома. Удаляются только проверочные БД текущего запуска;
БД приложения остаются на месте. Это локальная контрольная копия, а не резервирование
по расписанию или на другой машине. Для непустой общей БД остановите всех писателей
на время проверки. Сканирование отпечатков рассчитано на небольшие функциональные
данные этого этапа; до копирования миллиона строк потребуется оценить его стоимость.

Откат: InitialDataModel.Down удаляет Authors/Comments вместе с данными.
Не запускайте `database update 0` на БД с данными приложения.
Остановите запись, сохраните текущее состояние, проверьте описание/хеш контрольной
копии, восстановите дамп в отдельную восстановительную БД через pg_restore с
`--exit-on-error --no-owner --no-privileges`, сравните отпечатки таблиц с описанием.
До переключения приложения проверьте владельцев/права `comments_app`.
Сохраняйте текущую БД до проверки и принятия восстановления. Возврат к копии 1.1.0.2
теряет изменения после её создания; добавляющая миграция не делает обратную миграцию безопасной для данных.

Для создания/экспорта миграций нужны .NET 10 и манифест инструментов репозитория:

```bash
dotnet tool restore
dotnet ef migrations list --project src/CommentsService.Infrastructure --startup-project src/CommentsService.Api
dotnet ef migrations script --idempotent --project src/CommentsService.Infrastructure --startup-project src/CommentsService.Api --output docs/database/postgresql.sql
```

Предварительно задайте приватные `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`;
не включайте секреты в аргументы команд и исходники. Для генерации SQL достаточно
значений-заглушек: подключение не выполняется. Перед публикацией сравните SQL/модель со снимком миграции.

Проверено: восстановление копий обеих постоянных БД до миграции; сохранение контрольных
записей при первой миграции; сохранение связи автора/комментария и содержимого при повторной;
десять тестов с откатом; отдельная установка на пустую БД с уникальным именем и идемпотентный SQL.
Удалены только собственные контрольные записи/схемы и пустая проверочная БД.
Полная проверка сохранности веток ответов/вложений относится к следующим версиям схемы.

## Українська

Міграція `20261030122207_ImageAttachments` додає ImageAttachments (Id, унікальний CommentId FK CASCADE, FileName varchar128, Width/Height, Data bytea). Перевірки: розміри 1..320/1..240, байти 1..524288. Попередні таблиці/рядки та обидва ранні ID міграцій збережено. Оновлюйте всі використані БД, включно з постійним benchmark. Перед оновленням зупиніть запис і перевірте відновлення поточної копії; Down заборонено за наявності зображень. Зображення входять до dump БД, окремого тому немає.

У 1.1.0.9 хеші CAPTCHA з терміном зберігаються лише в пам’яті процесу. Міграція/заміна копій не потрібна; обидві БД, постійний benchmark, міграції й відновлювальні точки зберігаються.

У 1.1.0.8 нової міграції застосунку немає. Збережіть обидва ID міграцій і копії. Необов’язкову постійну benchmark-БД відокремлено; для міграції/перевірки використовуйте db-command.py --benchmark, перед зміною її схеми/даних — db-checkpoint.py --include-benchmark. Див. BENCHMARK.md.

Випуск 1.1.0.7 додає `20261030101720_CommentThreads` після `20261029195935_InitialDataModel`.
Зупиніть запис і перевірте відновлення поточної копії перед міграцією обох постійних БД.
Додаються nullable ParentId та обов’язковий RootId, заповнений попереднім Id; старі тексти,
метадані, ідентифікатори й час збережено. Додаються складений FK батьківський запис/корінь із RESTRICT,
альтернативний ключ (Id,RootId), перевірка узгодженості та індекси відповідей.
Оновлення відновленої копії й відкат лише з коренями зберігають рядки/схему/історію.
Down явно заборонено за наявності відповідей та відкочується атомарно; заборону не обходити.
За потреби відновлюйте перевірену копію до оновлення в окрему БД,
враховуючи зміни після неї. HTTP-тести видаляють лише власні записи починаючи з листків.

Для майбутніх змін схеми вказуйте `--output-dir Persistence/Migrations` під час створення міграції EF.

OnionArchitecture переносить попередню міграцію до Infrastructure/Persistence/Migrations, DbContext — до
Infrastructure/Persistence. ID і SQL збережено; для цього виправлення лише коду не потрібні
нова міграція та контрольна копія. Див. ../ARCHITECTURE.md.

Версія 1.1.0.3 додає міграцію `20261029195935_InitialDataModel` до БД попередньої
1.1.0.2 Docker/VisualStudio, де ще немає таблиць застосунку. Вона створює схему
`comments`, Authors, Comments, індекси, перевірки, зовнішній ключ із забороною видалення
автора та історію EF. Наявні об’єкти не видаляються, дані не скидаються.
Під час старту застосунок не мігрує; оператор явно запускає
`scripts/db-command.py migrate`. Повторний запуск нічого не змінює. `check` перевіряє
підключення, незастосовані міграції та відповідність моделі знімку.
Повна послідовність розгортання — у README.UA.md.

Перед зміною схеми/даних зупиніть запис застосунку та виконайте
`python3 scripts/db-checkpoint.py`. Скрипт створює архіви pg_dump custom, відновлює
кожен у тимчасову БД з унікальною назвою у відповідному сервісі PostgreSQL
і порівнює кількість рядків та відбитки вмісту кожної користувацької таблиці
з вихідною БД до/після дампа. Попередня копія замінюється лише після успішної перевірки
відновлення нової. Зберігайте `*.dump` і `*.json` разом і приватно; зберігайте
проєкт Compose та томи. Видаляються лише перевірочні БД поточного запуску;
БД застосунку залишаються на місці. Це локальна контрольна копія, а не резервування
за розкладом або на іншій машині. Для непорожньої спільної БД зупиніть усіх писачів
на час перевірки. Сканування відбитків розраховано на невеликі функціональні
дані цього етапу; до копіювання мільйона рядків потрібно оцінити його вартість.

Відкат: InitialDataModel.Down видаляє Authors/Comments разом із даними.
Не запускайте `database update 0` на БД з даними застосунку.
Зупиніть запис, збережіть поточний стан, перевірте опис/хеш контрольної
копії, відновіть дамп в окрему відновлювальну БД через pg_restore з
`--exit-on-error --no-owner --no-privileges`, порівняйте відбитки таблиць з описом.
До перемикання застосунку перевірте власників/права `comments_app`.
Зберігайте поточну БД до перевірки та прийняття відновлення. Повернення до копії 1.1.0.2
втрачає зміни після її створення; додавальна міграція не робить зворотну міграцію безпечною для даних.

Для створення/експорту міграцій потрібні .NET 10 і маніфест інструментів репозиторію:

```bash
dotnet tool restore
dotnet ef migrations list --project src/CommentsService.Infrastructure --startup-project src/CommentsService.Api
dotnet ef migrations script --idempotent --project src/CommentsService.Infrastructure --startup-project src/CommentsService.Api --output docs/database/postgresql.sql
```

Попередньо задайте приватні `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`;
не додавайте секрети до аргументів команд та вихідних файлів. Для генерації SQL достатньо
значень-заглушок: підключення не виконується. Перед публікацією порівняйте SQL/модель зі знімком міграції.

Перевірено: відновлення копій обох постійних БД до міграції; збереження контрольних
записів під час першої міграції; збереження зв’язку автора/коментаря та вмісту під час повторної;
десять тестів із відкатом; окреме встановлення на порожню БД з унікальною назвою та ідемпотентний SQL.
Видалено лише власні контрольні записи/схеми та порожню перевірочну БД.
Повна перевірка збереження гілок відповідей/вкладень належить до наступних версій схеми.
