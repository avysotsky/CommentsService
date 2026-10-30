# Persistent paging corpus / Постоянный набор / Постійний набір — 1.1.0.8

## English

Follow-up 1.1.0.10: the same paging-v1 corpus was migrated in place with ImageAttachments (empty image table), then all 18 cases passed again using one sample per case with the metadata-only image projection. No reseeding or corpus row changes occurred. The three-sample table below remains the original 1.1.0.8 measurement.

Opt-in `paging-v1`, version 1: 1,000,000 deterministic comments (800,000 roots, 200,000 replies, four reply levels in 50,000 chains), 10,000 author snapshots and 32,000 root pages. It lives in a separate `comments_benchmark` database inside the persistent test PostgreSQL service/volume. Development `comments` and functional `comments_test` remain unchanged. No additional volume or external service is required. The current application schema includes InitialDataModel + CommentThreads + ImageAttachments; generator-only metadata is in `benchmark.Dataset`.

Preparation creates the owned database only if absent, explicitly migrates it with the ordinary non-superuser application role, and seeds in one transaction under a database-local advisory lock. UUIDs, author metadata and timestamps are deterministic. Subsequent runs verify version/count and **reuse** the completed corpus without row writes; mismatches refuse implicit replacement. `--rows 100..1000000` supports a smaller initial corpus; always reuse the same count afterwards. Do not run the generator as functional-test setup or change/delete this data between measurements.

Build the current app and start both database services first, using the README deployment steps. From the repository root:

```bash
python3 scripts/prepare-benchmark.py
python3 scripts/measure-benchmark.py --start-api
# Before future schema changes that must preserve this corpus too:
python3 scripts/db-checkpoint.py --include-benchmark
python3 scripts/db-command.py migrate --benchmark
python3 scripts/db-command.py check --benchmark
```

All scripts accept `--env-file`, `--project` and repeated `--compose-file`; use matching options. `--start-api` creates a uniquely named temporary app against the benchmark DB, binds only to the loopback URL (default 8083), reads private test credentials without printing them, and removes only its verified owned container. It neither deletes the DB nor seeds it. Without that flag, provide an existing benchmark API via `--base-url`. The measurement command issues GETs only, runs three samples per sort/direction on first/middle/last pages, verifies counts/roots/stable IDs, and saves `artifacts/benchmark-paging-v1.json`. For a smaller corpus pass the matching `--expected-roots`.

The normal checkpoint command covers development/tests only. `--include-benchmark` additionally restores and fingerprints this corpus and writes `benchmark.dump`/`benchmark.json`; use it before future benchmark schema/data changes. Routine app updates preserve the benchmark DB and volume. No scheduled/off-host backup coverage is implied. The corpus is synthetic and reproducible; retain its version and metadata for comparable runs.

Recorded on 2026-10-30: one warmed containerized .NET 10/PostgreSQL 17 run, sequential requests, three samples/case, medians below. Counts and items, HTTP overhead and JSON are included. All 18 cases returned 25 roots and stable repeated IDs. Full-row aggregate fingerprints and dataset metadata matched before/after reuse and measurement. These are local observations, not an SLA. Numeric OFFSET and joining/sorting authors make deep name/email pages more expensive; this release adds no denormalization/cache or speculative index migration.

## Русский

Проверка 1.1.0.10: к тому же paging-v1 применена ImageAttachments (пустая таблица изображений), затем повторно прошли 18 сценариев по одному замеру с проекцией метаданных картинки. Перегенерации и изменений строк набора не было. Таблица трёх повторов ниже остаётся исходным замером 1.1.0.8.

Необязательный `paging-v1`, версия 1: 1 000 000 детерминированных комментариев (800 000 корней, 200 000 ответов, четыре уровня в 50 000 цепочек), 10 000 снимков авторов, 32 000 страниц корней. Набор хранится в отдельной БД `comments_benchmark` постоянного тестового PostgreSQL/тома. Рабочая `comments` и функциональная `comments_test` не меняются. Дополнительный том/внешний сервис не нужен. Текущая схема приложения — InitialDataModel + CommentThreads + ImageAttachments; метаданные генератора — `benchmark.Dataset`.

Подготовка создаёт свою БД только при отсутствии, явно применяет миграции обычной ролью приложения без суперправ и заполняет данные одной транзакцией с advisory lock внутри БД. UUID, авторы и время детерминированы. Повтор проверяет версию/число строк и **использует сохранённый набор без записи**; несовпадение запрещает неявную замену. `--rows 100..1000000` задаёт меньший первоначальный набор; далее используйте то же число. Не включайте генерацию в setup функциональных тестов и не изменяйте/удаляйте набор между замерами.

Сначала соберите приложение и запустите оба сервиса БД по README. Из корня репозитория:

```bash
python3 scripts/prepare-benchmark.py
python3 scripts/measure-benchmark.py --start-api
# Before future schema changes that must preserve this corpus too:
python3 scripts/db-checkpoint.py --include-benchmark
python3 scripts/db-command.py migrate --benchmark
python3 scripts/db-command.py check --benchmark
```

Все скрипты поддерживают `--env-file`, `--project` и повторяемый `--compose-file`; параметры должны совпадать. `--start-api` создаёт уникальный временный API для benchmark-БД, слушает только loopback (по умолчанию 8083), читает приватный тестовый пароль без вывода и удаляет только проверенный собственный контейнер. БД не удаляется и не заполняется. Без флага укажите готовый benchmark API через `--base-url`. Замер делает только GET, три повтора каждой сортировки/порядка на первой/средней/последней страницах, проверяет количество/корни/стабильные ID и сохраняет `artifacts/benchmark-paging-v1.json`. Для меньшего набора задайте соответствующий `--expected-roots`.

Обычная контрольная копия охватывает только рабочую/тестовую БД. `--include-benchmark` дополнительно восстанавливает и сравнивает отпечатки этого набора, записывая `benchmark.dump`/`benchmark.json`; используйте до будущего изменения схемы/данных benchmark. Обычное обновление сохраняет БД и том. Наличие планового/внешнего backup не подразумевается. Набор синтетический и воспроизводимый; версия/метаданные нужны для сравнения замеров.

Замер 2026-10-30: прогретые контейнеры .NET 10/PostgreSQL 17, последовательные запросы, три повтора, ниже медианы. Включены количество/строки, HTTP и JSON. Все 18 сценариев вернули 25 корней и стабильные ID. Полные агрегатные отпечатки строк и метаданные совпали до/после повторного использования и замера. Это локальные наблюдения, не SLA. Числовой OFFSET и соединение/сортировка авторов удорожают глубокие страницы имени/email; денормализация/кеш и спекулятивные миграции индексов не добавлены.

## Українська

Перевірка 1.1.0.10: до того самого paging-v1 застосовано ImageAttachments (порожня таблиця зображень), тоді повторно пройшли 18 сценаріїв по одному заміру з проєкцією метаданих зображення. Перегенерації та змін рядків набору не було. Таблиця трьох повторів нижче лишається початковим заміром 1.1.0.8.

Необов’язковий `paging-v1`, версія 1: 1 000 000 детермінованих коментарів (800 000 коренів, 200 000 відповідей, чотири рівні в 50 000 ланцюжках), 10 000 знімків авторів, 32 000 сторінок коренів. Набір зберігається в окремій БД `comments_benchmark` постійного тестового PostgreSQL/тому. Робоча `comments` і функціональна `comments_test` не змінюються. Додатковий том/зовнішній сервіс не потрібний. Поточна схема застосунку — InitialDataModel + CommentThreads + ImageAttachments; метадані генератора — `benchmark.Dataset`.

Підготовка створює власну БД лише за відсутності, явно застосовує міграції звичайною роллю застосунку без суперправ і заповнює дані однією транзакцією з advisory lock усередині БД. UUID, автори й час детерміновані. Повтор перевіряє версію/кількість рядків і **використовує збережений набір без запису**; невідповідність забороняє неявну заміну. `--rows 100..1000000` задає менший початковий набір; далі використовуйте ту саму кількість. Не додавайте генерацію до setup функціональних тестів і не змінюйте/видаляйте набір між замірами.

Спочатку зберіть застосунок і запустіть обидва сервіси БД за README. З кореня репозиторію:

```bash
python3 scripts/prepare-benchmark.py
python3 scripts/measure-benchmark.py --start-api
# Before future schema changes that must preserve this corpus too:
python3 scripts/db-checkpoint.py --include-benchmark
python3 scripts/db-command.py migrate --benchmark
python3 scripts/db-command.py check --benchmark
```

Усі скрипти підтримують `--env-file`, `--project` та повторюваний `--compose-file`; параметри мають збігатися. `--start-api` створює унікальний тимчасовий API для benchmark-БД, слухає лише loopback (типово 8083), читає приватний тестовий пароль без виведення та видаляє лише перевірений власний контейнер. БД не видаляється й не заповнюється. Без прапорця вкажіть готовий benchmark API через `--base-url`. Замір робить лише GET, три повтори кожного сортування/порядку на першій/середній/останній сторінках, перевіряє кількість/корені/стабільні ID та зберігає `artifacts/benchmark-paging-v1.json`. Для меншого набору задайте відповідний `--expected-roots`.

Звичайна контрольна копія охоплює лише робочу/тестову БД. `--include-benchmark` додатково відновлює та порівнює відбитки цього набору, записуючи `benchmark.dump`/`benchmark.json`; використовуйте перед майбутньою зміною схеми/даних benchmark. Звичайне оновлення зберігає БД й том. Наявність планового/зовнішнього backup не передбачається. Набір синтетичний і відтворюваний; версія/метадані потрібні для порівняння замірів.

Замір 2026-10-30: прогріті контейнери .NET 10/PostgreSQL 17, послідовні запити, три повтори, нижче медіани. Ураховано кількість/рядки, HTTP і JSON. Усі 18 сценаріїв повернули 25 коренів і стабільні ID. Повні агрегатні відбитки рядків та метадані збіглися до/після повторного використання й заміру. Це локальні спостереження, не SLA. Числовий OFFSET та з’єднання/сортування авторів здорожчують глибокі сторінки імені/email; денормалізацію/кеш та спекулятивні міграції індексів не додано.

## Measured medians / Медианы / Медіани

| Sort / Поле / Поле | Direction / Порядок / Порядок | 1 (ms) | 16000 (ms) | 32000 (ms) |
|---|---|---:|---:|---:|
| createdAt | asc | 51.93 | 134.66 | 267.32 |
| createdAt | desc | 91.51 | 166.46 | 316.51 |
| userName | asc | 276.67 | 1676.86 | 1794.43 |
| userName | desc | 323.61 | 1681.60 | 1821.36 |
| email | asc | 313.28 | 1338.42 | 1459.64 |
| email | desc | 289.70 | 1388.39 | 1787.20 |

## TextAttachments 1.1.0.11

EN: Reused paging-v1 after migration 20261030130243_TextAttachments: 1,000,000 comments, 800,000 roots, 200,000 replies. All 18 sort/direction/first-middle-last page cases passed (one sample each), stable repeated IDs and root-only results. Original full row fingerprints and Dataset metadata retained; no reseeding. This is a local regression measurement, not a universal latency guarantee.

RU: После миграции 20261030130243_TextAttachments повторно использован paging-v1: 1 000 000 комментариев, 800 000 корней, 200 000 ответов. Все 18 сочетаний сортировки/направления/первой-средней-последней страницы прошли (по одному измерению), повторные ID стабильны, выдаются только корни. Полные отпечатки исходных строк и Dataset сохранены; повторного заполнения нет. Это локальная проверка регрессий, не гарантия задержки.

UA: Після міграції 20261030130243_TextAttachments повторно використано paging-v1: 1 000 000 коментарів, 800 000 коренів, 200 000 відповідей. Усі 18 поєднань сортування/напрямку/першої-середньої-останньої сторінки пройшли (по одному вимірюванню), повторні ID стабільні, видаються лише корені. Повні відбитки початкових рядків і Dataset збережено; повторного заповнення немає. Це локальна перевірка регресій, не гарантія затримки.
