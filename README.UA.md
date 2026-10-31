# CommentsService

Версія: **1.1.1.16** · Гілка: `CommentsService/v_1.1.1.16_Deployment`

Deployment 1.1.1.16 доступний: [живе демо](https://comments.alexvysotsky.com). Пройшли288backend (27/84/56/121),26frontend, збирання frontend/Docker, публічні HTTPS/HSTS/CAPTCHA/TXT/браузер, захист довіреного проксі, перепідключення й відбитки поточних користувацьких даних. Користувач підтвердив запис із мобільного інтернету. [Розгортання й обмеження](docs/DEPLOYMENT_VERIFICATION.md). GUI Workbench і повне перезавантаження Windows не перевірено.

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

Потрібні Docker Engine, Compose v2.24+ та Python 3. Docker збирає застосунок; локальні .NET/Node потрібні лише для тестів на хості. З кореня репозиторію:

```bash
python3 scripts/init-env.py
docker compose -p commentsservice --profile testing up -d --wait database test-database
python3 scripts/db-checkpoint.py
docker compose -p commentsservice build app
python3 scripts/db-command.py migrate
python3 scripts/db-command.py migrate --service test-database
docker compose -p commentsservice up -d --no-build --wait app
curl --fail http://127.0.0.1:8080/health/database
```

Відкрийте [localhost:8080](http://localhost:8080/) на Docker-хості. За потреби використовуйте sudo для Docker. Облікові дані створюються один раз у приватному `.env`; не публікуйте його та не перезаписуйте паролі наявних томів. Щоб вибрати вільні порти **до першої генерації**, використовуйте `python3 scripts/init-env.py --port 8081 --test-db-port 55435`. Наявний файл оточення зберігається разом із портами.

Під час оновлень зберігайте назву Compose-проєкту та файл оточення. Розробка використовує `commentsservice_development_data`, тести — окремий `commentsservice_test_data`; обидва містять постійні дані PostgreSQL. Застосунок використовує роль `comments_app` без прав суперкористувача. Під час старту міграції автоматично не застосовуються.

## Оновлення та відновлення

1. Для зміни лише коду перебудуйте й перезапустіть застосунок із попередніми оточенням, проєктом і томами. У 1.1.1.16 немає нової міграції або залежності.
2. Перед зміною схеми/даних зупиніть усіх писачів, виконайте `scripts/db-checkpoint.py`, потім явно мігруйте всі використані БД. Додайте `--include-benchmark`, якщо існує збережена benchmark-БД. Запускайте попередні томи.
3. Зберігайте поточні БД та одну попередню копію, перевірену відновленням. Не скидайте спільні дані для звичайних тестів і не видаляйте постійні томи. За потреби зупиніть сервіси: `docker compose -p commentsservice --profile testing down`; томи не видаляйте.
4. Перевірте наявну копію без її заміни:

```bash
python3 scripts/verify-checkpoint.py artifacts/checkpoints/database.dump --upgrade
```

Команда відновлює лише в нову БД з унікальною назвою, перевіряє manifest/SHA-256 і відбитки рядків, за потреби застосовує поточний ідемпотентний SQL та видаляє лише свою тимчасову БД. Робоче підключення застосунку не перемикається. Відновлення повертає дані на момент копії, без подальших записів. Див. [міграції/відновлення](docs/database/MIGRATIONS.md).

Актуальні всі чотири міграції:

- `20261029195935_InitialDataModel`
- `20261030101720_CommentThreads`
- `20261030122207_ImageAttachments`
- `20261030130243_TextAttachments`

## Перевірки

Для тестів на хості потрібні .NET 10 SDK та Node.js 24; за відсутності frontend-залежностей виконайте `npm --prefix src/comments-web ci`. Профіль testing має працювати.

```bash
python3 scripts/db-command.py check
python3 scripts/db-command.py check --service test-database
python3 scripts/verify-schema.py
python3 scripts/verify-schema.py --service test-database --database comments_test
python3 scripts/test-backend.py
npm --prefix src/comments-web test
npm --prefix src/comments-web run build
```

DB-тести відкочують транзакції; HTTP-фікстури видаляють лише свої рядки. `verify-schema.py` порівнює каталог PostgreSQL із Workbench-проєкцією, не підтверджуючи імпорт через GUI. Команди розгортання/копій/схеми підтримують `--env-file`, `--project` та повторюваний `--compose-file`; використовуйте однакові параметри всюди. Backend-тести додатково підтримують `--host`/`--port`.

У Visual Studio 2026 відкривайте `CommentsService.slnx`: backend/test-проєкти та джерела frontend. Рішення збирає backend/тести, Docker — увесь застосунок. Каталоги Windows/Linux розділено.

## Запуск і приймання

`GET /api/status` повертає `{"application":"CommentsService","version":"1.1.1.16","stage":"Deployment"}`. `/health` перевіряє процес; `/health/database` — підключення й актуальність міграцій (503 за відмови/старої схеми). [Контракт API](docs/API.md).

JSON створення/перегляду потребує UTF-8, точних camelCase-полів і не більше 1 МіБ, включно з chunked. Невідомі/повторні/відмінні регістром поля та хибний ввід дають 400; великий JSON — 413; непідтримувані MIME/кодування — 415. CAPTCHA зберігається в процесі, діє п’ять хвилин і витрачається спробою перевірки. Після невдалої/невизначеної відправки оновіть її. Байти вкладень зберігаються транзакційно в PostgreSQL, входять у backup та не включаються в DTO списків.

Автоматичне приймання пройшло: 268 backend- і 26 frontend-тестів; збирання frontend/Docker; чотири етапи схеми й заборони відкочування; відновлення/оновлення трьох збережених копій; окреме встановлення на порожні БД; обмежені ролі; відновлення поточних даних із 7 точними вкладеннями й повторне створення контейнера; браузерні перевірки файлів/RTL/захисту/пагінації; 18 сценаріїв збереженого мільйона. Початкові БД, набір, томи, оточення та хеші копій не змінилися.

**Не підтверджено:** GUI-імпорт MySQL Workbench і контрольоване перезавантаження Windows. Публічний HTTPS розгорнуто; поточний автозапуск потребує входу користувача. [Перевірки Deployment](docs/DEPLOYMENT_VERIFICATION.md).

[Схема PostgreSQL / Workbench-проєкція](docs/database/SCHEMA.md) · [Архітектура](docs/ARCHITECTURE.md) · [Великий набір](docs/BENCHMARK.md)
