# CommentsService

Версія: **1.1.0.1** · Гілка: `CommentsService/v_1.1.0.1_Init`

[English](README.md) · [Русский](README.RU.md) · [Українська](README.UA.md)

[Історія змін](change.log.ua) · [read.me](read.me) · [План розробки](docs/PLAN.md)

CommentsService призначений для коментарів та обговорень із відповідями.
Ця початкова версія містить лише стартову сторінку React і мінімальний API на .NET.
Надсилання коментарів, зберігання даних, міграції БД, Docker і вкладення ще не реалізовані.

Інтерфейс англійською. Повідомлення будь-якою мовою з’являться разом із формою коментаря.
Це початковий каркас, а не завершений сервіс для публічного розміщення.

## Вимоги

- Стабільний .NET SDK 10.
- Node.js 24 та npm.
- Git для отримання вихідного коду.

Для Init не потрібні БД, рядок підключення або конфігурація застосунку.

## Збирання

З кореня репозиторію:

```bash
dotnet build src/CommentsService.Api -c Release
npm --prefix src/comments-web ci
npm --prefix src/comments-web run build
```

## Запуск початкового каркаса

У першому терміналі запустіть API:

```bash
dotnet run --project src/CommentsService.Api --no-launch-profile --urls http://127.0.0.1:5081
```

У другому терміналі:

```bash
npm --prefix src/comments-web run dev
```

Відкрийте http://localhost:5174 на машині, де працює застосунок. Стартова сторінка
показує стан з’єднання з API. Сервер frontend перенаправляє `/api` на backend
із портом 5081. Для зупинки процесу натисніть Ctrl+C у його терміналі.

`GET http://127.0.0.1:5081/api/status` повертає:

```json
{"application":"CommentsService","version":"1.1.0.1","stage":"Init"}
```

Docker і постійна БД належать до наступної ітерації. Кожен наступний випуск
отримає актуальну інструкцію запуску. Реалізовані можливості фіксуються в change.log.ua.
