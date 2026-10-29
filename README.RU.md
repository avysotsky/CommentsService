# CommentsService

Версия: **1.1.0.3** · Ветка: `CommentsService/v_1.1.0.3_BackendLayers`

[English](README.md) · [Русский](README.RU.md) · [Українська](README.UA.md)

[История изменений](change.log.ru) · [Краткая инструкция](read.me) · [План](docs/PLAN.md)

React и .NET 10 работают в одном Docker-контейнере с PostgreSQL 17 на постоянном
хранилище. Добавлены сущности автора/комментария, настройки EF Core, IP/User-Agent
клиента, время UTC и первая версионированная миграция.
Стартовая страница показывает связь с API и выпуск. Отправка/чтение комментариев,
ответы, CAPTCHA и вложения относятся к следующим этапам; это промежуточная версия.

## Развёртывание на своей инфраструктуре

Нужны Docker Engine, Compose v2.24+, Git и Python 3. Docker собирает .NET/React;
SDK и Node на хосте для развёртывания не нужны. Из корня репозитория:

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

Откройте http://localhost:8080 на Docker-хосте. При необходимости используйте sudo.
`commentsservice` — пример имени проекта; сохраняйте его и файл окружения при
обновлениях, чтобы использовать прежние тома. Другой порт можно выбрать один раз:
`python3 scripts/init-env.py --port 8081`.

Тестовая БД — отдельный постоянный сервис профиля `testing`.
Проверка контрольной копии охватывает обе БД, поэтому сначала запустите обе.
Миграции запускаются явно; приложение не применяет их автоматически при старте.
Обновление выполняется так же: остановите запись, запустите БД, проверьте контрольную
копию, соберите выбранную версию, примените миграции, запустите приложение и проверьте БД.

## Данные и восстановление

- Разработка: БД `comments`, том `commentsservice_development_data`.
- Тесты: `comments_test`, том `commentsservice_test_data`, локальный порт 55434.
- Роль приложения: `comments_app`, без прав суперпользователя, создания ролей и БД.
- Таблицы: `comments.Authors`, `comments.Comments`; история: `comments.__EFMigrationsHistory`.
- Миграция: `20261029195935_InitialDataModel`, добавляет схему к этапу Docker.
- Контрольные копии: `artifacts/checkpoints`, один проверенный дамп/описание на каждую БД.
  Скрипт восстанавливает их во временные БД с уникальными именами, сравнивает отпечатки
  строк и удаляет только эти временные БД. Прежние копии заменяются после проверки новых.

Храните копии приватно; важные данные дополнительно копируйте в защищённое хранилище.
При создании копии остановите запись; обнаруженные изменения приводят к ошибке проверки.
Обычный запуск и тесты сохраняют БД и тома. Не используйте `down -v` и общую очистку таблиц.
Подробности: [схема и диаграммы](docs/database/SCHEMA.md), [миграции и восстановление](docs/database/MIGRATIONS.md).

## Проверка

```bash
python3 scripts/db-command.py check
python3 scripts/db-command.py check --service test-database
dotnet test tests/CommentsService.Domain.Tests -c Release
python3 scripts/test-data-model.py
```

Для обоих наборов тестов нужен SDK .NET 10 на хосте. Тесты БД используют `comments_test`,
читают созданный файл паролей и откатывают транзакцию каждого теста.
Для другой машины/порта укажите `--host`/`--port` в команде тестов.
Скрипты команд БД и контрольных копий принимают `--env-file`, `--project` и повторяемый
`--compose-file`. Используйте одинаковые параметры во всех командах развёртывания.
Чтение паролей рассчитано на формат KEY=value, создаваемый `init-env.py`.

`GET /api/status` возвращает `{"application":"CommentsService","version":"1.1.0.3","stage":"DataModel"}`.
`GET /health` проверяет процесс API; `/health/database` — подключение и неприменённые
миграции, возвращая 503 при недоступности или устаревшей схеме. API комментариев пока возвращает 404.

## Пароли, остановка и IDE

`init-env.py` создаёт приватный `.env` один раз и сохраняет существующий файл.
Не публикуйте пароли и копии БД. Изменение .env не меняет пароль существующей роли.
Приложение слушает loopback; для внешнего доступа настройте свой HTTPS reverse proxy.
Остановка с сохранением данных:

```bash
docker compose -p commentsservice --profile testing down
```

В Visual Studio 2026 откройте `CommentsService.slnx`; нужны SDK .NET 10 и нагрузка
ASP.NET/веб. Решение собирает API и тесты; Frontend содержит редактируемые файлы React.
Docker собирает приложение целиком. Промежуточные каталоги Windows и Linux разделены.
Без переменных БД API/страница статуса запускаются; проверка БД возвращает 503.
Для подключения из IDE задайте `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`
в приватном окружении запуска. Не коммитьте их значения.

## Слои backend

Backend разделён на три проекта: Domain/Entities, Infrastructure (Persistence/Configurations
и Migrations) и Api. Domain не зависит от EF и HTTP. API использует внедряемые
контракты операций БД; тесты разделены на доменные/архитектурные и интеграционные.
См. [архитектуру и границы SOLID](docs/ARCHITECTURE.md). Исправление BackendLayers
сохраняет версию 1.1.0.3, идентификатор миграции и существующие данные.
