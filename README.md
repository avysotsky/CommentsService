# CommentsService

Version: **1.1.0.1** · Branch: `CommentsService/v_1.1.0.1_Init`

[Change log](change.log) · [read.me](read.me) · [Development plan](docs/PLAN.md)

## English

CommentsService will provide comments and threaded discussions. This initial release
contains only a React start page and a minimal .NET API. Comment submission, storage,
database migrations, Docker packaging and attachments are not implemented yet.

The interface is in English. Multilingual messages are planned for the comment form.
This is an initial preview, not a completed service for public hosting.

### Requirements

- .NET SDK 10, stable release.
- Node.js 24 with npm.
- Git to obtain the source.

No database, connection string or application configuration is required for Init.

### Build

From the repository root:

```bash
dotnet build src/CommentsService.Api -c Release
npm --prefix src/comments-web ci
npm --prefix src/comments-web run build
```

### Run the initial preview

Start the API in one terminal:

```bash
dotnet run --project src/CommentsService.Api --no-launch-profile --urls http://127.0.0.1:5081
```

In a second terminal:

```bash
npm --prefix src/comments-web run dev
```

Open http://localhost:5174 on the machine running the application. The start page
shows the API connection state. The frontend development server forwards `/api`
to the backend on port 5081. Stop either process with Ctrl+C in its terminal.

`GET http://127.0.0.1:5081/api/status` returns:

```json
{"application":"CommentsService","version":"1.1.0.1","stage":"Init"}
```

Docker and persistent database deployment belong to the next iteration. Later
versions will include their own current installation instructions. See change.log
for the functionality actually delivered by each release.

## Русский

CommentsService предназначен для комментариев и обсуждений с ответами. В этой
начальной версии есть только стартовая страница React и минимальный API на .NET.
Отправка комментариев, хранение данных, миграции БД, Docker и вложения пока не реализованы.

Интерфейс английский. Сообщения на любых языках появятся вместе с формой комментария.
Это начальный каркас, а не завершённый сервис для публичного размещения.

### Требования

- Стабильный .NET SDK 10.
- Node.js 24 и npm.
- Git для получения исходников.

Для Init не требуются БД, строка подключения или конфиг приложения.

### Сборка

Из корня репозитория:

```bash
dotnet build src/CommentsService.Api -c Release
npm --prefix src/comments-web ci
npm --prefix src/comments-web run build
```

### Запуск начального каркаса

В первом терминале запустите API:

```bash
dotnet run --project src/CommentsService.Api --no-launch-profile --urls http://127.0.0.1:5081
```

Во втором терминале:

```bash
npm --prefix src/comments-web run dev
```

Откройте http://localhost:5174 на машине, где работает приложение. Стартовая
страница показывает состояние соединения с API. Сервер frontend перенаправляет
`/api` на backend с портом 5081. Для остановки процесса нажмите Ctrl+C в его терминале.

`GET http://127.0.0.1:5081/api/status` возвращает:

```json
{"application":"CommentsService","version":"1.1.0.1","stage":"Init"}
```

Docker и постоянная БД относятся к следующей итерации. Каждый следующий выпуск
получит актуальную инструкцию запуска. Реализованные возможности фиксируются в change.log.
