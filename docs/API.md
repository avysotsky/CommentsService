# Comments API — 1.1.0.5

## English

The API creates and reads plain-text comments. API handlers call Application use cases;
validation, normalization and public DTOs are independent of ASP.NET/EF.

| Method | Route | Result |
|---|---|---|
| POST | `/api/comments` | 201, public comment and relative Location header |
| GET | `/api/comments/{id}` | 200, public comment; 404 for a missing UUID |
| GET | `/api/comments?limit=25` | 200, array of latest comments; default 25, range 1–100 |

The list orders by CreatedAt descending, then UUID descending. It is a bounded recent
window; page navigation and all sort directions are scheduled for 1.1.0.8.

POST accepts JSON only: userName, email, optional homePage and text. Unknown fields,
including clientIp, userAgent, createdAt and parentId, are rejected. Names/email/homepage
are trimmed; an empty homepage becomes null. Text whitespace, Unicode, RTL and emoji
are preserved. Required name: Latin letters/digits, 1–100 characters. Email: plain address,
up to 254 characters, without display name/control characters. Homepage: absolute HTTP(S),
up to 2048 characters, without embedded credentials. Text: non-whitespace, at most 65536
UTF-16 units; null characters, invalid Unicode and tag-like HTML markup are rejected.
Even the eventual XHTML allowlist is not enabled until 1.1.0.6. Numeric comparisons such
as `1 < 2` remain plain text. Request bodies are limited to 1 MiB, including JSON encoding.

The server generates UUID/time, captures the socket peer IP and User-Agent (up to 2048
characters), and saves author/comment together. If peer IP is unavailable, 0.0.0.0 records
that fact. Untrusted X-Forwarded-For is ignored; deployment behind a trusted proxy needs
explicit proxy configuration. IP/User-Agent and author persistence IDs are never public
response fields. Public fields are id, userName, email, homePage, text, createdAt.
Timestamps use UTC ISO 8601 with microsecond precision, stable across create/read.
Email is submitted metadata, not proof of account ownership.

Errors use ProblemDetails JSON: 400 for invalid input/JSON/limit, 404 for missing comments,
413 for oversized bodies, 415 for unsupported content type, 503 for unavailable storage.
Validation responses include an errors dictionary keyed by JSON field name. Internal
exception/connection details are not included. Caller cancellation reaches data operations.

The SPA form/table uses these endpoints in 1.1.0.5;
replies, CAPTCHA, XHTML and attachments remain later milestones. No schema change is
needed from 1.1.0.3; migration 20261029195935_InitialDataModel is unchanged.

## Русский

API создаёт и читает текстовые комментарии. HTTP-обработчики вызывают сценарии Application;
валидация, нормализация и публичные DTO не зависят от ASP.NET/EF.

| Метод | Маршрут | Результат |
|---|---|---|
| POST | `/api/comments` | 201, публичный комментарий и относительный заголовок Location |
| GET | `/api/comments/{id}` | 200, публичный комментарий; 404 для отсутствующего UUID |
| GET | `/api/comments?limit=25` | 200, массив последних комментариев; по умолчанию 25, диапазон 1–100 |

Список отсортирован по CreatedAt, затем UUID по убыванию. Это ограниченное окно последних
записей; переходы по страницам и все направления сортировки относятся к 1.1.0.8.

POST принимает только JSON: userName, email, необязательный homePage и text. Неизвестные
поля, включая clientIp, userAgent, createdAt и parentId, отклоняются. Пробелы по краям
имени/email/homepage удаляются; пустой homepage превращается в null. Пробелы текста,
Unicode, RTL и emoji сохраняются. Имя обязательно: 1–100 латинских букв/цифр. Email:
адрес до 254 символов без отображаемого имени и управляющих символов. Homepage: абсолютный
HTTP(S) URL до 2048 символов без учётных данных. Текст: не только пробелы, до 65536 единиц
UTF-16; нулевые символы, некорректный Unicode и похожая на HTML разметка отклоняются.
Даже будущие разрешённые XHTML-теги включаются только в 1.1.0.6. Числовые сравнения
`1 < 2` остаются обычным текстом. Предел тела запроса — 1 МиБ с учётом JSON-кодирования.

Сервер создаёт UUID/время, получает IP сетевого соединения и User-Agent (до 2048 символов),
сохраняет автора/комментарий вместе. Если IP недоступен, записывается 0.0.0.0.
Непроверенный X-Forwarded-For игнорируется; для доверенного reverse proxy нужна явная
настройка. IP/User-Agent и внутренний ID автора не выдаются в публичных ответах.
Публичные поля: id, userName, email, homePage, text, createdAt. Время — UTC ISO 8601
с точностью до микросекунд, одинаковое при создании/чтении. Email — введённые данные,
а не подтверждение владения учётной записью.

Ошибки — ProblemDetails JSON: 400 при неверном вводе/JSON/limit, 404 при отсутствии записи,
413 при слишком большом теле, 415 при неподдерживаемом формате, 503 при недоступности БД.
Ответ валидации содержит errors с именами JSON-полей. Внутренние исключения и параметры
подключения не выдаются. Отмена запроса передаётся операциям данных.

SPA-форма/таблица использует эти маршруты в 1.1.0.5; ответы,
CAPTCHA, XHTML и вложения остаются последующими этапами. Схема 1.1.0.3 сохраняется;
миграция 20261029195935_InitialDataModel не меняется.

## Українська

API створює та читає текстові коментарі. HTTP-обробники викликають сценарії Application;
валідація, нормалізація та публічні DTO не залежать від ASP.NET/EF.

| Метод | Маршрут | Результат |
|---|---|---|
| POST | `/api/comments` | 201, публічний коментар і відносний заголовок Location |
| GET | `/api/comments/{id}` | 200, публічний коментар; 404 для відсутнього UUID |
| GET | `/api/comments?limit=25` | 200, масив останніх коментарів; типово 25, діапазон 1–100 |

Список відсортований за CreatedAt, потім UUID за спаданням. Це обмежене вікно останніх
записів; переходи між сторінками та всі напрями сортування належать до 1.1.0.8.

POST приймає лише JSON: userName, email, необов’язковий homePage та text. Невідомі
поля, включно з clientIp, userAgent, createdAt і parentId, відхиляються. Пробіли навколо
імені/email/homepage видаляються; порожній homepage перетворюється на null. Пробіли тексту,
Unicode, RTL та emoji зберігаються. Ім’я обов’язкове: 1–100 латинських літер/цифр. Email:
адреса до 254 символів без відображуваного імені та керувальних символів. Homepage: абсолютний
HTTP(S) URL до 2048 символів без облікових даних. Текст: не лише пробіли, до 65536 одиниць
UTF-16; нульові символи, некоректний Unicode та схожа на HTML розмітка відхиляються.
Навіть майбутні дозволені XHTML-теги вмикаються лише в 1.1.0.6. Числові порівняння
`1 < 2` залишаються звичайним текстом. Межа тіла запиту — 1 МіБ з урахуванням JSON-кодування.

Сервер створює UUID/час, отримує IP мережевого з’єднання та User-Agent (до 2048 символів),
зберігає автора/коментар разом. Якщо IP недоступний, записується 0.0.0.0.
Неперевірений X-Forwarded-For ігнорується; для довіреного reverse proxy потрібне явне
налаштування. IP/User-Agent і внутрішній ID автора не надаються в публічних відповідях.
Публічні поля: id, userName, email, homePage, text, createdAt. Час — UTC ISO 8601
з точністю до мікросекунд, однаковий під час створення/читання. Email — введені дані,
а не підтвердження володіння обліковим записом.

Помилки — ProblemDetails JSON: 400 за неправильного вводу/JSON/limit, 404 за відсутності запису,
413 за надто великого тіла, 415 за непідтримуваного формату, 503 за недоступності БД.
Відповідь валідації містить errors з іменами JSON-полів. Внутрішні винятки й параметри
підключення не надаються. Скасування запиту передається операціям даних.

SPA-форма/таблиця використовує ці маршрути в 1.1.0.5; відповіді,
CAPTCHA, XHTML і вкладення залишаються наступними етапами. Схема 1.1.0.3 зберігається;
міграція 20261029195935_InitialDataModel не змінюється.

## Example / Пример / Приклад

```bash
curl --fail-with-body -H 'Content-Type: application/json' \
  -d '{"userName":"Reader123","email":"reader@example.test","homePage":null,"text":"Hello"}' \
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
  "createdAt": "2026-10-29T12:00:00.123456+00:00"
}
```
