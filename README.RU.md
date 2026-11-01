# CommentsService

Версия: **1.3.3.4** · `CommentsService/v_1.3.3.4_SubmissionDocs`

Исправление документации: восстановление и приёмка используют команды Docker/.NET. Runtime остаётся `1.3.3.4 / CloudAttachments`; код приложения, схема и зависимости прежние.

Supabase Storage Free — дополнительная копия явно разрешённых изображений/TXT. Все оригиналы остаются в PostgreSQL; запись комментария и файла не ждёт Cloud. REST/GraphQL, CAPTCHA, preview, поиск, Events, RabbitMQ, кеш и WS сохраняются при отсутствии или ограничении облака. В этом выпуске разрешены только собственные тестовые комментарии из явного списка. Миграции SQL, массового переноса, outbox и реплик нет.

[Настройка Cloud, ограничения и откат](docs/CLOUD_ATTACHMENTS.md) · [Поиск](docs/COMMENT_SEARCH.md) · [Брокер](docs/MESSAGE_BROKER.md) · [GraphQL](docs/GRAPHQL.md)

Проверено:449 различных backend-тестов (27 Domain,85 Application,67 Data,270 API),36 frontend и сборки frontend/Docker. Реальный Edge создал свои PNG/TXT в корне/ответе с CAPTCHA; проверены точные байты/заголовки/скачивание,6сортировок GraphQL, preview, WS/черновик и EN/RU/UA mobile. Настоящий Supabase принял2своих файла общим размером267байт; авторизованное скачивание совпало, публичный доступ закрыт; финальный образ подтвердил2копии и5проверенных чтений. Запуск с неверным/отсутствующим ключом сохранил запись/TXT/WS/SQL-чтение. Квоты/429/5xx/таймаут/повреждение/параллелизм/повторы/бюджеты проверены подставными ответами провайдера, без реального исчерпания квот/паузы проекта. Events не ждёт заблокированный worker. Свои SQL/cloud-фикстуры удалены; до очистки Cloud подтверждён404 SQL-удалённых ID. Публичные HTTPS/HSTS/БД/GraphQL/search/WSS/reconnect проверены только чтением. Исходные отпечатки данных/схемы/истории/миллиона/копий/env/томов/preview совпали. Прежние Junior/миллион/reboot не повторялись. Рабочие списки разрешения/повтора пусты; пользовательские файлы в Cloud не отправляются.

<!-- CLOUD-ATTACHMENTS-CURRENT-END -->

Проверена установка только Docker в отдельном пустом проекте: создание/сохранение env, восстановление/отпечатки двух начальных копий, явные миграции development/test, паритет схемы, восстановление/обновление/идемпотентность между серверами, Rabbit/ограниченный поиск. В tools нет исполняемого python3 и Docker socket. Все449backend прошли из контейнера tools(27/85/67/270). Edge/все сервисы Middle прошли; C#-проверка Cloud сверила2своих файла267байт и запрет публичного доступа, файл вне списка остался SQL. Негативные случаи отклонили неполный env, неверную цель, SHA копии, изменённую схему и неверные ES-права, сохранив файлы и скрыв секреты. Заполненная своими PNG/TXT/ответом копия также восстановилась/обновилась. Runtime/основной Compose прежние, новый путь обеспечивает только образ инструментов.

[Docker/.NET tools](docs/DOCKER_TOOLS.md).

## Сдача Middle

Реализованы четыре выбранные категории Middle: GraphQL, RabbitMQ, Elasticsearch и Supabase Storage Free. Events/кеш/очередь/WebSocket и прежние функции комментариев/файлов сохранены. [Финальная установка и приёмка](docs/MIDDLE_ACCEPTANCE.md) содержит совместную настройку дополнительных сервисов и воспроизводимую демонстрацию Cloud под управлением владельца. Передача пользовательских файлов в Supabase отключена пустыми списками разрешений; обычная загрузка демо остаётся в SQL. Реальное облако и подставные отказы в отчётах разделены.

Демо: [CommentsService](https://comments.alexvysotsky.com). Runtime остаётся `1.3.3.4 / CloudAttachments`; ветка исправляет документы сдачи и добавляет Cloud-проверку только чтением. Исходные ветки фич сохранены. Прежние268тестов относятся к исторической приёмке Junior; актуальное покрытие449backend/36frontend. Нагрузка Middle+, долговечный outbox и реплики в сдачу не входят.



Структурированные JSON-логи консоли/файла настраиваются через `Runtime.Logging`: InstanceId, TraceId, уровни/категории, ротация и хранение. Docker сохраняет логи в отдельном томе. [Настройки и политика данных](docs/LOGGING.md).

Backend `config.json`: проверяемые идентификатор инстанса, параметры БД и порт health; сохранены переопределения окружением/CLI и PG-переменные. Явные параметры CLI имеют приоритет над PG. [Конфигурация и Docker](docs/CONFIGURATION.md).

Контракты DTO собраны в собственных слоях Application/API, по типу на файл. Маппинг комментария вынесен отдельно; JSON, маршруты и схема БД сохранены. Прежний отчёт приёмки описывает выпуск 1.1.0.15.

[English](README.md) · [Русский](README.RU.md) · [Українська](README.UA.md)

[История изменений](change.log.ru) · [Краткая инструкция](read.me) · [Отчёт приёмки](docs/ACCEPTANCE.md) · [План](docs/PLAN.md)

SPA на .NET 10 / React с PostgreSQL 17, упакованное в Docker. Domain, Application, Infrastructure и API следуют Onion Architecture, OOP и SOLID.

## Возможности

- Комментарии и ответы любой глубины; 25 корней на странице, новые сверху по умолчанию; сортировка имени/e-mail/даты в обе стороны.
- Обязательны имя из латинских букв/цифр, e-mail, текст и CAPTCHA-картинка; полный HTTP(S)-адрес сайта необязателен. E-mail публичен; автор не является аутентифицированной учётной записью.
- Безопасный XHTML: `a` (`href`, `title`), `i`, `strong`, `code`; кнопки форматирования, проверка вложенности и AJAX-предпросмотр. Ошибки сохраняют черновик.
- Одно необязательное изображение **или** TXT. JPG/JPEG/GIF/PNG: до 5 МиБ, 16 млн пикселей и 10000 пикселей на сторону; пропорционально до 320×240 без увеличения, EXIF-ориентация, первый кадр GIF, PNG без исходных метаданных. TXT: 1–102400 байт, строгий UTF-8; BOM/Unicode/CRLF/табуляция сохраняются точно.
- Модальный просмотр изображений/TXT, эффекты, стрелки/Escape, управление фокусом, точные скачивания и уменьшенное движение. EN/RU/UA, вертикальные строки на телефоне и смешанный RTL-текст.
- Серверная/клиентская валидация, параметризованные EF-запросы, лимиты, CSP, безопасные заголовки файлов и общие ошибки. [Объём проверки безопасности](docs/SECURITY.md).

## Первая установка

Нужны только Docker Engine и Compose v2.24+. Отдельный образ инструментов .NET10 обеспечивает установку, обслуживание и backend-тесты; Python и .NET SDK на хосте не требуются. Локальный Node необязателен для тестов разработки frontend. Из корня репозитория:

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

Откройте [localhost:8080](http://localhost:8080/) на Docker-хосте. При необходимости используйте sudo для Docker. Учётные данные создаются один раз в приватном `.env`; не публикуйте его и не перезаписывайте пароли существующих томов. Чтобы выбрать свободные порты **до первой генерации**, используйте `docker compose -p commentsservice -f compose.tools.yaml run --rm --no-deps tools init-env --port 8081 --test-db-port 55435`. Существующий файл окружения сохраняется вместе с портами.

При обновлениях сохраняйте имя Compose-проекта и файл окружения. Разработка использует `commentsservice_development_data`, тесты — отдельный `commentsservice_test_data`; оба содержат постоянные данные PostgreSQL. Приложение использует роль `comments_app` без прав суперпользователя. При старте миграции автоматически не применяются.

## Обновления и восстановление

1. Для изменения только кода пересоберите и перезапустите приложение с прежними окружением, проектом и томами. Приёмочное исправление сохраняет runtime 1.3.3.4 / CloudAttachments, схему и зависимости приложения.
2. Перед изменением схемы/данных остановите всех писателей, выполните `docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools checkpoint`, затем явно мигрируйте все используемые БД. Добавьте `--include-benchmark`, если существует сохранённая benchmark-БД. Для команд копирования/миграции сохраняйте прежние Compose-проект, приватное окружение и все overrides запуска, затем запускайте прежние тома.
3. Храните текущие БД и одну предыдущую копию, проверенную восстановлением. Не сбрасывайте общие данные для обычных тестов и не удаляйте постоянные тома. При необходимости остановите сервисы: `docker compose -p commentsservice --profile testing down`; тома не удаляйте.
4. Проверьте существующую копию без её замены:

```bash
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-checkpoint --dump artifacts/checkpoints/database.dump --target test --upgrade
```

Команда восстанавливает только в новую БД с уникальным именем, проверяет manifest/SHA-256 и отпечатки строк, при необходимости применяет текущий идемпотентный SQL и удаляет только свою временную БД. Рабочее подключение приложения не переключается. Восстановление возвращает данные на момент копии, без последующих записей. См. [миграции/восстановление](docs/database/MIGRATIONS.md).

Актуальны все четыре миграции:

- `20261029195935_InitialDataModel`
- `20261030101720_CommentThreads`
- `20261030122207_ImageAttachments`
- `20261030130243_TextAttachments`

## Проверки

Backend-тесты выполняются в контейнере .NET-инструментов. Node.js24 необязателен для тестов разработки frontend; зависимости: `npm --prefix src/comments-web ci`. Профиль testing должен работать.

```bash
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools check
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools check --target test
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-schema
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools verify-schema --target test
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools test-backend --target test
npm --prefix src/comments-web test
npm --prefix src/comments-web run build
```

DB-тесты откатывают транзакции; HTTP-фикстуры удаляют только свои строки. `verify-schema` сравнивает каталог PostgreSQL с Workbench-проекцией. Проект, env и overrides задаются параметрами Docker Compose; `--env-file` после имени команды tools выбирает приватный файл утилиты, `--target test` — тестовую БД. [Инструменты Docker/.NET](docs/DOCKER_TOOLS.md).

В Visual Studio 2026 открывайте `CommentsService.slnx`: backend/test-проекты и исходники frontend. Решение собирает backend/тесты, Docker — всё приложение. Каталоги Windows/Linux разделены.

## Запуск и приёмка

`GET /api/status` возвращает `{"application":"CommentsService","version":"1.3.3.4","stage":"CloudAttachments"}`. `/health` проверяет процесс; `/health/database` — подключение и актуальность миграций (503 при отказе/старой схеме). [Контракт API](docs/API.md).

JSON создания/предпросмотра требует UTF-8, точных camelCase-полей и не более 1 МиБ, включая chunked. Неизвестные/повторные/отличающиеся регистром поля и неверный ввод дают 400; большой JSON — 413; неподдерживаемые MIME/кодировка — 415. CAPTCHA хранится в процессе, действует пять минут и расходуется попыткой проверки. После неудачной/неопределённой отправки обновите её. Байты вложений сохраняются транзакционно в PostgreSQL, входят в backup и не включаются в DTO списков.

Историческая приёмка Junior: Автоматическая приёмка прошла: 268 backend- и 26 frontend-тестов; сборки frontend/Docker; четыре этапа схемы и запреты отката; восстановление/обновление трёх сохранённых копий; отдельная установка на пустые БД; ограниченные роли; восстановление текущих данных с 7 точными вложениями и пересоздание контейнера; браузерные проверки файлов/RTL/защиты/пагинации; 18 сценариев сохранённого миллиона. Исходные БД, набор, тома, окружение и хеши копий не изменились.

Текущие настройки, проверки и оставшиеся оговорки ручных инструментов: [RuntimeLimits](docs/RUNTIME_LIMITS.md).

[Схема PostgreSQL / Workbench-проекция](docs/database/SCHEMA.md) · [Архитектура](docs/ARCHITECTURE.md) · [Большой набор](docs/BENCHMARK.md)
