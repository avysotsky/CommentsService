# Comment search - 1.3.3.3

## English

The NoSQL item uses Elasticsearch9.5.5 with the Basic license and the documented REST API through a dedicated bounded HttpClient. There is no client SDK dependency, Kibana, paid cloud service or external account. Search is optional and disabled in defaults/old JSON files. Only searchEnabled and searchQueryMaxLength are added to public configuration. All previous REST/GraphQL reads, writes, CAPTCHA, preview, files, cache invalidation and WebSocket notifications keep their existing paths. Elasticsearch is not an application/database health or startup dependency.

GET /api/comments/search?query=words&page=1 searches comment text and public author names, including replies. Query length, page size/window, timeouts and concurrency are bounded; empty/control-character queries and invalid pages return400, disabled404, busy429 and unavailable/partial/malformed provider results503. Queries are literal multi_match text over an explicit field allowlist, not caller-supplied Elasticsearch DSL/wildcards/regular expressions. Standard Unicode analysis supports EN/RU/UA words; stemming/fuzzy matching and substring search are not promised. Search is eventually consistent; result order is relevance, timestamp and stable UUID, with previous/next pages rather than an exact global count. Documents may move between requests as the index changes.

The index contains only id/userName/visible text/createdAt, strict mapping schema1, one shard/no replicas and disabled _source. Email, homepage, IP, User-Agent, attachment metadata/bytes are not indexed. Results return IDs only; one bounded SQL query hydrates canonical public CommentDto records in result order, so deleted/stale index entries cannot expose removed comments or index-supplied text. UI rendering uses the existing safe CommentText component; there is no raw highlight HTML. Search has its own form/results/errors/cancellation and preserves the existing draft, root-page query and sorting. Open discussion loads the canonical root through REST; a selected reply remains directly visible and can be replied to using the existing form.

A separate RabbitMQ connection/temporary bounded drop-head queue feeds search IDs without awaiting Elasticsearch. It never delays or modifies the original notification consumer, publisher or local fallback. A bounded background worker reloads source documents from SQL and indexes by stable UUID. Missing signals, RabbitMQ downtime and Elasticsearch outages are repaired by repeated UUID cursor scans in SQL, followed by bounded orphan scans/deletes in the index. Cursors advance only after successful writes and restart from the beginning after a process restart; no SQL checkpoint table or schema migration. Each full reconciliation cycle sees inserts behind the cursor on the next cycle. Convergence time depends on corpus size and configured pacing; there is no fixed freshness SLA or durable delivery guarantee. Acknowledgements mean an in-memory signal was accepted, not that indexing is durable.

Bulk requests are capped at1MiB and configurable batches at100 records. The worker uses dedicated scoped read queries and pauses between batches; failures back off. A soft index-size guard stops additional writes at MaximumIndexBytes (a batch may overshoot); Elasticsearch disk watermarks reserve space. The supplied container has1.5GiB RAM/512MiB Java heap/1CPU, mmap disabled without changing host sysctls, its own derived search_data volume and no published HTTP/transport ports. Search timeouts return a local search error while normal reading/posting continues. Previous results and drafts remain available for retry. Both app startup with Elasticsearch absent and recovery without app restart are intended acceptance cases, not a reason to restart the whole stack.

Authentication is enabled. The server bootstrap password lives only in the server environment file; the app uses a separate comments_search user limited to comments-* index read/write/create/metadata/monitor privileges, no cluster administration. Automatic index creation is disabled server-side; the adapter explicitly creates its strict versioned mapping. The configured Index is a base prefix; the app always appends a stable12hex SHA-256 database-name partition, isolating the three existing databases even when configuration is shared. Separate deployments sharing a search cluster should also choose separate base prefixes. The benchmark measurement tool explicitly disables search in its temporary read-only app. Use private-network HTTP only with the provided local topology; remote deployments require their own transport/security design.

## Русский

Пункт NoSQL реализован через Elasticsearch9.5.5 с лицензией Basic и документированным REST API через отдельный ограниченный HttpClient. SDK клиента, Kibana, платные облака и внешние аккаунты не нужны. Поиск необязателен и по умолчанию выключен, в том числе для старых JSON. Публично добавлены только searchEnabled и searchQueryMaxLength. Прежние REST/GraphQL-чтение, запись, CAPTCHA, preview, файлы, сброс кеша и WS сохраняют свои пути. Health/запуск app и БД не зависят от Elasticsearch.

GET /api/comments/search?query=words&page=1 ищет по тексту и публичному имени автора, включая ответы. Длина, размер/окно страниц, срок и одновременность ограничены: неверные запросы400, отключение404, перегрузка429, недоступный/частичный/неверный ответ провайдера503. Используется буквальный multi_match с явными полями, не пользовательский DSL/wildcards/regex. Стандартный Unicode-анализ поддерживает слова EN/RU/UA; морфология/нечёткий/подстрочный поиск не обещаны. Согласованность отложенная; порядок - релевантность, дата, UUID, страницы предыдущая/следующая без точного общего счётчика. При изменении индекса записи могут перемещаться между запросами.

Индекс содержит только id/userName/видимый текст/createdAt, strict mapping schema1, один shard/без replicas и отключённый _source. E-mail/homepage/IP/User-Agent и вложения не индексируются. Индекс отдаёт ID; один ограниченный SQL-запрос читает текущие публичные CommentDto в исходном порядке, скрывая удалённые/устаревшие записи. Текст берётся из SQL и выводится прежним безопасным CommentText, без сырого HTML highlight. Отдельные форма/результаты/ошибки/отмена поиска сохраняют черновик, страницу и сортировку основного списка. «Открыть обсуждение» получает корень через REST; найденный ответ показан отдельно и доступен для ответа через прежнюю форму.

Отдельное подключение/временная ограниченная drop-head очередь RabbitMQ передаёт ID без ожидания Elasticsearch и не меняет прежнюю доставку/локальный fallback. Фоновый worker читает SQL и индексирует по UUID. Пропуски сигналов и отказы брокера/поиска исправляются повторными SQL-сканами по UUID и ограниченной сверкой/удалением сирот индекса. Курсор продвигается только после успешной записи; после рестарта начинается сначала. Таблица checkpoint/миграция SQL не нужны. Вставки позади курсора попадут в следующем цикле. Срок зависит от объёма/темпа, фиксированного SLA свежести и долговечной доставки нет. Ack означает приём сигнала в память, не долговечную индексацию.

Bulk ограничен1МиБ, batch - до100записей; отдельные scoped SQL-чтения, паузы и backoff. Мягкий MaximumIndexBytes останавливает новые записи при превышении, возможен выход на размер batch; disk watermarks резервируют место. Контейнер:1,5GiB RAM/heap512MiB/1CPU, mmap выключен без host sysctl, свой производный том search_data, порты не опубликованы. Таймаут даёт ошибку только поиска; чтение/запись продолжаются, предыдущие результаты/черновик остаются. Запуск без Elasticsearch и восстановление без рестарта app проверяются отдельно.

Аутентификация включена. Bootstrap-пароль только в серверном env, у app отдельный comments_search с правами чтения/записи/создания/метаданных/monitor только comments-* без cluster admin. Автосоздание индексов сервером выключено; адаптер явно создаёт strict mapping. Index - базовый префикс; app всегда добавляет стабильный12hex SHA-256 раздел имени БД. Три существующие БД изолированы даже с общим конфигом. Разным развёртываниям на одном поисковом кластере нужны разные префиксы. Временный app замеров миллиона явно отключает поиск. Обычный HTTP допустим только внутри предлагаемой приватной Docker-сети; удалённая схема требует отдельного проектирования.

## Українська

Пункт NoSQL реалізовано через Elasticsearch9.5.5 з ліцензією Basic і документованим REST API через окремий обмежений HttpClient. SDK клієнта, Kibana, платні хмари й зовнішні акаунти не потрібні. Пошук необов'язковий і типово вимкнений, зокрема для старих JSON. Публічно додано лише searchEnabled і searchQueryMaxLength. Попередні REST/GraphQL-читання, запис, CAPTCHA, preview, файли, скидання кешу й WS зберігають свої шляхи. Health/запуск app та БД не залежать від Elasticsearch.

GET /api/comments/search?query=words&page=1 шукає за текстом і публічним ім'ям автора, включно з відповідями. Довжина, розмір/вікно сторінок, строк і одночасність обмежені: хибні запити400, вимкнення404, перевантаження429, недоступна/часткова/хибна відповідь провайдера503. Використано буквальний multi_match з явними полями, не користувацький DSL/wildcards/regex. Стандартний Unicode-аналіз підтримує слова EN/RU/UA; морфологію/нечіткий/підрядковий пошук не обіцяно. Узгодженість відкладена; порядок - релевантність, дата, UUID, сторінки попередня/наступна без точного загального лічильника. За зміни індексу записи можуть переміщуватися між запитами.

Індекс містить лише id/userName/видимий текст/createdAt, strict mapping schema1, один shard/без replicas та вимкнений _source. E-mail/homepage/IP/User-Agent і вкладення не індексуються. Індекс повертає ID; один обмежений SQL-запит читає поточні публічні CommentDto в початковому порядку, приховуючи видалені/застарілі записи. Текст береться з SQL і виводиться попереднім безпечним CommentText, без сирого HTML highlight. Окремі форма/результати/помилки/скасування пошуку зберігають чернетку, сторінку й сортування основного списку. «Відкрити обговорення» отримує корінь через REST; знайдену відповідь показано окремо й доступно для відповіді через попередню форму.

Окреме підключення/тимчасова обмежена drop-head черга RabbitMQ передає ID без очікування Elasticsearch і не змінює попередню доставку/локальний fallback. Фоновий worker читає SQL та індексує за UUID. Пропуски сигналів і відмови брокера/пошуку виправляються повторними SQL-сканами за UUID й обмеженою звіркою/видаленням сиріт індексу. Курсор просувається лише після успішного запису; після рестарту починає спочатку. Таблиця checkpoint/міграція SQL не потрібні. Вставки позаду курсора потраплять у наступному циклі. Строк залежить від обсягу/темпу, фіксованого SLA свіжості й довговічної доставки немає. Ack означає приймання сигналу в пам'ять, не довговічну індексацію.

Bulk обмежено1МіБ, batch - до100записів; окремі scoped SQL-читання, паузи й backoff. М'який MaximumIndexBytes зупиняє нові записи за перевищення, можливий вихід на розмір batch; disk watermarks резервують місце. Контейнер:1,5GiB RAM/heap512MiB/1CPU, mmap вимкнено без host sysctl, власний похідний том search_data, порти не опубліковані. Таймаут дає помилку лише пошуку; читання/запис тривають, попередні результати/чернетка залишаються. Запуск без Elasticsearch і відновлення без рестарту app перевіряються окремо.

Автентифікацію ввімкнено. Bootstrap-пароль лише в серверному env, у app окремий comments_search з правами читання/запису/створення/метаданих/monitor лише comments-* без cluster admin. Автостворення індексів сервером вимкнено; адаптер явно створює strict mapping. Index - базовий префікс; app завжди додає стабільний12hex SHA-256 розділ імені БД. Три наявні БД ізольовані навіть зі спільним конфігом. Різним розгортанням на одному пошуковому кластері потрібні різні префікси. Тимчасовий app вимірювань мільйона явно вимикає пошук. Звичайний HTTP допустимий лише всередині запропонованої приватної Docker-мережі; віддалена схема потребує окремого проєктування.

## Configuration / Настройки / Налаштування

Runtime.Search keys; defaults and limits / значения и пределы / значення й межі:

| Key | Default | Range |
| --- | --- | --- |
| Enabled | false | boolean |
| Endpoint | http://elasticsearch:9200 | HTTP(S) origin, no userinfo/path/query/fragment |
| Username | comments_search | 1..64 characters, no controls |
| Index | comments-v1 | comments- prefix, <=100 lowercase ASCII/digits/hyphen/underscore |
| MaximumQueryLength | 200 | 1..500 |
| PageSize | 10 | 1..25 |
| MaximumPage | 100 | 1..100 |
| TimeoutSeconds | 2 | 1..5 |
| MaximumConcurrentRequests | 4 | 1..8 |
| BatchSize | 50 | 1..100 |
| BatchDelayMilliseconds | 1000 | 100..60000 |
| ReconcileIntervalSeconds | 30 | 5..3600, after a complete scan/cleanup cycle |
| FailureRetrySeconds | 5 | 1..60 |
| Capacity | 256 | 1..2048 |
| MaximumIndexBytes | 1073741824 | 1048576..2147483648, soft guard |

SEARCH_PASSWORD is an environment secret, not a JSON/public field / секрет окружения, не JSON/публичное поле / секрет оточення, не JSON/публічне поле. Changes require restart / Изменения требуют перезапуска / Зміни потребують перезапуску.

## Optional installation / Необязательная установка / Необов'язкове встановлення

Retain your existing project/environment/overrides/volumes. This is the portable default example / Сохраните прежние проект/окружение/overrides/тома; переносимый пример / Збережіть попередні проєкт/оточення/overrides/томи; переносний приклад:

```bash
python3 scripts/init-search-env.py
docker compose -p commentsservice --profile search up -d --no-deps --wait elasticsearch
python3 scripts/configure-search-user.py
docker compose -p commentsservice build app
docker compose -p commentsservice up -d --no-deps --no-build --wait app
```

EN: Keep .env.search and .env.elasticsearch separate/private/ignored; the server file is not supplied to the app. Preserve existing secrets when recreating services. Existing broker setup remains optional; search reconciliation works without RabbitMQ. No PostgreSQL migration/data move/backup replacement is needed. An index is a disposable derivative, not a replacement for relational backups. It can be rebuilt from current SQL by the worker; large corpora require controlled pacing and capacity planning. Rollback uses the old app image and existing original environment/volumes; disable search and reload the browser. Stop only the owned search service if desired. Never restore an old SQL database or delete original volumes to roll back search code.

RU: .env.search и .env.elasticsearch хранить раздельно/приватно/вне Git; серверный файл не передавать app. При пересоздании сохранять секреты. Настройка брокера остаётся необязательной, сверка поиска работает без RabbitMQ. Миграция PostgreSQL/перенос/замена копий не нужны. Индекс производный, не замена SQL-backup; worker восстанавливает его из текущей SQL, большой объём требует контролируемого темпа/ёмкости. Откат - прежний образ app и исходные окружение/тома, отключение поиска и перезагрузка страницы. При необходимости остановить только свой поисковый сервис. Нельзя восстанавливать старую SQL или удалять исходные тома ради отката поиска.

UA: .env.search та .env.elasticsearch зберігати окремо/приватно/поза Git; серверний файл не передавати app. За перестворення зберігати секрети. Налаштування брокера залишається необов'язковим, звірка пошуку працює без RabbitMQ. Міграція PostgreSQL/перенесення/заміна копій не потрібні. Індекс похідний, не заміна SQL-backup; worker відновлює його з поточної SQL, великий обсяг потребує контрольованого темпу/ємності. Відкат - попередній образ app і початкові оточення/томи, вимкнення пошуку та перезавантаження сторінки. За потреби зупинити лише власний пошуковий сервіс. Не можна відновлювати стару SQL чи видаляти початкові томи задля відкату пошуку.

Sources: [Docker deployment](https://www.elastic.co/docs/deploy-manage/deploy/self-managed/install-elasticsearch-docker-basic), [multi-match query](https://www.elastic.co/docs/reference/query-languages/query-dsl/query-dsl-multi-match-query), [bulk API](https://www.elastic.co/docs/api/doc/elasticsearch/operation/operation-bulk).

## Verification / Проверки / Перевірки

EN: Verification: 418 distinct backend tests (27/85/67/239), 36 frontend tests, frontend/Docker builds; real Edge EN/RU/UA search, selected reply/discussion, ES pause/stop/recovery while posting/CAPTCHA/preview/exact TXT/GraphQL six sorts/WS/drafts remain usable; SQL reconciliation after RabbitMQ loss; offline startup and recovery without app restart; strict private mapping, scoped credentials, orphan cleanup and index reconstruction. Original data/schema/history/million/checkpoints/env/volumes/preview preserved. Earlier million paging/reboot/heartbeat acceptance was not repeated.

RU: Проверки: 418 различных backend-тестов (27/85/67/239), 36 frontend, сборки frontend/Docker; Edge EN/RU/UA, найденный ответ/обсуждение, пауза/остановка/возврат ES при работающих записи/CAPTCHA/preview/точном TXT/6сортировках GraphQL/WS/черновиках; сверка SQL после потери RabbitMQ; запуск без ES и восстановление без рестарта app; приватный mapping, ограниченные права, удаление сирот и восстановление индекса. Исходные данные/схема/история/миллион/копии/env/тома/preview сохранены. Прежние проверки пагинации миллиона/reboot/heartbeat не повторялись.

UA: Перевірки: 418 різних backend-тестів (27/85/67/239), 36 frontend, збирання frontend/Docker; Edge EN/RU/UA, знайдена відповідь/обговорення, пауза/зупинка/повернення ES за працюючих запису/CAPTCHA/preview/точного TXT/6сортувань GraphQL/WS/чернеток; звірка SQL після втрати RabbitMQ; запуск без ES і відновлення без рестарту app; приватний mapping, обмежені права, видалення сиріт і відновлення індексу. Початкові дані/схема/історія/мільйон/копії/env/томи/preview збережено. Попередні перевірки пагінації мільйона/reboot/heartbeat не повторювались.

EN: Public read-only acceptance passed HTTPS/HSTS, actual search and discussion navigation, GraphQL, database health, WebSocket reconnect/draft retention and360px layout. Live app/RabbitMQ/Elasticsearch are healthy. The live derived index is about24KB; original SQL fingerprints remain unchanged. No public comments were created for these checks.

RU: Публично только чтением прошли HTTPS/HSTS, реальный поиск/обсуждение, GraphQL, health БД, переподключение WS/черновик и360px. App/RabbitMQ/Elasticsearch здоровы. Рабочий производный индекс около24KB, исходные отпечатки SQL прежние. Публичные комментарии для проверок не создавались.

UA: Публічно лише читанням пройшли HTTPS/HSTS, справжній пошук/обговорення, GraphQL, health БД, перепідключення WS/чернетка й360px. App/RabbitMQ/Elasticsearch здорові. Робочий похідний індекс близько24KB, початкові відбитки SQL попередні. Публічні коментарі для перевірок не створювались.
