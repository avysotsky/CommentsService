# Docker/.NET tools — 1.3.3.4

## English

The supported installation, recovery and Middle acceptance route uses Docker Engine and Compose only. .NET10 SDK, PostgreSQL17 clients and the maintenance executable are inside a separate tools image. Python and host .NET SDK are not required. The app remains C#/.NET; optional Node24 is for frontend development tests only. Existing .py scripts are retained as optional legacy developer compatibility, not called by this route. MySQL Workbench's own embedded scripting is unrelated to application installation.

Build tools once and generate .env before parsing the main Compose file, whose credentials are required:

```bash
docker compose -p commentsservice -f compose.tools.yaml build tools
docker compose -p commentsservice -f compose.tools.yaml run --rm --no-deps tools init-env
```

Then follow First installation in the README. Later commands include both Compose files so existing services are known; keep the same -p project and private environment on all calls. Never add --remove-orphans to silence a warning. compose.tools.yaml defines a profiled one-shot service, no published ports, no Docker socket and no persistent tools data. --rm removes the helper and its inherited disposable client-image volume. PostgreSQL, broker/search and the app are not started implicitly by tools. Start required services explicitly. The root workspace is mounted for input/config/dump output; it must be your trusted checkout. Default UID/GID1000 creates private env/dump files. On a Linux host owned by another account, set TOOLS_UID/TOOLS_GID to the numeric owner IDs before running the command; use normal Docker permissions/sudo as required. Do not make secrets world-readable.

The private .env contains the database credentials, .env.broker the matching Rabbit credentials, .env.search limited search credentials, .env.elasticsearch separate admin credentials, and .env.cloud backend-only Supabase settings. Generated files are created exclusively, with0600 on Unix. Existing valid files are preserved; incomplete/mismatched files fail without replacing credentials. No key or provider error body is printed. Do not pass passwords as CLI arguments, publish env files or copy them into image layers.

Use --target development (default), test or benchmark for database operations. The hostnames database/test-database resolve inside the selected Compose project network, port5432. Tools read --env-file .env by default. This tool option is placed after the command; Docker Compose's --env-file/-p/-f options belong before run. Existing non-default deployments must retain their private env/overrides and use the same project network; do not use the default example to create a parallel replacement of an existing deployment.

```bash
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools check
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools migrate --target test
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools checkpoint
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-checkpoint --dump artifacts/checkpoints/database.dump --target test --upgrade
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-schema
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools test-backend --target test
```

migrate/check use the application role and the existing EF migrations. No migration is automatic. Before retained schema/data changes stop all writers and obtain a restore-verified checkpoint; follow the agreed migration/recovery procedure. checkpoint defaults to development+test and supports --include-benchmark only when that retained DB exists; --target selects one DB. It compares source/restored/source row fingerprints and promotes a candidate only after verification. verify-checkpoint validates SHA256 before creating an owned UUID recovery DB, restores as comments_app, optionally upgrades using the shipped idempotent SQL and verifies previous data/idempotency. It checks ownership and removes only that temporary DB. Original dumps remain untouched. Old checkpoint manifests remain compatible. verify-schema compares actual SQL and Workbench columns/types/nullability/indexes/PK/FK/checks/history; it does not claim manual GUI behavior. test-backend requires --target test, runs4suites sequentially and uses the existing fixture/rollback cleanup; it never reseeds the million corpus.

configure-search-user accepts only the private elasticsearch service and limited comments_search namespace. It retries temporary network/5xx/429 readiness failures with a120second deadline per role/user operation and5second request limit; auth failures stop. The app never receives admin credentials. verify-cloud-demo is read-only, needs exactly two own comment IDs/author marker, uses --base-url http://app:8080 inside Docker and --env-file .env.cloud; it verifies real bytes/hash and public denial with a2MiB/9request budget, no retries or uploads. Follow [owner-controlled Cloud demonstration](MIDDLE_ACCEPTANCE.md). Budgets are not durable monthly accounting. No user-data bulk transfer, outbox, replica or paid upgrade is enabled by tools.

## Русский

Поддерживаемый путь установки, восстановления и приёмки Middle требует только Docker Engine и Compose. .NET10 SDK, клиенты PostgreSQL17 и утилита обслуживания находятся в отдельном образе. Python и .NET SDK на хосте не нужны. Приложение остаётся C#/.NET; необязательный Node24 нужен только тестам разработки frontend. Прежние .py сохранены для необязательной совместимости разработки, этот путь их не вызывает. Встроенный scripting MySQL Workbench не относится к установке приложения.

Сначала собрать инструменты и создать .env, затем читать основной Compose, требующий пароли:

```bash
docker compose -p commentsservice -f compose.tools.yaml build tools
docker compose -p commentsservice -f compose.tools.yaml run --rm --no-deps tools init-env
```

Далее первая установка README. Последующие команды включают оба Compose-файла, чтобы видеть существующие сервисы; сохранять одинаковые -p проект и приватное окружение. Не добавлять --remove-orphans ради предупреждения. compose.tools.yaml содержит профилированный одноразовый сервис без опубликованных портов, Docker socket и постоянных данных tools. --rm удаляет helper и его унаследованный одноразовый том образа клиентов. Tools не запускает БД/брокер/поиск/app неявно: нужные сервисы запускаются отдельно. Рабочая папка монтируется для конфигурации/копий и должна быть доверенной копией проекта. UID/GID1000 по умолчанию создаёт приватные env/dump. Если владелец каталога Linux другой, задать TOOLS_UID/TOOLS_GID его числовыми ID; использовать штатные Docker-права/sudo. Секреты не делать общедоступными.

Приватный .env хранит пароли БД, .env.broker — согласованные Rabbit-пароли, .env.search — ограниченные поисковые права, .env.elasticsearch — отдельные admin-права, .env.cloud — backend-настройки Supabase. Создание эксклюзивное,0600 в Unix. Существующие корректные файлы сохраняются; неполные/несогласованные завершаются ошибкой без замены ключей. Секреты/тела ошибок провайдера не печатаются. Пароли не передавать аргументами CLI, не публиковать и не включать в образ.

Цели БД: --target development по умолчанию, test или benchmark. Имена database/test-database разрешаются внутри сети выбранного Compose-проекта, порт5432. Tools читает --env-file .env; этот параметр после имени команды. Параметры Docker Compose --env-file/-p/-f находятся до run. Для существующей нестандартной установки сохранять приватные env/overrides и прежнюю сеть проекта; не создавать параллельную замену по примеру по умолчанию.

```bash
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools check
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools migrate --target test
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools checkpoint
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-checkpoint --dump artifacts/checkpoints/database.dump --target test --upgrade
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-schema
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools test-backend --target test
```

migrate/check используют роль приложения и прежние EF-миграции, автоматических миграций нет. Перед изменением сохранённой схемы/данных остановить всех писателей, получить проверенную восстановлением копию и следовать согласованному порядку. checkpoint по умолчанию обрабатывает development+test; --include-benchmark только при наличии сохранённой БД, --target выбирает одну БД. Сравнивает отпечатки исходной/восстановленной/повторно исходной БД и заменяет прежнюю копию только после успеха. verify-checkpoint проверяет SHA256 до создания собственной UUID-БД, восстанавливает под comments_app, необязательно обновляет идемпотентным SQL с проверкой прежних данных/повторного применения. Проверяет владельца и удаляет только временную БД; исходный dump не меняется. Прежние manifest совместимы. verify-schema сравнивает SQL/Workbench по колонкам/типам/nullability/индексам/PK/FK/checks/истории, без заявления ручной GUI-проверки. test-backend требует --target test, запускает4набора последовательно с прежней очисткой фикстур/откатом, миллион не пересоздаёт.

configure-search-user работает только с приватным сервисом elasticsearch и ограниченным comments_search. Временные сеть/5xx/429 повторяются с общим сроком120секунд на роль/пользователя и5секунд на запрос; ошибки авторизации останавливают. Admin-права app не передаются. verify-cloud-demo только читает: ровно2своих ID/маркер автора, --base-url http://app:8080 внутри Docker и --env-file .env.cloud; проверяет настоящие байты/хеш/запрет публичного доступа с бюджетом2МиБ/9запросов, без повторов/загрузки. См.[демонстрацию владельца](MIDDLE_ACCEPTANCE.md). Бюджет не является постоянным месячным учётом. Массовая передача данных пользователя, outbox, реплики и платные переходы не включаются.

## Українська

Підтримуваний шлях інсталяції, відновлення й приймання Middle потребує лише Docker Engine і Compose. .NET10 SDK, клієнти PostgreSQL17 та утиліта обслуговування містяться в окремому образі. Python і .NET SDK на хості не потрібні. Застосунок залишається C#/.NET; необов’язковий Node24 потрібен лише тестам розробки frontend. Попередні .py збережені для необов’язкової сумісності розробки, цей шлях їх не викликає. Вбудований scripting MySQL Workbench не належить до інсталяції застосунку.

Спочатку зібрати інструменти й створити .env, потім читати основний Compose, що потребує паролів:

```bash
docker compose -p commentsservice -f compose.tools.yaml build tools
docker compose -p commentsservice -f compose.tools.yaml run --rm --no-deps tools init-env
```

Далі перша інсталяція README. Наступні команди включають обидва Compose-файли, щоб бачити наявні сервіси; зберігати однакові -p проєкт і приватне оточення. Не додавати --remove-orphans заради попередження. compose.tools.yaml містить профільований одноразовий сервіс без опублікованих портів, Docker socket і постійних даних tools. --rm видаляє helper та його успадкований одноразовий том образу клієнтів. Tools не запускає БД/брокер/пошук/app неявно: потрібні сервіси запускаються окремо. Робоча папка монтується для конфігурації/копій і має бути довіреною копією проєкту. UID/GID1000 типово створює приватні env/dump. Якщо власник каталогу Linux інший, задати TOOLS_UID/TOOLS_GID його числовими ID; використовувати штатні Docker-права/sudo. Секрети не робити загальнодоступними.

Приватний .env зберігає паролі БД, .env.broker — узгоджені Rabbit-паролі, .env.search — обмежені пошукові права, .env.elasticsearch — окремі admin-права, .env.cloud — backend-налаштування Supabase. Створення ексклюзивне,0600 в Unix. Наявні коректні файли зберігаються; неповні/неузгоджені завершуються помилкою без заміни ключів. Секрети/тіла помилок провайдера не друкуються. Паролі не передавати аргументами CLI, не публікувати й не включати до образу.

Цілі БД: --target development типово, test або benchmark. Імена database/test-database визначаються в мережі обраного Compose-проєкту, порт5432. Tools читає --env-file .env; цей параметр після імені команди. Параметри Docker Compose --env-file/-p/-f розташовані до run. Для наявної нестандартної інсталяції зберігати приватні env/overrides і попередню мережу проєкту; не створювати паралельну заміну за типовим прикладом.

```bash
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools check
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools migrate --target test
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools checkpoint
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-checkpoint --dump artifacts/checkpoints/database.dump --target test --upgrade
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-schema
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools test-backend --target test
```

migrate/check використовують роль застосунку та попередні EF-міграції, автоматичних міграцій немає. Перед зміною збереженої схеми/даних зупинити всіх авторів запису, отримати перевірену відновленням копію й дотримуватися погодженого порядку. checkpoint типово обробляє development+test; --include-benchmark лише за наявності збереженої БД, --target обирає одну БД. Порівнює відбитки початкової/відновленої/повторно початкової БД та замінює попередню копію лише після успіху. verify-checkpoint перевіряє SHA256 до створення власної UUID-БД, відновлює під comments_app, необов’язково оновлює ідемпотентним SQL із перевіркою попередніх даних/повторного застосування. Перевіряє власника й видаляє лише тимчасову БД; початковий dump не змінюється. Попередні manifest сумісні. verify-schema порівнює SQL/Workbench за колонками/типами/nullability/індексами/PK/FK/checks/історією, без заяви ручної GUI-перевірки. test-backend потребує --target test, запускає4набори послідовно з попереднім очищенням фікстур/відкатом, мільйон не перестворює.

configure-search-user працює лише з приватним сервісом elasticsearch та обмеженим comments_search. Тимчасові мережа/5xx/429 повторюються зі спільним строком120секунд на роль/користувача та5секунд на запит; помилки авторизації зупиняють. Admin-права app не передаються. verify-cloud-demo лише читає: рівно2власні ID/маркер автора, --base-url http://app:8080 усередині Docker та --env-file .env.cloud; перевіряє справжні байти/хеш/заборону публічного доступу з бюджетом2МіБ/9запитів, без повторів/завантаження. Див.[демонстрацію власника](MIDDLE_ACCEPTANCE.md). Бюджет не є постійним місячним обліком. Масове передавання даних користувача, outbox, репліки та платні переходи не вмикаються.


## DockerTools verification / Проверки / Перевірки

EN: Verified Docker-only installation in an isolated empty project: environment creation/preservation; two initial dumps restored and fingerprinted; explicit development/test migrations; schema parity; restore/upgrade/idempotency across servers; scoped Rabbit/search setup. The tools container has no python3 executable and no Docker socket. All449backend tests passed from the tools container (27/85/67/270). Real Edge and all Middle services passed; the C# Cloud verifier checked2own files267bytes with public denial, while an unlisted file stayed SQL-only. Negative cases rejected incomplete env, invalid target, bad checkpoint SHA, altered schema and wrong ES credentials, preserving files and hiding secrets. A populated own PNG/TXT/reply checkpoint also restored/upgraded correctly. Runtime and primary Compose remain unchanged; the tooling image alone provides the new route.

RU: Проверена установка только Docker в отдельном пустом проекте: создание/сохранение env, восстановление/отпечатки двух начальных копий, явные миграции development/test, паритет схемы, восстановление/обновление/идемпотентность между серверами, Rabbit/ограниченный поиск. В tools нет исполняемого python3 и Docker socket. Все449backend прошли из контейнера tools(27/85/67/270). Edge/все сервисы Middle прошли; C#-проверка Cloud сверила2своих файла267байт и запрет публичного доступа, файл вне списка остался SQL. Негативные случаи отклонили неполный env, неверную цель, SHA копии, изменённую схему и неверные ES-права, сохранив файлы и скрыв секреты. Заполненная своими PNG/TXT/ответом копия также восстановилась/обновилась. Runtime/основной Compose прежние, новый путь обеспечивает только образ инструментов.

UA: Перевірено інсталяцію лише Docker в окремому порожньому проєкті: створення/збереження env, відновлення/відбитки двох початкових копій, явні міграції development/test, паритет схеми, відновлення/оновлення/ідемпотентність між серверами, Rabbit/обмежений пошук. У tools немає виконуваного python3 і Docker socket. Усі449backend пройшли з контейнера tools(27/85/67/270). Edge/усі сервіси Middle пройшли; C#-перевірка Cloud звірила2власні файли267байтів і заборону публічного доступу, файл поза списком залишився SQL. Негативні випадки відхилили неповний env, хибну ціль, SHA копії, змінену схему й хибні ES-права, зберігши файли та приховавши секрети. Заповнена власними PNG/TXT/відповіддю копія також відновилася/оновилася. Runtime/основний Compose попередні, новий шлях забезпечує лише образ інструментів.
