# CommentsService

Версія: **1.3.3.4** · `CommentsService/v_1.3.3.4_SubmissionDocs`

Виправлення документації: відновлення та приймання використовують команди Docker/.NET. Runtime залишається `1.3.3.4 / CloudAttachments`; код застосунку, схема та залежності попередні.

Supabase Storage Free — додаткова копія явно дозволених зображень/TXT. Усі оригінали залишаються в PostgreSQL; запис коментаря/файлу не чекає Cloud. REST/GraphQL, CAPTCHA, preview, пошук, Events, RabbitMQ, кеш і WS працюють за відсутності чи обмеження хмари. У випуску дозволено лише власні тестові коментарі з явного списку. Міграцій SQL, масового перенесення, outbox і реплік немає.

[Налаштування Cloud, обмеження та відкат](docs/CLOUD_ATTACHMENTS.md) · [Пошук](docs/COMMENT_SEARCH.md) · [Брокер](docs/MESSAGE_BROKER.md) · [GraphQL](docs/GRAPHQL.md)

Перевірено:449різних backend-тестів (27 Domain,85 Application,67 Data,270 API),36 frontend і збирання frontend/Docker. Справжній Edge створив власні PNG/TXT у корені/відповіді з CAPTCHA; перевірено точні байти/заголовки/завантаження,6сортувань GraphQL, preview, WS/чернетку та EN/RU/UA mobile. Справжній Supabase прийняв2власні файли загалом267байтів; авторизоване завантаження збіглося, публічний доступ закрито; фінальний образ підтвердив2копії та5перевірених читань. Старт із хибним/відсутнім ключем зберіг запис/TXT/WS/SQL-читання. Квоти/429/5xx/таймаут/пошкодження/паралельність/повтори/бюджети перевірено підставними відповідями провайдера, без справжнього вичерпання квот/паузи проєкту. Events не чекає заблокований worker. Власні SQL/cloud-фікстури видалено; до очищення Cloud підтверджено404 SQL-видалених ID. Публічні HTTPS/HSTS/БД/GraphQL/search/WSS/reconnect перевірено лише читанням. Початкові відбитки даних/схеми/історії/мільйона/копій/env/томів/preview збіглися. Попередні Junior/мільйон/reboot не повторювалися. Робочі списки дозволу/повтору порожні; користувацькі файли в Cloud не надсилаються.

<!-- CLOUD-ATTACHMENTS-CURRENT-END -->

Перевірено інсталяцію лише Docker в окремому порожньому проєкті: створення/збереження env, відновлення/відбитки двох початкових копій, явні міграції development/test, паритет схеми, відновлення/оновлення/ідемпотентність між серверами, Rabbit/обмежений пошук. У tools немає виконуваного python3 і Docker socket. Усі449backend пройшли з контейнера tools(27/85/67/270). Edge/усі сервіси Middle пройшли; C#-перевірка Cloud звірила2власні файли267байтів і заборону публічного доступу, файл поза списком залишився SQL. Негативні випадки відхилили неповний env, хибну ціль, SHA копії, змінену схему й хибні ES-права, зберігши файли та приховавши секрети. Заповнена власними PNG/TXT/відповіддю копія також відновилася/оновилася. Runtime/основний Compose попередні, новий шлях забезпечує лише образ інструментів.

[Docker/.NET tools](docs/DOCKER_TOOLS.md).

## Здача Middle

Реалізовано чотири обрані категорії Middle: GraphQL, RabbitMQ, Elasticsearch і Supabase Storage Free. Events/кеш/черга/WebSocket та попередні функції коментарів/файлів збережені. [Фінальна інсталяція та приймання](docs/MIDDLE_ACCEPTANCE.md) містить спільне налаштування додаткових сервісів і відтворювану демонстрацію Cloud під керуванням власника. Передавання користувацьких файлів до Supabase вимкнене порожніми списками дозволів; звичайне завантаження демо залишається в SQL. Справжню хмару та підставні відмови у звітах розділено.

Демо: [CommentsService](https://comments.alexvysotsky.com). Runtime залишається `1.3.3.4 / CloudAttachments`; гілка виправляє документи здачі та додає Cloud-перевірку лише читанням. Початкові гілки фіч збережені. Попередні268тестів належать до історичного приймання Junior; актуальне покриття449backend/36frontend. Навантаження Middle+, довговічний outbox і репліки до здачі не входять.



Структуровані JSON-логи консолі/файла налаштовуються через `Runtime.Logging`: InstanceId, TraceId, рівні/категорії, ротація та зберігання. Docker зберігає логи в окремому томі. [Налаштування й політика даних](docs/LOGGING.md).

Backend `config.json`: перевірювані ідентифікатор інстанса, параметри БД та порт health; збережено перевизначення оточенням/CLI та PG-змінні. Явні параметри CLI мають пріоритет над PG. [Конфігурація й Docker](docs/CONFIGURATION.md).

Контракти DTO зібрано у власних шарах Application/API, по типу на файл. Маппінг коментаря винесено окремо; JSON, маршрути й схему БД збережено. Попередній звіт приймання описує випуск 1.1.0.15.

[English](README.md) · [Русский](README.RU.md) · [Українська](README.UA.md)

[Історія змін](change.log.ua) · [Коротка інструкція](read.me) · [Звіт приймання](docs/ACCEPTANCE.md) · [План](docs/PLAN.md)

SPA на .NET 10 / React із PostgreSQL 17, упаковане в Docker. Domain, Application, Infrastructure та API дотримуються Onion Architecture, OOP і SOLID.

## Можливості

- Коментарі та відповіді будь-якої глибини; 25 коренів на сторінці, нові зверху типово; сортування імені/e-mail/дати в обидва боки.
- Обов’язкові ім’я з латинських літер/цифр, e-mail, текст і CAPTCHA-зображення; повна HTTP(S)-адреса сайту необов’язкова. E-mail публічний; автор не є автентифікованим обліковим записом.
- Безпечний XHTML: `a` (`href`, `title`), `i`, `strong`, `code`; кнопки форматування, перевірка вкладеності та AJAX-перегляд. Помилки зберігають чернетку.
- Одне необов’язкове зображення **або** TXT. JPG/JPEG/GIF/PNG: до 5 МіБ, 16 млн пікселів і 10000 пікселів на бік; пропорційно до 320×240 без збільшення, EXIF-орієнтація, перший кадр GIF, PNG без початкових метаданих. TXT: 1–102400 байтів, суворий UTF-8; BOM/Unicode/CRLF/табуляція зберігаються точно.
- Модальний перегляд зображень/TXT, ефекти, стрілки/Escape, керування фокусом, точні завантаження та зменшений рух. EN/RU/UA, вертикальні рядки на телефоні й змішаний RTL-текст.
- Серверна/клієнтська валідація, параметризовані EF-запити, межі, CSP, безпечні заголовки файлів та загальні помилки. [Обсяг перевірки безпеки](docs/SECURITY.md).

## Перше встановлення

Потрібні лише Docker Engine і Compose v2.24+. Окремий образ інструментів .NET10 забезпечує інсталяцію, обслуговування та backend-тести; Python і .NET SDK на хості не потрібні. Локальний Node необов’язковий для тестів розробки frontend. З кореня репозиторію:

```bash
docker compose -p commentsservice -f compose.tools.yaml build tools
docker compose -p commentsservice -f compose.tools.yaml run --rm --no-deps tools init-env
docker compose -p commentsservice --profile testing up -d --wait database test-database
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools checkpoint
docker compose -p commentsservice build app
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools migrate
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools migrate --target test
docker compose -p commentsservice up -d --no-build --wait app
curl --fail http://127.0.0.1:8080/health/database
```

Відкрийте [localhost:8080](http://localhost:8080/) на Docker-хості. За потреби використовуйте sudo для Docker. Облікові дані створюються один раз у приватному `.env`; не публікуйте його та не перезаписуйте паролі наявних томів. Щоб вибрати вільні порти **до першої генерації**, використовуйте `docker compose -p commentsservice -f compose.tools.yaml run --rm --no-deps tools init-env --port 8081 --test-db-port 55435`. Наявний файл оточення зберігається разом із портами.

Під час оновлень зберігайте назву Compose-проєкту та файл оточення. Розробка використовує `commentsservice_development_data`, тести — окремий `commentsservice_test_data`; обидва містять постійні дані PostgreSQL. Застосунок використовує роль `comments_app` без прав суперкористувача. Під час старту міграції автоматично не застосовуються.

## Оновлення та відновлення

1. Для зміни лише коду перебудуйте й перезапустіть застосунок із попередніми оточенням, проєктом і томами. Приймальне виправлення зберігає runtime 1.3.3.4 / CloudAttachments, схему та залежності застосунку.
2. Перед зміною схеми/даних зупиніть усіх писачів, виконайте `docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools checkpoint`, потім явно мігруйте всі використані БД. Додайте `--include-benchmark`, якщо існує збережена benchmark-БД. Для команд копіювання/міграції зберігайте попередні Compose-проєкт, приватне оточення й усі overrides запуску, потім запускайте попередні томи.
3. Зберігайте поточні БД та одну попередню копію, перевірену відновленням. Не скидайте спільні дані для звичайних тестів і не видаляйте постійні томи. За потреби зупиніть сервіси: `docker compose -p commentsservice --profile testing down`; томи не видаляйте.
4. Перевірте наявну копію без її заміни:

```bash
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-checkpoint --dump artifacts/checkpoints/database.dump --target test --upgrade
```

Команда відновлює лише в нову БД з унікальною назвою, перевіряє manifest/SHA-256 і відбитки рядків, за потреби застосовує поточний ідемпотентний SQL та видаляє лише свою тимчасову БД. Робоче підключення застосунку не перемикається. Відновлення повертає дані на момент копії, без подальших записів. Див. [міграції/відновлення](docs/database/MIGRATIONS.md).

Актуальні всі чотири міграції:

- `20261029195935_InitialDataModel`
- `20261030101720_CommentThreads`
- `20261030122207_ImageAttachments`
- `20261030130243_TextAttachments`

## Перевірки

Backend-тести виконуються в контейнері .NET-інструментів. Node.js24 необов’язковий для тестів розробки frontend; залежності: `npm --prefix src/comments-web ci`. Профіль testing має працювати.

```bash
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools check
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools check --target test
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-schema
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-schema --target test
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools test-backend --target test
npm --prefix src/comments-web test
npm --prefix src/comments-web run build
```

DB-тести відкочують транзакції; HTTP-фікстури видаляють лише свої рядки. `verify-schema` порівнює каталог PostgreSQL з Workbench-проєкцією. Проєкт, env і overrides задаються параметрами Docker Compose; `--env-file` після імені команди tools обирає приватний файл утиліти, `--target test` — тестову БД. [Інструменти Docker/.NET](docs/DOCKER_TOOLS.md).

У Visual Studio 2026 відкривайте `CommentsService.slnx`: backend/test-проєкти та джерела frontend. Рішення збирає backend/тести, Docker — увесь застосунок. Каталоги Windows/Linux розділено.

## Запуск і приймання

`GET /api/status` повертає `{"application":"CommentsService","version":"1.3.3.4","stage":"CloudAttachments"}`. `/health` перевіряє процес; `/health/database` — підключення й актуальність міграцій (503 за відмови/старої схеми). [Контракт API](docs/API.md).

JSON створення/перегляду потребує UTF-8, точних camelCase-полів і не більше 1 МіБ, включно з chunked. Невідомі/повторні/відмінні регістром поля та хибний ввід дають 400; великий JSON — 413; непідтримувані MIME/кодування — 415. CAPTCHA зберігається в процесі, діє п’ять хвилин і витрачається спробою перевірки. Після невдалої/невизначеної відправки оновіть її. Байти вкладень зберігаються транзакційно в PostgreSQL, входять у backup та не включаються в DTO списків.

Історичне приймання Junior: Автоматичне приймання пройшло: 268 backend- і 26 frontend-тестів; збирання frontend/Docker; чотири етапи схеми й заборони відкочування; відновлення/оновлення трьох збережених копій; окреме встановлення на порожні БД; обмежені ролі; відновлення поточних даних із 7 точними вкладеннями й повторне створення контейнера; браузерні перевірки файлів/RTL/захисту/пагінації; 18 сценаріїв збереженого мільйона. Початкові БД, набір, томи, оточення та хеші копій не змінилися.

Поточні налаштування, перевірки та застереження ручних інструментів: [RuntimeLimits](docs/RUNTIME_LIMITS.md).

[Схема PostgreSQL / Workbench-проєкція](docs/database/SCHEMA.md) · [Архітектура](docs/ARCHITECTURE.md) · [Великий набір](docs/BENCHMARK.md)
