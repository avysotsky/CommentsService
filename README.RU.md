# CommentsService

Версия: **1.1.0.1** · Ветка: `CommentsService/v_1.1.0.1_Init`

[English](README.md) · [Русский](README.RU.md) · [Українська](README.UA.md)

[История изменений](change.log) · [read.me](read.me) · [План разработки](docs/PLAN.md)

CommentsService предназначен для комментариев и обсуждений с ответами. В этой
начальной версии есть только стартовая страница React и минимальный API на .NET.
Отправка комментариев, хранение данных, миграции БД, Docker и вложения пока не реализованы.

Интерфейс английский. Сообщения на любых языках появятся вместе с формой комментария.
Это начальный каркас, а не завершённый сервис для публичного размещения.

## Требования

- Стабильный .NET SDK 10.
- Node.js 24 и npm.
- Git для получения исходников.

Для Init не требуются БД, строка подключения или конфиг приложения.

## Сборка

Из корня репозитория:

```bash
dotnet build src/CommentsService.Api -c Release
npm --prefix src/comments-web ci
npm --prefix src/comments-web run build
```

## Запуск начального каркаса

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
