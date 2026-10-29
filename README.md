# CommentsService

Version: **1.1.0.1** · Branch: `CommentsService/v_1.1.0.1_Init`

[English](README.md) · [Русский](README.RU.md) · [Українська](README.UA.md)

[Change log](change.log) · [read.me](read.me) · [Development plan](docs/PLAN.md)

CommentsService will provide comments and threaded discussions. This initial release
contains only a React start page and a minimal .NET API. Comment submission, storage,
database migrations, Docker packaging and attachments are not implemented yet.

The interface is in English. Multilingual messages are planned for the comment form.
This is an initial preview, not a completed service for public hosting.

## Requirements

- .NET SDK 10, stable release.
- Node.js 24 with npm.
- Git to obtain the source.

No database, connection string or application configuration is required for Init.

## Build

From the repository root:

```bash
dotnet build src/CommentsService.Api -c Release
npm --prefix src/comments-web ci
npm --prefix src/comments-web run build
```

## Run the initial preview

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

