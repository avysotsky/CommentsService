# CommentsService

Версія: **1.1.0.3** · Гілка: `CommentsService/v_1.1.0.3_DataModel`

[English](README.md) · [Русский](README.RU.md) · [Українська](README.UA.md)

[Історія змін](change.log.ua) · [Коротка інструкція](read.me) · [План](docs/PLAN.md)

React і .NET 10 працюють в одному Docker-контейнері з PostgreSQL 17 на постійному
сховищі. Додано сутності автора/коментаря, налаштування EF Core, IP/User-Agent
клієнта, час UTC і першу версіоновану міграцію.
Стартова сторінка показує зв’язок з API та випуск. Надсилання/читання коментарів,
відповіді, CAPTCHA й вкладення належать до наступних етапів; це проміжна версія.

## Розгортання на власній інфраструктурі

Потрібні Docker Engine, Compose v2.24+, Git і Python 3. Docker збирає .NET/React;
SDK і Node на хості для розгортання не потрібні. Із кореня репозиторію:

```bash
python3 scripts/init-env.py
docker compose -p commentsservice --profile testing up -d --wait database test-database
python3 scripts/db-checkpoint.py
docker compose -p commentsservice build app
python3 scripts/db-command.py migrate
python3 scripts/db-command.py migrate --service test-database
docker compose -p commentsservice up -d --no-build --wait
curl --fail http://127.0.0.1:8080/health/database
```

Відкрийте http://localhost:8080 на Docker-хості. За потреби використовуйте sudo.
`commentsservice` — приклад назви проєкту; зберігайте її та файл оточення під час
оновлень, щоб використовувати попередні томи. Інший порт можна вибрати один раз:
`python3 scripts/init-env.py --port 8081`.

Тестова БД — окремий постійний сервіс профілю `testing`.
Перевірка контрольної копії охоплює обидві БД, тому спочатку запустіть обидві.
Міграції запускаються явно; застосунок не застосовує їх автоматично під час старту.
Оновлення виконується так само: зупиніть запис, запустіть БД, перевірте контрольну
копію, зберіть вибрану версію, застосуйте міграції, запустіть застосунок і перевірте БД.

## Дані та відновлення

- Розробка: БД `comments`, том `commentsservice_development_data`.
- Тести: `comments_test`, том `commentsservice_test_data`, локальний порт 55434.
- Роль застосунку: `comments_app`, без прав суперкористувача, створення ролей і БД.
- Таблиці: `comments.Authors`, `comments.Comments`; історія: `comments.__EFMigrationsHistory`.
- Міграція: `20261029195935_InitialDataModel`, додає схему до етапу Docker.
- Контрольні копії: `artifacts/checkpoints`, один перевірений дамп/опис для кожної БД.
  Скрипт відновлює їх у тимчасові БД з унікальними назвами, порівнює відбитки
  рядків і видаляє лише ці тимчасові БД. Попередні копії замінюються після перевірки нових.

Зберігайте копії приватно; важливі дані додатково копіюйте в захищене сховище.
Під час створення копії зупиніть запис; виявлені зміни спричиняють помилку перевірки.
Звичайний запуск і тести зберігають БД та томи. Не використовуйте `down -v` і загальне очищення таблиць.
Докладніше: [схема й діаграми](docs/database/SCHEMA.md), [міграції та відновлення](docs/database/MIGRATIONS.md).

## Перевірка

```bash
python3 scripts/db-command.py check
python3 scripts/db-command.py check --service test-database
python3 scripts/test-data-model.py
```

SDK .NET 10 на хості потрібен лише інтеграційним тестам. Вони використовують `comments_test`,
читають створений файл паролів і відкочують транзакцію кожного тесту.
Для іншої машини/порту вкажіть `--host`/`--port` у команді тестів.
Скрипти команд БД і контрольних копій приймають `--env-file`, `--project` та повторюваний
`--compose-file`. Використовуйте однакові параметри в усіх командах розгортання.
Читання паролів розраховано на формат KEY=value, створюваний `init-env.py`.

`GET /api/status` повертає `{"application":"CommentsService","version":"1.1.0.3","stage":"DataModel"}`.
`GET /health` перевіряє процес API; `/health/database` — підключення та незастосовані
міграції, повертаючи 503 за недоступності або застарілої схеми. API коментарів поки повертає 404.

## Паролі, зупинка та IDE

`init-env.py` створює приватний `.env` один раз і зберігає наявний файл.
Не публікуйте паролі та копії БД. Зміна .env не змінює пароль наявної ролі.
Застосунок слухає loopback; для зовнішнього доступу налаштуйте власний HTTPS reverse proxy.
Зупинка зі збереженням даних:

```bash
docker compose -p commentsservice --profile testing down
```

У Visual Studio 2026 відкрийте `CommentsService.slnx`; потрібні SDK .NET 10 і навантаження
ASP.NET/веб. Рішення збирає API й тести; Frontend містить редаговані файли React.
Docker збирає весь застосунок. Проміжні каталоги Windows і Linux розділено.
Без змінних БД API/сторінка статусу запускаються; перевірка БД повертає 503.
Для підключення з IDE задайте `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`
у приватному оточенні запуску. Не комітьте їхні значення.
