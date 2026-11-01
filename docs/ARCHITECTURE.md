# Backend architecture / Архитектура backend / Архітектура backend

EN: Optional Elasticsearch full-text search now finds roots and replies by words or author name, with paging and discussion navigation. Search has separate bounded requests, background indexing and a RabbitMQ consumer; SQL reconciliation repairs missed signals and stale entries. PostgreSQL remains authoritative and all earlier functionality remains independent of Elasticsearch availability. Indexes are automatically partitioned by database. No SQL migration, outbox, cloud or replica deployment. [Search](COMMENT_SEARCH.md).

RU: Необязательный полнотекстовый поиск Elasticsearch находит корни и ответы по словам или имени автора, с пагинацией и переходом к обсуждению. Запросы, фоновая индексация и потребитель RabbitMQ изолированы; SQL-сверка исправляет пропуски и устаревшие записи. Источник истины - PostgreSQL, прежние функции не зависят от доступности Elasticsearch. Индексы автоматически разделены по БД. Без миграции SQL, outbox, Cloud и реплик. [Search](COMMENT_SEARCH.md).

UA: Необов’язковий повнотекстовий пошук Elasticsearch знаходить корені й відповіді за словами чи ім’ям автора, з пагінацією та переходом до обговорення. Запити, фонову індексацію й споживача RabbitMQ ізольовано; SQL-звірка виправляє пропуски й застарілі записи. Джерело істини - PostgreSQL, попередні функції не залежать від доступності Elasticsearch. Індекси автоматично розділено за БД. Без міграції SQL, outbox, Cloud і реплік. [Search](COMMENT_SEARCH.md).

EN: RabbitMQ now carries comment-change signals in the background. If disabled, disconnected, stalled or overloaded, the existing local queue keeps WebSocket refresh working; comment writes, GraphQL/REST, CAPTCHA, preview and files remain independent of the broker. Runtime.Broker adds private validated settings; credentials are separate environment secrets. PostgreSQL schema/data and the public configuration contract are unchanged. No outbox, NoSQL, cloud or replica deployment. [Broker](MESSAGE_BROKER.md).

RU: RabbitMQ передаёт сигналы изменения комментариев в фоне. При отключении, потере связи, зависании или перегрузке прежняя локальная очередь сохраняет WebSocket-обновления; запись, GraphQL/REST, CAPTCHA, preview и файлы не зависят от брокера. Runtime.Broker содержит приватные проверяемые параметры; секреты вынесены в отдельное окружение. Схема/данные PostgreSQL и публичный контракт конфига прежние. Outbox, NoSQL, Cloud и реплики не добавлены. [Broker](MESSAGE_BROKER.md).

UA: RabbitMQ передає сигнали зміни коментарів у фоні. За вимкнення, втрати зв’язку, зависання чи перевантаження попередня локальна черга зберігає WebSocket-оновлення; запис, GraphQL/REST, CAPTCHA, preview та файли не залежать від брокера. Runtime.Broker містить приватні перевірювані параметри; секрети винесено в окреме оточення. Схема/дані PostgreSQL і публічний контракт конфігу попередні. Outbox, NoSQL, Cloud і репліки не додані. [Broker](MESSAGE_BROKER.md).

EN: GraphQL adds a bounded read-only /api/graphql endpoint and uses it for SPA root-page reads. Existing REST writes/replies/files, Events/Cache/Queue/WebSocket and PostgreSQL data remain. Runtime.GraphQL adds validated limits; only graphQLEnabled is public. No schema migration, broker, NoSQL or cloud. This completes the Graph item only, not the entire Middle level. [GraphQL](GRAPHQL.md).

RU: GraphQL добавляет ограниченный endpoint чтения /api/graphql и чтение корневой страницы SPA через него. Прежние REST-запись/ответы/файлы, Events/Cache/Queue/WebSocket и данные PostgreSQL сохранены. Runtime.GraphQL задаёт проверяемые лимиты; публичен только graphQLEnabled. Миграций схемы, брокера, NoSQL и Cloud нет. Выполнен только пункт Graph, а не весь Middle. [GraphQL](GRAPHQL.md).

UA: GraphQL додає обмежений endpoint читання /api/graphql і читання кореневої сторінки SPA через нього. Попередні REST-запис/відповіді/файли, Events/Cache/Queue/WebSocket та дані PostgreSQL збережено. Runtime.GraphQL задає перевірювані межі; публічний лише graphQLEnabled. Міграцій схеми, брокера, NoSQL та Cloud немає. Виконано лише пункт Graph, а не весь Middle. [GraphQL](GRAPHQL.md).

## English

TXT attachments (1.1.0.11): roots and replies accept one optional attachment: an image OR a `.txt` file. TXT must contain 1..102400 bytes (100 KiB), strict UTF-8; BOM, Unicode, CR/LF and tabs are preserved byte-for-byte. Invalid encoding and control characters other than CR/LF/tab are rejected; HTML/script text is accepted as literal text. Filename/MIME do not establish content validity. Multipart uses `textFile` with the usual comment/CAPTCHA fields; never send both file parts. JSON text-only creation remains available. Invalid TXT returns 400/errors.textFile. CAPTCHA is consumed before content processing; retry with a fresh code. The localized picker keeps draft/file on errors, clears on success and permits removing/replacing a selection.

DTOs add nullable `textFile: {id,fileName,sizeBytes}`; list/page/reply projections omit file bytes. `GET /api/text-files/{id}` downloads exact bytes as `text/plain; charset=utf-8`, generated UUID `.txt` filename, attachment disposition, nosniff, no-store and sandbox CSP; missing files return 404. HTML is never rendered. FileViewer is available in 1.1.0.12.

Upgrade 1.1.0.10 → 1.1.0.11: stop all writers, restore-verify a current checkpoint (include the benchmark when present), build, explicitly migrate every used database, then restart using the existing volumes. Migration `20261030130243_TextAttachments` adds TextAttachments: Id, unique CommentId FK CASCADE, sanitized FileName varchar128, SizeBytes, Data bytea; checks enforce 1..102400 bytes and matching size. Existing authors/comments/images and migration IDs are preserved. Down refuses while TXT rows exist. Bytes and comment are committed atomically in the same PostgreSQL database/backup; no filesystem volume. Preserve verified pre-upgrade copies, accounting for changes after their checkpoint.

Onion: Domain enforces immutable ownership/defensive bytes and one attachment per comment; Application defines processing/read ports and coordinates the transaction; Infrastructure validates UTF-8 and implements EF storage/queries; API binds streams and sets download headers. No new external dependency.

TextAttachments 1.1.0.11 verification: 235 backend tests (27 Domain,84 Application,56 Infrastructure/database,68 HTTP),16 frontend tests, frontend/Docker builds, restore upgrade/downgrade round trip and populated-TXT downgrade guard passed. Edge desktop/360px passed real CAPTCHA, TXT root/reply, boundary size, exact Unicode/BOM/CRLF/HTML download, restart persistence, safe headers, rejection/draft retention/removal, exclusive selection and EN/RU/UA without JavaScript errors. All 18 retained-million-corpus paging scenarios passed without reseeding; original row fingerprints, metadata, volumes and restore-verified checkpoints were retained. GUI import in VS/Workbench is not checked.

Domain ImageAttachment enforces one image/comment, bounded dimensions/bytes and defensive byte copies. Application owns ImageUpload/PreparedImage, IImageProcessor/IImageQueries and GetImage. CreateComment verifies CAPTCHA, processes the image before staging and saves the whole graph once. Infrastructure implements codec checks/orientation/resizing/PNG normalization, a two-slot decoder limit, EF bytea mapping and metadata-only comment projections. API owns multipart parsing/limits and inline image responses. No HTTP, EF or Skia dependency enters Domain/Application.

Application defines segregated ICaptchaIssuer/ICaptchaVerifier and challenge DTOs. CreateComment verifies before parent queries/staged writes. Infrastructure implements the bounded atomic hash store, TimeProvider expiry and a PNG-renderer abstraction. API maps challenge bytes to a data URL and no-store headers. Frontend useCaptcha owns refresh, cancellation, expiry and answer state; no persistence or HTTP types enter Domain/Application. Test-only DI doubles isolate legacy use-case suites; production contains no bypass.

GetRootPage validates page/sort/direction and exposes typed query contracts in Application. Infrastructure applies an allowlisted LINQ order, UUID tie-break, offset/25 limit, and repeatable-read count/items snapshot. The API only maps HTTP. Frontend RootPagination controls query state through CommentsClient; drafts and thread rendering remain separate. The compatibility latest-list use case is retained.

Application owns ICommentMarkup/XhtmlCommentMarkup, shared by create and read-only preview. HTTP only maps transport. Frontend uses the CommentsClient port and an allowlisted React renderer, never raw HTML. No provider/HTTP dependency enters the core.

Release **1.1.0.11 TextAttachments** implements comment use cases inside the Onion core.

- Domain/Entities protects entity invariants and has no external project/package dependencies.
- Application/Comments contains CreateComment, GetComment, GetReplies, GetRootPage and GetLatestComments, input validation,
  normalization, public DTOs and validation outcomes. Application references Domain only.
- Application/Abstractions/Persistence owns ICommentRepository, ICommentQueries and IUnitOfWork.
  These ports expose no EF/HTTP/provider types or IQueryable.
- Infrastructure implements tracked writes, no-tracking DTO projections and a scoped EfUnitOfWork.
  All write adapters share one DbContext. Only connectivity failures are translated into the
  core StorageUnavailableException; provider details stay outside HTTP responses.
- Infrastructure/Persistence contains DbContext, Configurations, Repositories, Queries and Migrations.
  Operational migration/status contracts and the standard IHealthCheck adapter remain technical concerns.
- Api/Endpoints maps HTTP to application inputs/results. Api/Contracts owns JSON transport rules;
  Api/Errors emits generic ProblemDetails. IP/User-Agent are read from server request context.
  Program.cs registers implementations; its Infrastructure reference is limited to composition/maintenance.

Validation occurs before staging writes. Author/comment are saved atomically by the shared
unit of work. Read queries project only public fields and bound the latest list; no network
metadata or entity navigation graph is serialized. See [API contract](API.md).

Tests are split into Domain.Tests (domain/architecture), Application.Tests (in-memory use cases),
Data.Tests (PostgreSQL with rollback), and Api.Tests (HTTP with cleanup of owned rows only).
Architecture tests guard inward references, core independence, public port types and endpoint boundaries.

Migration 20261030101720_CommentThreads extends the initial schema with immutable parent/root fields,
backfills old roots and preserves original content. Create validates the parent before staging an author;
GetReplies owns cursor/limit validation. Infrastructure implements bounded direct-child queries and projects
flat DTOs; no recursive EF navigation graphs are serialized. The UI flattens expanded branches iteratively.
Future EF add commands use --output-dir Persistence/Migrations. Root sorting/pagination is implemented in 1.1.0.8; OOP/SOLID/Onion remain mandatory. See database/MIGRATIONS.md for verified upgrade/rollback.

## Русский

TXT-вложения (1.1.0.11): корень и ответ принимают одно необязательное вложение: картинку ИЛИ `.txt`. TXT — 1..102400 байт (100 КиБ), строгий UTF-8; BOM, Unicode, CR/LF и табуляция сохраняются побайтно. Неверная кодировка и управляющие символы, кроме CR/LF/tab, отклоняются; HTML/script допускается как обычный текст. Имя/MIME не доказывают корректность содержимого. Multipart использует `textFile` с обычными полями комментария/CAPTCHA; два файла запрещены. JSON без файла сохранён. Неверный TXT: 400/errors.textFile. CAPTCHA расходуется перед проверкой содержимого; повтор требует нового кода. Локализованная форма сохраняет черновик/файл при ошибках, очищает после успеха, позволяет убрать/заменить файл.

DTO добавляет nullable `textFile: {id,fileName,sizeBytes}`; списки/страницы/ответы не содержат байты. `GET /api/text-files/{id}` скачивает точные байты с `text/plain; charset=utf-8`, UUID-именем `.txt`, attachment, nosniff, no-store и sandbox CSP; отсутствующий файл — 404. HTML не отображается. Просмотр с эффектами реализован в 1.1.0.12.

Обновление 1.1.0.10 → 1.1.0.11: остановить все записи, проверить восстановление текущей копии (включая benchmark, если существует), собрать, явно мигрировать все используемые БД и запустить прежние тома. Миграция `20261030130243_TextAttachments` добавляет TextAttachments: Id, уникальный CommentId FK CASCADE, очищенное FileName varchar128, SizeBytes, Data bytea; проверки 1..102400 байт и совпадения размера. Авторы/комментарии/картинки и прежние ID миграций сохранены. Down запрещён при наличии TXT. Файл и комментарий записываются одной транзакцией в PostgreSQL и общую копию; файлового тома нет. Сохраните проверенные копии до обновления, учитывая изменения после контрольной точки.

Onion: Domain защищает принадлежность/байты и одно вложение; Application задаёт контракты обработки/чтения и транзакцию; Infrastructure проверяет UTF-8 и реализует EF; API связывает потоки и задаёт заголовки скачивания. Новых внешних зависимостей нет.

Проверки выпуска TextAttachments 1.1.0.11: 235 backend (27 Domain,84 Application,56 Infrastructure/БД,68 HTTP),16 frontend, сборки frontend/Docker, обновление/откат восстановленной копии и запрет отката с TXT прошли. Edge десктоп/360px прошёл реальную CAPTCHA, TXT корня/ответа, граничный размер, точные Unicode/BOM/CRLF/HTML-байты при скачивании и после перезапуска, заголовки, отказы/черновик/удаление, выбор одного файла и EN/RU/UA без ошибок JavaScript. Все 18 сценариев сохранённого миллиона прошли без пересоздания; отпечатки исходных строк, метаданные, тома и проверенные восстановлением копии сохранены. GUI VS/Workbench не проверены.

Domain ImageAttachment защищает одну картинку на комментарий, пределы размеров/байтов и копирование массива. Application задаёт ImageUpload/PreparedImage, IImageProcessor/IImageQueries и GetImage. CreateComment проверяет CAPTCHA, обрабатывает картинку до подготовки записи и сохраняет граф один раз. Infrastructure реализует кодек/поворот/уменьшение/PNG, два параллельных декодера, EF bytea и проекции только метаданных. API отвечает за multipart/лимиты и inline-выдачу. Domain/Application не зависят от HTTP, EF или Skia.

Application задаёт отдельные ICaptchaIssuer/ICaptchaVerifier и DTO проверки. CreateComment проверяет код до поиска родителя/подготовки записи. Infrastructure содержит ограниченное атомарное хранилище хешей, TimeProvider и абстракцию PNG-рендера. API выдаёт data URL и no-store. useCaptcha управляет обновлением, отменой, просрочкой и вводом; Domain/Application не получают зависимостей HTTP/хранения. Только тесты внедряют заглушки для остальных сценариев; в production обхода нет.

GetRootPage проверяет page/sort/direction и задаёт типизированный запрос в Application. Infrastructure применяет разрешённую LINQ-сортировку, UUID для равных значений, offset/25 и repeatable-read снимок количества/строк. API связывает HTTP. RootPagination управляет запросом через CommentsClient; черновик и ветки отделены. Прежний сценарий последнего списка сохранён.

Application содержит ICommentMarkup/XhtmlCommentMarkup для создания и предпросмотра без записи. HTTP связывает транспорт. Frontend использует контракт CommentsClient и разрешённые React-элементы, без вставки HTML. Зависимости провайдера/HTTP не входят в ядро.

Выпуск **1.1.0.11 TextAttachments** реализует сценарии комментариев внутри ядра Onion.

- Domain/Entities защищает инварианты сущностей и не имеет внешних зависимостей от проектов/пакетов.
- Application/Comments содержит CreateComment, GetComment, GetReplies, GetRootPage и GetLatestComments, валидацию ввода,
  нормализацию, публичные DTO и результаты валидации. Application ссылается только на Domain.
- Application/Abstractions/Persistence содержит ICommentRepository, ICommentQueries и IUnitOfWork.
  Контракты не раскрывают типы EF/HTTP/провайдера и IQueryable.
- Infrastructure реализует запись с отслеживанием, DTO-проекции без отслеживания и scoped EfUnitOfWork.
  Адаптеры записи используют один DbContext. Только ошибки доступности переводятся в
  StorageUnavailableException ядра; детали провайдера не выдаются в HTTP-ответах.
- Infrastructure/Persistence содержит DbContext, Configurations, Repositories, Queries и Migrations.
  Контракты миграции/состояния и стандартный адаптер IHealthCheck остаются техническими операциями.
- Api/Endpoints связывает HTTP с входами/результатами Application. Api/Contracts задаёт правила JSON;
  Api/Errors выдаёт общие ProblemDetails. IP/User-Agent берутся из серверного контекста запроса.
  Program.cs регистрирует реализации; ссылка на Infrastructure ограничена сборкой/обслуживанием.

Валидация выполняется до подготовки записи. Автор/комментарий сохраняются атомарно общей
единицей работы. Чтение проецирует только публичные поля и ограничивает список последних записей;
сетевые метаданные и граф сущностей не сериализуются. См. [контракт API](API.md).

Тесты разделены на Domain.Tests (домен/архитектура), Application.Tests (сценарии в памяти),
Data.Tests (PostgreSQL с откатом) и Api.Tests (HTTP с удалением только своих записей).
Архитектурные тесты контролируют ссылки внутрь, независимость ядра, типы контрактов и границы endpoints.

Миграция 20261030101720_CommentThreads расширяет схему неизменяемыми полями родителя/корня,
заполняет старые корни и сохраняет прежние данные. Create проверяет родителя до подготовки автора;
GetReplies проверяет курсор/лимит. Infrastructure выполняет ограниченные запросы прямых ответов и выдаёт
плоские DTO без рекурсивного графа EF. UI итеративно разворачивает открытые ветки.
При генерации EF указывать --output-dir Persistence/Migrations. Сортировки/страницы корней реализованы в 1.1.0.8; OOP/SOLID/Onion обязательны. Обновление/откат: database/MIGRATIONS.md.

## Українська

TXT-вкладення (1.1.0.11): корінь і відповідь приймають одне необов’язкове вкладення: зображення АБО `.txt`. TXT — 1..102400 байтів (100 КіБ), строгий UTF-8; BOM, Unicode, CR/LF і табуляція зберігаються побайтово. Хибне кодування та керівні символи, крім CR/LF/tab, відхиляються; HTML/script дозволено як звичайний текст. Назва/MIME не доводять коректність вмісту. Multipart використовує `textFile` зі звичайними полями коментаря/CAPTCHA; два файли заборонено. JSON без файла збережено. Хибний TXT: 400/errors.textFile. CAPTCHA витрачається до перевірки вмісту; повтор потребує нового коду. Локалізована форма зберігає чернетку/файл після помилок, очищає після успіху, дозволяє прибрати/замінити файл.

DTO додає nullable `textFile: {id,fileName,sizeBytes}`; списки/сторінки/відповіді не містять байтів. `GET /api/text-files/{id}` завантажує точні байти з `text/plain; charset=utf-8`, UUID-назвою `.txt`, attachment, nosniff, no-store та sandbox CSP; відсутній файл — 404. HTML не відображається. Перегляд з ефектами реалізовано в 1.1.0.12.

Оновлення 1.1.0.10 → 1.1.0.11: зупинити всі записи, перевірити відновлення поточної копії (включно з benchmark, якщо існує), зібрати, явно мігрувати всі використані БД і запустити попередні томи. Міграція `20261030130243_TextAttachments` додає TextAttachments: Id, унікальний CommentId FK CASCADE, очищене FileName varchar128, SizeBytes, Data bytea; перевірки 1..102400 байтів і збігу розміру. Авторів/коментарі/зображення й попередні ID міграцій збережено. Down заборонено за наявності TXT. Файл і коментар записуються однією транзакцією в PostgreSQL і спільну копію; файлового тому немає. Збережіть перевірені копії до оновлення, враховуючи зміни після контрольної точки.

Onion: Domain захищає належність/байти й одне вкладення; Application задає контракти обробки/читання та транзакцію; Infrastructure перевіряє UTF-8 і реалізує EF; API зв’язує потоки та задає заголовки завантаження. Нових зовнішніх залежностей немає.

Перевірки випуску TextAttachments 1.1.0.11: 235 backend (27 Domain,84 Application,56 Infrastructure/БД,68 HTTP),16 frontend, збирання frontend/Docker, оновлення/відкат відновленої копії та заборона відкату з TXT пройшли. Edge десктоп/360px пройшов реальну CAPTCHA, TXT кореня/відповіді, граничний розмір, точні Unicode/BOM/CRLF/HTML-байти під час завантаження й після перезапуску, заголовки, відмови/чернетку/видалення, вибір одного файла та EN/RU/UA без помилок JavaScript. Усі 18 сценаріїв збереженого мільйона пройшли без повторного створення; відбитки початкових рядків, метадані, томи та перевірені відновленням копії збережено. GUI VS/Workbench не перевірено.

Domain ImageAttachment захищає одне зображення на коментар, межі розмірів/байтів і копіювання масиву. Application задає ImageUpload/PreparedImage, IImageProcessor/IImageQueries та GetImage. CreateComment перевіряє CAPTCHA, обробляє зображення до підготовки запису та зберігає граф один раз. Infrastructure реалізує кодек/поворот/зменшення/PNG, два паралельні декодери, EF bytea та проєкції лише метаданих. API відповідає за multipart/ліміти й inline-видачу. Domain/Application не залежать від HTTP, EF чи Skia.

Application задає окремі ICaptchaIssuer/ICaptchaVerifier і DTO перевірки. CreateComment перевіряє код до пошуку батьківського запису/підготовки запису. Infrastructure містить обмежене атомарне сховище хешів, TimeProvider та абстракцію PNG-рендеру. API видає data URL і no-store. useCaptcha керує оновленням, скасуванням, простроченням та введенням; Domain/Application не отримують залежностей HTTP/сховища. Лише тести впроваджують заглушки для решти сценаріїв; у production обходу немає.

GetRootPage перевіряє page/sort/direction і задає типізований запит в Application. Infrastructure застосовує дозволене LINQ-сортування, UUID для однакових значень, offset/25 і repeatable-read знімок кількості/рядків. API пов’язує HTTP. RootPagination керує запитом через CommentsClient; чернетку й гілки відокремлено. Попередній сценарій останнього списку збережено.

Application містить ICommentMarkup/XhtmlCommentMarkup для створення та перегляду без запису. HTTP пов’язує транспорт. Frontend використовує контракт CommentsClient і дозволені React-елементи, без вставлення HTML. Залежності провайдера/HTTP не входять до ядра.

Випуск **1.1.0.11 TextAttachments** реалізує сценарії коментарів усередині ядра Onion.

- Domain/Entities захищає інваріанти сутностей і не має зовнішніх залежностей від проєктів/пакетів.
- Application/Comments містить CreateComment, GetComment, GetReplies, GetRootPage і GetLatestComments, валідацію вводу,
  нормалізацію, публічні DTO та результати валідації. Application посилається лише на Domain.
- Application/Abstractions/Persistence містить ICommentRepository, ICommentQueries та IUnitOfWork.
  Контракти не розкривають типи EF/HTTP/провайдера та IQueryable.
- Infrastructure реалізує запис із відстеженням, DTO-проєкції без відстеження та scoped EfUnitOfWork.
  Адаптери запису використовують один DbContext. Лише помилки доступності перетворюються на
  StorageUnavailableException ядра; деталі провайдера не надаються в HTTP-відповідях.
- Infrastructure/Persistence містить DbContext, Configurations, Repositories, Queries та Migrations.
  Контракти міграції/стану і стандартний адаптер IHealthCheck залишаються технічними операціями.
- Api/Endpoints пов’язує HTTP з входами/результатами Application. Api/Contracts задає правила JSON;
  Api/Errors надає загальні ProblemDetails. IP/User-Agent беруться із серверного контексту запиту.
  Program.cs реєструє реалізації; посилання на Infrastructure обмежене складанням/обслуговуванням.

Валідація виконується до підготовки запису. Автор/коментар зберігаються атомарно спільною
одиницею роботи. Читання проєктує лише публічні поля та обмежує список останніх записів;
мережеві метадані й граф сутностей не серіалізуються. Див. [контракт API](API.md).

Тести розділено на Domain.Tests (домен/архітектура), Application.Tests (сценарії в пам’яті),
Data.Tests (PostgreSQL із відкатом) та Api.Tests (HTTP з видаленням лише власних записів).
Архітектурні тести контролюють посилання всередину, незалежність ядра, типи контрактів і межі endpoints.

Міграція 20261030101720_CommentThreads розширює схему незмінними полями батьківського запису/кореня,
заповнює старі корені та зберігає попередні дані. Create перевіряє батьківський запис до підготовки автора;
GetReplies перевіряє курсор/ліміт. Infrastructure виконує обмежені запити прямих відповідей та видає
плоскі DTO без рекурсивного графа EF. UI ітеративно розгортає відкриті гілки.
Під час генерації EF вказувати --output-dir Persistence/Migrations. Сортування/сторінки коренів реалізовано в 1.1.0.8; OOP/SOLID/Onion обов’язкові. Оновлення/відкат: database/MIGRATIONS.md.

## Dependencies / Зависимости / Залежності

```mermaid
flowchart BT
    Application --> Domain
    Infrastructure --> Application
    Infrastructure --> Domain
    Api --> Application
    Api -. "composition root / DI" .-> Infrastructure
```

## Frontend boundaries / Границы frontend / Межі frontend

EN: React presentation uses the CommentsClient port. HttpCommentsClient implements HTTP;
the composition root injects it. Pure input validation and safe URL handling are separate
from rendering. Form state, request errors and the recent list have separate responsibilities.
Backend Domain/Application remain independent of React/HTTP/EF. Server validation is authoritative.

RU: React использует контракт CommentsClient. HttpCommentsClient реализует HTTP;
точка сборки внедряет его. Чистая валидация и безопасные URL отделены от отображения.
Состояние формы, ошибки запросов и список имеют отдельные обязанности.
Domain/Application не зависят от React/HTTP/EF. Серверная валидация окончательная.

UA: React використовує контракт CommentsClient. HttpCommentsClient реалізує HTTP;
точка складання впроваджує його. Чисту валідацію та безпечні URL відокремлено від відображення.
Стан форми, помилки запитів і список мають окремі обов’язки.
Domain/Application не залежать від React/HTTP/EF. Серверна валідація остаточна.


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


## ResponsiveUi 1.1.0.13 — EN / RU / UA

EN: Presentation changes only. CommentForm maps typed validation fields through a localized fieldError function rather than displaying provider text. Domain/Application/Infrastructure and backend use cases are unchanged. CommentsTable preserves native table semantics with explicit roles for the stacked mobile layout; direction isolation is local to user content. main still constructs and injects the HTTP adapters. No migration or new dependency.

RU: Изменено только представление. CommentForm переводит типизированные ошибки полей через fieldError, не выводя текст провайдера. Domain/Application/Infrastructure и backend-сценарии прежние. CommentsTable сохраняет семантику таблицы с явными ролями в мобильной вертикальной вёрстке; направление изолируется только для пользовательского содержимого. main по-прежнему создаёт и внедряет HTTP-адаптеры. Миграций и новых зависимостей нет.

UA: Змінено лише представлення. CommentForm перекладає типізовані помилки полів через fieldError, не виводячи текст провайдера. Domain/Application/Infrastructure та backend-сценарії попередні. CommentsTable зберігає семантику таблиці з явними ролями в мобільній вертикальній верстці; напрямок ізольовано лише для користувацького вмісту. main і далі створює та впроваджує HTTP-адаптери. Міграцій і нових залежностей немає.


## SecurityReview 1.1.0.14 — EN / RU / UA

EN: JsonRequest owns bounded UTF-8/canonical JSON binding for create/preview; SecurityHeaders owns response policy before exception handling/static files/endpoints. These HTTP concerns remain in API, with unchanged Application ports, domain invariants and Infrastructure adapters. Queries still use typed allowlisted sorting and parameterized EF expressions; file processing remains behind Application interfaces. No new provider/framework dependencies enter Domain/Application.

RU: JsonRequest отвечает за ограниченный UTF-8/camelCase JSON создания/просмотра, SecurityHeaders — за политику ответа перед обработкой ошибок/статикой/маршрутами. HTTP-правила остаются в API; порты Application, доменные инварианты и адаптеры Infrastructure прежние. Запросы используют типизированные сортировки и параметризованные EF-выражения; файлы обрабатываются через интерфейсы Application. Новые зависимости провайдеров/фреймворков в Domain/Application не добавлены.

UA: JsonRequest відповідає за обмежений UTF-8/camelCase JSON створення/перегляду, SecurityHeaders — за політику відповіді перед обробкою помилок/статикою/маршрутами. HTTP-правила залишаються в API; порти Application, доменні інваріанти й адаптери Infrastructure попередні. Запити використовують типізовані сортування та параметризовані EF-вирази; файли обробляються через інтерфейси Application. Нові залежності провайдерів/фреймворків у Domain/Application не додано.


## Acceptance 1.1.0.15 — EN / RU / UA

EN: Runtime boundaries and dependencies are unchanged. The two verification CLI tools are operational adapters outside Domain/Application: metadata comparison is read-only; checkpoint verification uses an isolated, owned temporary DB and never switches runtime connections. All architecture/domain tests passed again; EF reports no model drift or new migration.

RU: Границы и зависимости приложения прежние. Два CLI-инструмента — операционные адаптеры вне Domain/Application: сравнение метаданных не пишет данные; проверка копии использует собственную временную БД и не переключает рабочие подключения. Доменные/архитектурные тесты прошли; EF не обнаружил изменений модели/новой миграции.

UA: Межі й залежності застосунку попередні. Два CLI-інструменти — операційні адаптери поза Domain/Application: порівняння метаданих не пише дані; перевірка копії використовує власну тимчасову БД і не перемикає робочі підключення. Доменні/архітектурні тести пройшли; EF не знайшов змін моделі/нової міграції.


## CloudAttachments 1.3.3.4 — EN / RU / UA

EN: Optional Cloud ports/adapters, consent lists, bounded post-commit worker and canonical SQL fallback: [Cloud documentation](CLOUD_ATTACHMENTS.md). No schema migration or new provider dependency in inner layers. Public configuration has no Cloud secrets or consent IDs.

RU: Необязательные порты/адаптеры Cloud, списки разрешений, ограниченный worker после commit и резервное SQL-чтение: [документация Cloud](CLOUD_ATTACHMENTS.md). Без миграции схемы/зависимости внутренних слоёв от провайдера. Публичный конфиг не содержит секретов Cloud/ID разрешений.

UA: Необов’язкові порти/адаптери Cloud, списки дозволів, обмежений worker після commit та резервне SQL-читання: [документація Cloud](CLOUD_ATTACHMENTS.md). Без міграції схеми/залежності внутрішніх шарів від провайдера. Публічний конфіг не містить секретів Cloud/ID дозволів.
