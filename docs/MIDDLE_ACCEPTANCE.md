# Middle final acceptance — 1.3.3.4

## English

Runtime1.3.3.4/CloudAttachments implements all selected Middle tools and preserves Junior/Junior+. This acceptance correction keeps the version and app unchanged. Scope: GraphQL, RabbitMQ, Elasticsearch, real Supabase Storage Free. No Middle+ load target, outbox or replica claim.

### Install the published source

Clone the published CommentsService/v_1.3.3.4_MiddleAcceptance branch into a new directory, then follow First installation in README.md. Use a new Compose project, empty volumes and unused ports only for an independent acceptance installation; preserve existing environments on updates. Keep the initial restore-verified checkpoints, apply migrations explicitly and run schema verification. Do not copy the retained user databases or million corpus. After core installation, enable the optional local services with these commands. If using a different project/env/override, supply the same arguments consistently to every Compose and database/search script.

```bash
python3 scripts/init-broker-env.py
docker compose -p commentsservice --profile broker up -d --no-deps --wait broker
python3 scripts/init-search-env.py
docker compose -p commentsservice --profile search up -d --no-deps --wait elasticsearch
python3 scripts/configure-search-user.py
docker compose -p commentsservice up -d --no-deps --no-build --wait app
```

The search provisioning command now waits/retries bounded transient readiness failures (up to120seconds per idempotent role/user operation,5seconds per HTTP attempt). A401/403 is not retried as readiness: correct the private settings. It never prints credentials or changes global Docker/host settings. Restarting app picks up the newly generated private env files. PostgreSQL, broker and search stay private; only the selected app port is exposed on loopback.

### Demonstrate Cloud on your own files

1. Use a separate Supabase Free project/private bucket following CLOUD_ATTACHMENTS.md. Fill a private .env.cloud with SUPABASE_URL, SUPABASE_SECRET_KEY and SUPABASE_BUCKET. No card, paid add-on, automatic upgrade or Management API token is needed. Do not paste the key into a command or chat.
2. In your local app create a root comment with a tiny own PNG and a reply with a tiny own UTF-8 TXT, both under username CloudDemoOwner. Solve the normal CAPTCHA. Copy the two comment UUIDs from the POST201 response in browser Network, or from rootPage and replies GraphQL reads. Select only files you own; never existing user attachments.
3. In your private .env.cloud add the entries below, replacing placeholders with those exact comment UUIDs (not attachment IDs). If entries already exist, edit them instead of adding duplicates. This is explicit consent for just these2files and one replay at app startup; no scanning/backfill occurs.

```dotenv
Runtime__Cloud__Enabled=true
Runtime__Cloud__AllowedCommentIds__0=OWN_IMAGE_COMMENT_UUID
Runtime__Cloud__AllowedCommentIds__1=OWN_TXT_COMMENT_UUID
Runtime__Cloud__ReplayCommentIds__0=OWN_IMAGE_COMMENT_UUID
Runtime__Cloud__ReplayCommentIds__1=OWN_TXT_COMMENT_UUID
```

4. Recreate only app and run the read-only verifier below. Use the actual SQL database name for the object namespace and your actual demo username. The script requires one PNG and one TXT, verifies the author marker, byte equality/SHA256 in real private Storage and denied public access. It performs no upload, SQL write or bucket scan; no redirects/retries,5second request timeout,2MiB total response budget, at most9requests. It prints object keys/UUIDs/hashes for your own fixtures, never keys, private project URL or provider error bodies. App logs should contain Cloud attachment copy completed and Cloud attachment read verified with the selected attachment IDs. The verifier alone does not replace this backend-path evidence.

```bash
docker compose -p commentsservice up -d --no-deps --no-build --wait app
python3 scripts/verify-cloud-demo.py --base-url http://127.0.0.1:8080   --env-file .env.cloud --database comments --expected-author CloudDemoOwner   --comment-id OWN_IMAGE_COMMENT_UUID --comment-id OWN_TXT_COMMENT_UUID
docker compose -p commentsservice logs --tail 100 app
```

5. After demonstration, remove all AllowedCommentIds/ReplayCommentIds entries and disable Cloud or keep consent empty; recreate only app again. This prevents replay after a later restart. Delete only the exact own object keys printed by the verifier via Storage Dashboard; never purge a bucket. SQL originals remain available. An isolated acceptance installation can be removed only after checking exact project/resource ownership; never use down -v/global prune against a shared project.

### Checks and limits

Confirm /api/status1.3.3.4/CloudAttachments; health/database200; GraphQL rootPage25 and6sorts; actual word search and selected reply; broker publisher/consumer logs; WS refresh without losing draft; PNG/TXT exact download and safe headers. Recreate app with the same environment/volumes and confirm your own rows/files survive. verify-schema.py must match the shipped Workbench projection. Optional provider failures have previous real/injected evidence; do not exhaust a real cloud quota or pause the owner's project just for acceptance. Supabase limits/pausing and process-local delivery/budgets are documented in CLOUD_ATTACHMENTS.md. Native .mwb import/reopen is verified; the known Workbench shutdown-code caveat remains. Hosting has no24/7 SLA.

## Русский

Runtime1.3.3.4/CloudAttachments реализует выбранные инструменты Middle и сохраняет Junior/Junior+. Приёмочное исправление сохраняет версию и приложение. Объём: GraphQL, RabbitMQ, Elasticsearch, реальный Supabase Storage Free. Нагрузка Middle+, outbox и реплики не заявлены.

### Установка опубликованного кода

Клонировать опубликованную ветку CommentsService/v_1.3.3.4_MiddleAcceptance в новый каталог и выполнить первую установку README.RU.md. Для независимой приёмки использовать новый Compose-проект, пустые тома и свободные порты; при обновлении сохранять прежнее окружение. Сохранить начальные проверенные восстановлением копии, явно применить миграции и сверить схему. Пользовательские БД/миллион не копировать. После основной установки включить дополнительные локальные сервисы командами ниже. Другие проект/env/overrides одинаково указывать во всех Compose и скриптах БД/поиска.

```bash
python3 scripts/init-broker-env.py
docker compose -p commentsservice --profile broker up -d --no-deps --wait broker
python3 scripts/init-search-env.py
docker compose -p commentsservice --profile search up -d --no-deps --wait elasticsearch
python3 scripts/configure-search-user.py
docker compose -p commentsservice up -d --no-deps --no-build --wait app
```

Настройка пользователя поиска ограниченно ждёт/повторяет временные ошибки готовности: до120секунд на идемпотентную операцию роли/пользователя,5секунд на HTTP-попытку.401/403 не повторяются как готовность: исправить приватные настройки. Ключи не печатаются, глобальные параметры Docker/хоста не меняются. Пересоздание app подключает новые приватные env-файлы. БД/брокер/поиск остаются приватными; наружу loopback выдаётся только выбранный порт app.

### Демонстрация Cloud на собственных файлах

1. Отдельный Supabase Free/приватный bucket настроить по CLOUD_ATTACHMENTS.md. Заполнить приватный .env.cloud: SUPABASE_URL/SUPABASE_SECRET_KEY/SUPABASE_BUCKET. Без карты, платных дополнений, auto-upgrade и Management API token. Ключ не вставлять в команду или чат.
2. В локальном app создать свой корень с маленьким PNG и ответ с маленьким UTF-8 TXT под именем CloudDemoOwner. Пройти обычную CAPTCHA. Два UUID комментариев скопировать из POST201 в Network браузера либо чтений GraphQL rootPage/replies. Выбирать только свои файлы, не прежние пользовательские вложения.
3. В приватный .env.cloud добавить строки ниже, заменив placeholders точными UUID комментариев, не файлов. Прежние строки редактировать, не дублировать. Это разрешение только2своих файлов и одного replay при старте, без сканирования/переноса истории.

```dotenv
Runtime__Cloud__Enabled=true
Runtime__Cloud__AllowedCommentIds__0=OWN_IMAGE_COMMENT_UUID
Runtime__Cloud__AllowedCommentIds__1=OWN_TXT_COMMENT_UUID
Runtime__Cloud__ReplayCommentIds__0=OWN_IMAGE_COMMENT_UUID
Runtime__Cloud__ReplayCommentIds__1=OWN_TXT_COMMENT_UUID
```

4. Пересоздать только app и запустить проверку ниже. Указать фактическое имя SQL-БД для namespace и своё имя автора. Скрипт требует один PNG и один TXT; проверяет маркер автора, точные байты/SHA256 настоящего приватного Storage и закрытый публичный доступ. Не загружает файлы, не пишет SQL, не сканирует bucket; без redirect/повторов,5секунд на запрос,2МиБ общего ответа, максимум9запросов. Печатает ключи объектов/UUID/хеши своих фикстур, но не секреты/приватный URL/ответы ошибок провайдера. Логи app должны показать Cloud attachment copy completed и Cloud attachment read verified с выбранными ID файлов. Одна утилита не заменяет это доказательство backend-пути.

```bash
docker compose -p commentsservice up -d --no-deps --no-build --wait app
python3 scripts/verify-cloud-demo.py --base-url http://127.0.0.1:8080   --env-file .env.cloud --database comments --expected-author CloudDemoOwner   --comment-id OWN_IMAGE_COMMENT_UUID --comment-id OWN_TXT_COMMENT_UUID
docker compose -p commentsservice logs --tail 100 app
```

5. После демонстрации удалить все AllowedCommentIds/ReplayCommentIds, выключить Cloud либо оставить пустое разрешение; снова пересоздать только app. Это исключает replay после следующего рестарта. В Storage Dashboard удалить только точные свои ключи объектов из результата, не весь bucket. SQL-оригиналы доступны. Изолированную установку удалять только после проверки точных владельцев ресурсов; down -v/global prune для общего проекта запрещены.

### Проверки и ограничения

Проверить /api/status1.3.3.4/CloudAttachments, health/database200, GraphQL rootPage25/6сортировок, реальный поиск/найденный ответ, логи publisher/consumer брокера, WS без потери черновика, точные PNG/TXT/защитные заголовки. Пересоздать app с теми же env/томами и проверить сохранность своих строк/файлов. verify-schema.py должен совпасть с Workbench-проекцией. Для отказов сервисов сохранены реальные/подставные доказательства; настоящую квоту не исчерпывать, проект владельца ради приёмки не приостанавливать. Лимиты/пауза Supabase и память процесса описаны в CLOUD_ATTACHMENTS.md. Импорт/повторное открытие .mwb проверены; известная оговорка кода завершения Workbench остаётся. Хостинг без SLA24/7.

## Українська

Runtime1.3.3.4/CloudAttachments реалізує обрані інструменти Middle та зберігає Junior/Junior+. Приймальне виправлення зберігає версію й застосунок. Обсяг: GraphQL, RabbitMQ, Elasticsearch, справжній Supabase Storage Free. Навантаження Middle+, outbox і репліки не заявлені.

### Інсталяція опублікованого коду

Клонувати опубліковану гілку CommentsService/v_1.3.3.4_MiddleAcceptance до нового каталогу й виконати першу інсталяцію README.UA.md. Для незалежного приймання використати новий Compose-проєкт, порожні томи та вільні порти; за оновлення зберігати попереднє оточення. Зберегти початкові перевірені відновленням копії, явно застосувати міграції та звірити схему. Користувацькі БД/мільйон не копіювати. Після основної інсталяції ввімкнути додаткові локальні сервіси командами нижче. Інші проєкт/env/overrides однаково вказувати в усіх Compose і скриптах БД/пошуку.

```bash
python3 scripts/init-broker-env.py
docker compose -p commentsservice --profile broker up -d --no-deps --wait broker
python3 scripts/init-search-env.py
docker compose -p commentsservice --profile search up -d --no-deps --wait elasticsearch
python3 scripts/configure-search-user.py
docker compose -p commentsservice up -d --no-deps --no-build --wait app
```

Налаштування користувача пошуку обмежено чекає/повторює тимчасові помилки готовності: до120секунд на ідемпотентну операцію ролі/користувача,5секунд на HTTP-спробу.401/403 не повторюються як готовність: виправити приватні налаштування. Ключі не друкуються, глобальні параметри Docker/хоста не змінюються. Перестворення app підключає нові приватні env-файли. БД/брокер/пошук залишаються приватними; назовні loopback надається лише обраний порт app.

### Демонстрація Cloud на власних файлах

1. Окремий Supabase Free/приватний bucket налаштувати за CLOUD_ATTACHMENTS.md. Заповнити приватний .env.cloud: SUPABASE_URL/SUPABASE_SECRET_KEY/SUPABASE_BUCKET. Без картки, платних доповнень, auto-upgrade і Management API token. Ключ не вставляти в команду чи чат.
2. У локальному app створити власний корінь із маленьким PNG та відповідь із маленьким UTF-8 TXT під ім'ям CloudDemoOwner. Пройти звичайну CAPTCHA. Два UUID коментарів скопіювати з POST201 у Network браузера або читань GraphQL rootPage/replies. Обирати лише власні файли, не попередні користувацькі вкладення.
3. До приватного .env.cloud додати рядки нижче, замінивши placeholders точними UUID коментарів, не файлів. Попередні рядки редагувати, не дублювати. Це дозвіл лише2власних файлів та одного replay на старті, без сканування/перенесення історії.

```dotenv
Runtime__Cloud__Enabled=true
Runtime__Cloud__AllowedCommentIds__0=OWN_IMAGE_COMMENT_UUID
Runtime__Cloud__AllowedCommentIds__1=OWN_TXT_COMMENT_UUID
Runtime__Cloud__ReplayCommentIds__0=OWN_IMAGE_COMMENT_UUID
Runtime__Cloud__ReplayCommentIds__1=OWN_TXT_COMMENT_UUID
```

4. Перестворити лише app і запустити перевірку нижче. Указати фактичне ім'я SQL-БД для namespace та своє ім'я автора. Скрипт вимагає один PNG та один TXT; перевіряє маркер автора, точні байти/SHA256 справжнього приватного Storage й закритий публічний доступ. Не завантажує файли, не пише SQL, не сканує bucket; без redirect/повторів,5секунд на запит,2МіБ спільної відповіді, максимум9запитів. Друкує ключі об'єктів/UUID/хеші власних фікстур, але не секрети/приватний URL/відповіді помилок провайдера. Логи app мають показати Cloud attachment copy completed і Cloud attachment read verified з обраними ID файлів. Одна утиліта не замінює це підтвердження backend-шляху.

```bash
docker compose -p commentsservice up -d --no-deps --no-build --wait app
python3 scripts/verify-cloud-demo.py --base-url http://127.0.0.1:8080   --env-file .env.cloud --database comments --expected-author CloudDemoOwner   --comment-id OWN_IMAGE_COMMENT_UUID --comment-id OWN_TXT_COMMENT_UUID
docker compose -p commentsservice logs --tail 100 app
```

5. Після демонстрації видалити всі AllowedCommentIds/ReplayCommentIds, вимкнути Cloud або залишити порожній дозвіл; знову перестворити лише app. Це виключає replay після наступного рестарту. У Storage Dashboard видалити лише точні власні ключі об'єктів із результату, не весь bucket. SQL-оригінали доступні. Ізольовану інсталяцію видаляти лише після перевірки точних власників ресурсів; down -v/global prune для спільного проєкту заборонені.

### Перевірки й обмеження

Перевірити /api/status1.3.3.4/CloudAttachments, health/database200, GraphQL rootPage25/6сортувань, справжній пошук/знайдену відповідь, логи publisher/consumer брокера, WS без втрати чернетки, точні PNG/TXT/захисні заголовки. Перестворити app з тими самими env/томами та перевірити збереження власних рядків/файлів. verify-schema.py має збігтися з Workbench-проєкцією. Для відмов сервісів збережені справжні/підставні докази; справжню квоту не вичерпувати, проєкт власника заради приймання не призупиняти. Межі/пауза Supabase та пам'ять процесу описані в CLOUD_ATTACHMENTS.md. Імпорт/повторне відкриття .mwb перевірені; відоме застереження коду завершення Workbench лишається. Хостинг без SLA24/7.

## Acceptance evidence / Результаты / Результати

EN: Final acceptance: published1.3.3.4 source installed into independent empty PostgreSQL volumes from README; initial checkpoints restored/verified, all4migrations explicit. Optional RabbitMQ/search configured by supplied scripts. A new-volume ES readiness race was reproduced and corrected with bounded provisioning retries, then a second empty ES volume passed. Real Edge CAPTCHA/root/reply/PNG/TXT/preview/6GraphQL sorts/WS/draft/EN-RU-UA/360px passed. All3own records were searchable; Rabbit publish/receipt and the independent search consumer were observed. Real Supabase2objects267bytes matched with denied public access; the new read-only verifier passed. A third non-consented file stayed SQL-only. Own records/files survived app recreation. Existing449backend/36frontend acceptance remains valid; this correction changes no runtime code/schema/package. The exact published correction checkout is verified separately against this tested runtime source and installer/verifier files. Own temporary resources/cloud objects are removed after ownership checks; original working deployment is preserved.

RU: Финальная приёмка: опубликованный код1.3.3.4 установлен по README в отдельные пустые тома PostgreSQL; начальные копии проверены восстановлением,4миграции применены явно. RabbitMQ/поиск настроены штатными скриптами. Гонка готовности ES на новом томе воспроизведена/исправлена ограниченными повторами, второй пустой ES-том прошёл. Edge CAPTCHA/корень/ответ/PNG/TXT/preview/6GraphQL-сортировок/WS/черновик/EN-RU-UA/360px прошли. Все3свои записи найдены поиском, наблюдались публикация/приём Rabbit и отдельный search consumer. Реальные2объекта Supabase267байт совпали, публичный доступ закрыт; новая утилита чтения прошла. Третий файл без разрешения остался в SQL. Свои записи/файлы пережили пересоздание app. Прежние449backend/36frontend сохраняются; runtime/схема/пакеты не менялись. Точная опубликованная копия исправления отдельно сверяется с проверенными runtime/установщиком/утилитой. Свои временные ресурсы/объекты облака удаляются после проверки владельцев; рабочее развёртывание сохранено.

UA: Фінальне приймання: опублікований код1.3.3.4 встановлено за README в окремі порожні томи PostgreSQL; початкові копії перевірено відновленням,4міграції застосовано явно. RabbitMQ/пошук налаштовано штатними скриптами. Гонку готовності ES на новому томі відтворено/виправлено обмеженими повторами, другий порожній ES-том пройшов. Edge CAPTCHA/корінь/відповідь/PNG/TXT/preview/6GraphQL-сортувань/WS/чернетка/EN-RU-UA/360px пройшли. Усі3власні записи знайдено пошуком, спостерігали публікацію/приймання Rabbit та окремий search consumer. Справжні2об’єкти Supabase267байтів збіглися, публічний доступ закрито; нова утиліта читання пройшла. Третій файл без дозволу лишився в SQL. Власні записи/файли пережили перестворення app. Попередні449backend/36frontend зберігаються; runtime/схема/пакети не змінювалися. Точна опублікована копія виправлення окремо звіряється з перевіреними runtime/інсталятором/утилітою. Власні тимчасові ресурси/об’єкти хмари видаляються після перевірки власників; робоче розгортання збережене.
