# Comments API — 1.1.0.9

## English

Every comment/reply requires a fresh verification challenge. `GET /api/captcha` returns `{id,imageDataUrl,expiresAt}` with a raster PNG and no-store headers; the answer is never returned as text or metadata. Enter the six Latin letters/digits from the image, case-insensitively. Codes expire after **5 minutes**. `GET /api/captcha?replace={oldId}` invalidates that challenge and issues a new one.

POST /api/comments additionally requires `captchaId` (UUID) and `captchaCode`. A verification attempt consumes its ID, including a wrong answer; missing, expired, replaced or spent challenges return 400 with `errors.captchaCode` and never stage an author/comment. Other input is validated before verification; after successful verification, parent/storage errors still consume the challenge. Refresh after any uncertain or failed submission before retrying. Preview and read endpoints do not require CAPTCHA.

The form loads/refreshes the picture, blocks submission while loading/expired/unavailable, and preserves the draft after errors. It obtains a new challenge after every POST attempt. EN/RU/UA labels and errors are provided. The store is process-local, holds at most 2,048 challenges, reclaims expired entries, and returns 429 at capacity. Restarting/recreating the app invalidates outstanding challenges. Multiple app instances have separate challenges; use one instance for this release. The renderer uses portable .NET PNG primitives without platform graphics dependencies. Only answer hashes and expiry are retained in memory; no CAPTCHA data or migration is added to PostgreSQL.


The SPA loads `GET /api/comments/page?page=1&sort=createdAt&direction=desc`. Allowed sort fields: `userName`, `email`, `createdAt`; direction: `asc` or `desc`. Date descending is default LIFO. Each page contains **25 roots**, excluding replies. The response contains `items`, `page`, `pageSize`, `totalCount` (roots only), `totalPages`. Page must be an integer from 1 to 85,899,345; invalid fields/directions return 400. Beyond the last page, the API returns an empty items array with the real totals; the SPA corrects stale bookmarks to the last page, or page 1 for an empty list. An empty list has totalPages=0.

Sort controls offer all six combinations; sorting returns to page 1. First/Previous/Next/Last and page jump navigate; the address preserves page/sort/direction across reload. Drafts remain while browsing. A new root returns to page 1 with newest-first order; a reply retains its current page and does not change the root count. Replies keep their own Show/Hide/More controls. Failed refresh retains previous results and offers Refresh.

Equal sort values use UUID in the selected direction as a stable tie-breaker. Name/email use the database collation; dates use UTC. Count and rows share a repeatable-read snapshot within a request. Separate page requests are live: concurrent additions can shift offset boundaries. The compatibility route `GET /api/comments?limit=25` retains its bounded latest-root array response. No schema migration is added in 1.1.0.8.

[Persistent million-row corpus and measurements](BENCHMARK.md).


Replies can target any existing comment. Choose Reply beneath a comment; the form identifies the recipient. Cancel reply returns to root posting and preserves the draft. Successful replies appear under their parent; missing targets produce an error without creating an author or comment.

The root table shows 25 roots per page in the selected order. Show/Hide replies expands or collapses direct replies; More replies loads the next 25. Replies use descending UTC time and UUID; roots use the selected order. Expanding descendants has no application depth limit: rendering is iterative, and visual indentation stops growing after six levels while the numeric level continues. Refresh reloads roots and open branches; branch failures offer Retry and preserve loaded content. Full root pagination/sorting is now implemented.

API: POST /api/comments accepts optional parentId (UUID/null); rootId is derived by the server and cannot be supplied. GET /api/comments/{id}/replies?limit=25&after={replyId} returns {items,nextAfter}; omit after for the first page. Limit is 1–100; after must be a direct child of that parent. Missing parent is 404 for reads and a parentId validation error (400) for writes. Unknown/foreign cursors are 400. Pages limit direct siblings, not thread depth. No delete/edit API is introduced.


The API creates and reads text/XHTML comments. API handlers call Application use cases;
validation, normalization and public DTOs are independent of ASP.NET/EF.

| Method | Route | Result |
|---|---|---|
| GET | `/api/comments/page?page=1&sort=createdAt&direction=desc` | 200 `{items,page,pageSize,totalCount,totalPages}`; 400 |
| GET | `/api/captcha?replace={oldId}` | 200 `{id,imageDataUrl,expiresAt}`; 429 capacity |
| POST | `/api/comments` | 201, public comment and relative Location header |
| POST | `/api/comments/preview` | 200 `{html}`, 400 validation errors; `{text}` only, no persistence |
| GET | `/api/comments/{id}/replies?limit=25&after={replyId}` | 200 `{items,nextAfter}`; 404 / 400 |
| GET | `/api/comments/{id}` | 200, public comment; 404 for a missing UUID |
| GET | `/api/comments?limit=25` | 200, array of latest root comments; default 25, range 1–100 |

The list orders by CreatedAt descending, then UUID descending. It is a bounded recent
window retained for compatibility; the page endpoint implements full root paging/sorting.

POST accepts JSON only: userName, email, optional homePage, text and optional parentId, captchaId and captchaCode. Unknown fields,
including clientIp, userAgent, createdAt and rootId, are rejected. Names/email/homepage
are trimmed; an empty homepage becomes null. Text whitespace, Unicode, RTL and emoji
are preserved. Required name: Latin letters/digits, 1–100 characters. Email: plain address,
up to 254 characters, without display name/control characters. Homepage: absolute HTTP(S),
up to 2048 characters, without embedded credentials. Text: non-whitespace, at most 65536
UTF-16 units. XHTML validation and escaping rules are in [XHTML editor](../README.md#xhtml-editor). Request bodies are limited to 1 MiB, including JSON encoding.

The server generates UUID/time, captures the socket peer IP and User-Agent (up to 2048
characters), and saves author/comment together. If peer IP is unavailable, 0.0.0.0 records
that fact. Untrusted X-Forwarded-For is ignored; deployment behind a trusted proxy needs
explicit proxy configuration. IP/User-Agent and author persistence IDs are never public
response fields. Public fields are id, userName, email, homePage, text, createdAt, parentId, rootId.
Timestamps use UTC ISO 8601 with microsecond precision, stable across create/read.
Email is submitted metadata, not proof of account ownership.

Errors use ProblemDetails JSON: 400 for invalid input/JSON/limit, 404 for missing comments,
413 for oversized bodies, 415 for unsupported content type, 503 for unavailable storage.
Validation responses include an errors dictionary keyed by JSON field name. Internal
exception/connection details are not included. Caller cancellation reaches data operations.

The SPA form/table uses these endpoints in 1.1.0.9;
Attachments remain later milestones. Thread migration: 20261030101720_CommentThreads;
initial migration 20261029195935_InitialDataModel is retained.

## Русский

Каждый комментарий/ответ требует новой проверки. `GET /api/captcha` возвращает `{id,imageDataUrl,expiresAt}` с растровой PNG-картинкой и заголовками no-store; ответ текстом или метаданными не выдаётся. Введите шесть латинских букв/цифр с картинки, регистр не важен. Код действует **5 минут**. `GET /api/captcha?replace={oldId}` отменяет прежнюю проверку и выдаёт новую.

POST /api/comments дополнительно требует `captchaId` (UUID) и `captchaCode`. Вызов проверки расходует ID, в том числе при неверном ответе; отсутствие, просрочка, замена или повтор дают 400 с `errors.captchaCode`, без подготовки автора/комментария. Остальные поля проверяются до CAPTCHA; после успешной проверки ошибка родителя/хранилища тоже расходует код. После неудачной или неопределённой отправки обновите картинку перед повтором. Предпросмотр и чтение CAPTCHA не требуют.

Форма загружает/обновляет картинку, блокирует отправку при загрузке/просрочке/недоступности и сохраняет черновик при ошибке. После любой попытки POST получает новую проверку. Подписи/ошибки — EN/RU/UA. Хранилище в памяти процесса ограничено 2 048 проверками, освобождает просроченные записи и возвращает 429 при заполнении. Перезапуск/пересоздание приложения отменяет оставшиеся коды. Разные экземпляры имеют раздельные коды; для этой версии используйте один экземпляр. PNG создаётся переносимыми средствами .NET без платформенной графики. В памяти остаются только хеш ответа и срок; данные CAPTCHA и миграция в PostgreSQL не добавляются.


SPA загружает `GET /api/comments/page?page=1&sort=createdAt&direction=desc`. Поля сортировки: `userName`, `email`, `createdAt`; порядок: `asc` или `desc`. По умолчанию дата убывает — LIFO. На странице **25 корней**, без учёта ответов. Ответ API: `items`, `page`, `pageSize`, `totalCount` (только корни), `totalPages`. Page — целое 1–85 899 345; неверные поля/порядок дают 400. После последней страницы API возвращает пустой items с реальными итогами; SPA исправляет устаревший адрес на последнюю страницу либо первую для пустого списка. Для пустого списка totalPages=0.

Доступны все шесть сочетаний сортировки; изменение возвращает на первую страницу. Первая/Предыдущая/Следующая/Последняя и переход по номеру управляют страницами; адрес сохраняет page/sort/direction при перезагрузке. Черновик не пропадает. Новый корень возвращает на первую страницу новых записей; ответ сохраняет текущую страницу и не меняет число корней. У ответов прежние кнопки раскрытия/скрытия/дозагрузки. Ошибка обновления сохраняет прежние результаты; повтор — «Обновить».

Равные значения упорядочены по UUID в том же направлении. Имя/email используют сопоставление БД, дата — UTC. Количество и строки читаются в одном repeatable-read снимке запроса. Разные запросы страниц отражают текущие данные: параллельное добавление может сдвигать offset-границы. Совместимый `GET /api/comments?limit=25` сохраняет массив последних корней. В 1.1.0.8 новой миграции схемы нет.

[Постоянный миллионный набор и измерения](BENCHMARK.md).


Ответить можно на любой существующий комментарий. Нажмите «Ответить» под записью; форма покажет адресата. «Отменить ответ» возвращает создание корня и сохраняет черновик. Успешный ответ появляется под родителем; отсутствующий адресат вызывает ошибку без создания автора или комментария.

Таблица показывает 25 корней на странице в выбранном порядке. «Показать/Скрыть ответы» раскрывает прямые ответы; «Ещё ответы» загружает следующие 25. Ответы идут по времени UTC/UUID по убыванию, корни — в выбранном порядке. Глубина обсуждения не ограничена приложением: вывод итеративный, визуальный отступ перестаёт расти после шести уровней, числовой уровень продолжает расти. «Обновить» перечитывает корни и открытые ветки; ошибка ветки предлагает «Повторить» и сохраняет загруженное содержимое. Полные страницы/сортировки корней теперь реализованы.

API: POST /api/comments принимает необязательный parentId (UUID/null); rootId определяет сервер, передавать его нельзя. GET /api/comments/{id}/replies?limit=25&after={replyId} возвращает {items,nextAfter}; для первой порции after не указывается. Limit — 1–100; after должен быть прямым ответом этому родителю. Нет родителя: 404 при чтении, ошибка parentId (400) при записи. Неизвестный/чужой курсор — 400. Порции ограничивают соседние ответы, а не глубину. API удаления/редактирования не добавлен.


API создаёт и читает текстовые комментарии. HTTP-обработчики вызывают сценарии Application;
валидация, нормализация и публичные DTO не зависят от ASP.NET/EF.

| Метод | Маршрут | Результат |
|---|---|---|
| GET | `/api/comments/page?page=1&sort=createdAt&direction=desc` | 200 `{items,page,pageSize,totalCount,totalPages}`; 400 |
| GET | `/api/captcha?replace={oldId}` | 200 `{id,imageDataUrl,expiresAt}`; 429 capacity |
| POST | `/api/comments` | 201, публичный комментарий и относительный заголовок Location |
| POST | `/api/comments/preview` | 200 `{html}`, 400 при ошибке; только `{text}`, без записи |
| GET | `/api/comments/{id}/replies?limit=25&after={replyId}` | 200 `{items,nextAfter}`; 404 / 400 |
| GET | `/api/comments/{id}` | 200, публичный комментарий; 404 для отсутствующего UUID |
| GET | `/api/comments?limit=25` | 200, массив последних корневых комментариев; по умолчанию 25, диапазон 1–100 |

Список отсортирован по CreatedAt, затем UUID по убыванию. Это ограниченное окно последних
записей; переходы по страницам и все направления сортировки относятся к 1.1.0.8.

POST принимает только JSON: userName, email, необязательный homePage, text и необязательный parentId, captchaId и captchaCode. Неизвестные
поля, включая clientIp, userAgent, createdAt и rootId, отклоняются. Пробелы по краям
имени/email/homepage удаляются; пустой homepage превращается в null. Пробелы текста,
Unicode, RTL и emoji сохраняются. Имя обязательно: 1–100 латинских букв/цифр. Email:
адрес до 254 символов без отображаемого имени и управляющих символов. Homepage: абсолютный
HTTP(S) URL до 2048 символов без учётных данных. Текст: не только пробелы, до 65536 единиц
UTF-16; нулевые символы, некорректный Unicode и недопустимые XML-символы отклоняются.
Правила XHTML, ссылок, вложенности и экранирования: [XHTML-редактор](../README.RU.md#xhtml-редактор). Размер JSON-запроса — до 1 МиБ.

Сервер создаёт UUID/время, получает IP сетевого соединения и User-Agent (до 2048 символов),
сохраняет автора/комментарий вместе. Если IP недоступен, записывается 0.0.0.0.
Непроверенный X-Forwarded-For игнорируется; для доверенного reverse proxy нужна явная
настройка. IP/User-Agent и внутренний ID автора не выдаются в публичных ответах.
Публичные поля: id, userName, email, homePage, text, createdAt, parentId, rootId. Время — UTC ISO 8601
с точностью до микросекунд, одинаковое при создании/чтении. Email — введённые данные,
а не подтверждение владения учётной записью.

Ошибки — ProblemDetails JSON: 400 при неверном вводе/JSON/limit, 404 при отсутствии записи,
413 при слишком большом теле, 415 при неподдерживаемом формате, 503 при недоступности БД.
Ответ валидации содержит errors с именами JSON-полей. Внутренние исключения и параметры
подключения не выдаются. Отмена запроса передаётся операциям данных.

SPA-форма/таблица использует эти маршруты в 1.1.0.9; Вложения остаются последующими этапами. Миграция ответов: 20261030101720_CommentThreads;
первая миграция 20261029195935_InitialDataModel сохранена.

## Українська

Кожен коментар/відповідь потребує нової перевірки. `GET /api/captcha` повертає `{id,imageDataUrl,expiresAt}` із растровим PNG-зображенням та заголовками no-store; відповідь текстом чи метаданими не надається. Введіть шість латинських літер/цифр із зображення, регістр не важливий. Код діє **5 хвилин**. `GET /api/captcha?replace={oldId}` скасовує попередню перевірку та видає нову.

POST /api/comments додатково потребує `captchaId` (UUID) і `captchaCode`. Виклик перевірки витрачає ID, зокрема за хибної відповіді; відсутність, прострочення, заміна чи повтор дають 400 з `errors.captchaCode`, без підготовки автора/коментаря. Решту полів перевірено до CAPTCHA; після успішної перевірки помилка батьківського запису/сховища також витрачає код. Після невдалої чи невизначеної спроби оновіть зображення перед повтором. Перегляд і читання CAPTCHA не потребують.

Форма завантажує/оновлює зображення, блокує надсилання під час завантаження/прострочення/недоступності та зберігає чернетку за помилки. Після кожної спроби POST отримує нову перевірку. Підписи/помилки — EN/RU/UA. Сховище в пам’яті процесу обмежене 2 048 перевірками, звільняє прострочені записи та повертає 429 за заповнення. Перезапуск/перестворення застосунку скасовує решту кодів. Різні екземпляри мають окремі коди; для цієї версії використовуйте один екземпляр. PNG створюється переносимими засобами .NET без платформної графіки. У пам’яті лишаються лише хеш відповіді й термін; дані CAPTCHA та міграція в PostgreSQL не додаються.


SPA завантажує `GET /api/comments/page?page=1&sort=createdAt&direction=desc`. Поля сортування: `userName`, `email`, `createdAt`; порядок: `asc` або `desc`. Типово дата спадає — LIFO. На сторінці **25 коренів**, без урахування відповідей. Відповідь API: `items`, `page`, `pageSize`, `totalCount` (лише корені), `totalPages`. Page — ціле 1–85 899 345; хибні поля/порядок дають 400. Після останньої сторінки API повертає порожній items із реальними підсумками; SPA виправляє застарілу адресу на останню сторінку або першу для порожнього списку. Для порожнього списку totalPages=0.

Доступні всі шість поєднань сортування; зміна повертає на першу сторінку. Перша/Попередня/Наступна/Остання та перехід за номером керують сторінками; адреса зберігає page/sort/direction під час перезавантаження. Чернетка не зникає. Новий корінь повертає на першу сторінку нових записів; відповідь зберігає поточну сторінку й не змінює кількість коренів. У відповідей попередні кнопки розгортання/приховування/дозавантаження. Помилка оновлення зберігає попередні результати; повтор — «Оновити».

Однакові значення впорядковано за UUID у тому самому напрямку. Ім’я/email використовують зіставлення БД, дата — UTC. Кількість і рядки читаються в одному repeatable-read знімку запиту. Різні запити сторінок відображають поточні дані: паралельне додавання може зміщувати offset-межі. Сумісний `GET /api/comments?limit=25` зберігає масив останніх коренів. У 1.1.0.8 нової міграції схеми немає.

[Постійний мільйонний набір та вимірювання](BENCHMARK.md).


Відповісти можна на будь-який наявний коментар. Натисніть «Відповісти» під записом; форма покаже адресата. «Скасувати відповідь» повертає створення кореня й зберігає чернетку. Успішна відповідь з’являється під батьківським записом; відсутній адресат спричиняє помилку без створення автора чи коментаря.

Таблиця показує 25 коренів на сторінці у вибраному порядку. «Показати/Приховати відповіді» розгортає прямі відповіді; «Ще відповіді» завантажує наступні 25. Відповіді йдуть за часом UTC/UUID за спаданням, корені — у вибраному порядку. Глибина обговорення не обмежена застосунком: виведення ітеративне, візуальний відступ перестає зростати після шести рівнів, числовий рівень продовжує зростати. «Оновити» перечитує корені й відкриті гілки; помилка гілки пропонує «Повторити» та зберігає завантажений вміст. Повні сторінки/сортування коренів тепер реалізовано.

API: POST /api/comments приймає необов’язковий parentId (UUID/null); rootId визначає сервер, передавати його не можна. GET /api/comments/{id}/replies?limit=25&after={replyId} повертає {items,nextAfter}; для першої порції after не вказується. Limit — 1–100; after має бути прямою відповіддю цьому батьківському запису. Немає батьківського запису: 404 під час читання, помилка parentId (400) під час запису. Невідомий/чужий курсор — 400. Порції обмежують сусідні відповіді, а не глибину. API видалення/редагування не додано.


API створює та читає текстові коментарі. HTTP-обробники викликають сценарії Application;
валідація, нормалізація та публічні DTO не залежать від ASP.NET/EF.

| Метод | Маршрут | Результат |
|---|---|---|
| GET | `/api/comments/page?page=1&sort=createdAt&direction=desc` | 200 `{items,page,pageSize,totalCount,totalPages}`; 400 |
| GET | `/api/captcha?replace={oldId}` | 200 `{id,imageDataUrl,expiresAt}`; 429 capacity |
| POST | `/api/comments` | 201, публічний коментар і відносний заголовок Location |
| POST | `/api/comments/preview` | 200 `{html}`, 400 за помилки; лише `{text}`, без запису |
| GET | `/api/comments/{id}/replies?limit=25&after={replyId}` | 200 `{items,nextAfter}`; 404 / 400 |
| GET | `/api/comments/{id}` | 200, публічний коментар; 404 для відсутнього UUID |
| GET | `/api/comments?limit=25` | 200, масив останніх кореневих коментарів; типово 25, діапазон 1–100 |

Список відсортований за CreatedAt, потім UUID за спаданням. Це обмежене вікно останніх
записів; переходи між сторінками та всі напрями сортування належать до 1.1.0.8.

POST приймає лише JSON: userName, email, необов’язковий homePage та text. Невідомі
поля, включно з clientIp, userAgent, createdAt і rootId, відхиляються. Пробіли навколо
імені/email/homepage видаляються; порожній homepage перетворюється на null. Пробіли тексту,
Unicode, RTL та emoji зберігаються. Ім’я обов’язкове: 1–100 латинських літер/цифр. Email:
адреса до 254 символів без відображуваного імені та керувальних символів. Homepage: абсолютний
HTTP(S) URL до 2048 символів без облікових даних. Текст: не лише пробіли, до 65536 одиниць
UTF-16; нульові символи, некоректний Unicode та недопустимі XML-символи відхиляються.
Правила XHTML, посилань, вкладеності й екранування: [XHTML-редактор](../README.UA.md#xhtml-редактор). Розмір JSON-запиту — до 1 МіБ.

Сервер створює UUID/час, отримує IP мережевого з’єднання та User-Agent (до 2048 символів),
зберігає автора/коментар разом. Якщо IP недоступний, записується 0.0.0.0.
Неперевірений X-Forwarded-For ігнорується; для довіреного reverse proxy потрібне явне
налаштування. IP/User-Agent і внутрішній ID автора не надаються в публічних відповідях.
Публічні поля: id, userName, email, homePage, text, createdAt, parentId, rootId. Час — UTC ISO 8601
з точністю до мікросекунд, однаковий під час створення/читання. Email — введені дані,
а не підтвердження володіння обліковим записом.

Помилки — ProblemDetails JSON: 400 за неправильного вводу/JSON/limit, 404 за відсутності запису,
413 за надто великого тіла, 415 за непідтримуваного формату, 503 за недоступності БД.
Відповідь валідації містить errors з іменами JSON-полів. Внутрішні винятки й параметри
підключення не надаються. Скасування запиту передається операціям даних.

SPA-форма/таблиця використовує ці маршрути в 1.1.0.9; Вкладення залишаються наступними етапами. Міграція відповідей: 20261030101720_CommentThreads;
першу міграцію 20261029195935_InitialDataModel збережено.

## Example / Пример / Приклад

```bash
curl --fail http://127.0.0.1:8080/api/captcha
# Read the picture; replace CAPTCHA_ID and CAPTCHA_CODE below / прочитайте картинку / прочитайте зображення.
curl --fail-with-body -H 'Content-Type: application/json' \
  -d '{"userName":"Reader123","email":"reader@example.test","homePage":null,"text":"Hello","captchaId":"CAPTCHA_ID","captchaCode":"CAPTCHA_CODE"}' \
  http://127.0.0.1:8080/api/comments
curl --fail 'http://127.0.0.1:8080/api/comments?limit=25'
```

```json
{
  "id": "d98642a1-5f3b-4657-94d5-9d7bb8fca503",
  "userName": "Reader123",
  "email": "reader@example.test",
  "homePage": null,
  "text": "Hello",
  "createdAt": "2026-10-29T12:00:00.123456+00:00",
  "parentId": null,
  "rootId": "d98642a1-5f3b-4657-94d5-9d7bb8fca503"
}
```
