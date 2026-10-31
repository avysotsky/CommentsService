# CommentsService

Version: **1.2.2.4** · `CommentsService/v_1.2.2.4_LiveComments`

Junior+ integrates post-commit Events, a bounded read Cache, a coalescing notification Queue and same-origin WebSocket updates. New comments/replies show a localized refresh notice; reconnect re-reads data while preserving drafts, page and sort. Additive Cache/Notifications/Realtime settings support old config files. PostgreSQL remains authoritative; notifications/cache are process-local, not durable or multi-replica. No schema migration, new broker, NoSQL or cloud dependency.

Passed:337 backend tests (27/85/67/158),31 frontend tests, frontend/Docker builds, real Edge multi-window root/reply/CAPTCHA/refresh/reconnect/draft/sort/EN-RU-UA/360px,18 retained-million cases and public HTTPS/WSS/reconnect/45s heartbeat. Original data/schema/checkpoints/volumes preserved. Host reboot was not repeated.

[Junior+](docs/JUNIOR_PLUS.md)

<!-- JUNIOR-PLUS-CURRENT-END -->

Structured console/file JSON logging is configured through `Runtime.Logging`, with InstanceId, TraceId, level/category filters and retained rolling files. Docker preserves logs in a separate volume. [Logging settings and data policy](docs/LOGGING.md).

Backend `config.json` supports validated per-instance identity, database connection settings and health port; environment/CLI overrides and PG variables remain supported. Explicit CLI settings take precedence over PG variables. [Configuration and Docker mounts](docs/CONFIGURATION.md).

DTO contracts are grouped in the owning Application/API layers, one type per file. Explicit comment mapping is separate; JSON, routes and database schema are unchanged. The original acceptance report describes the preceding 1.1.0.15 release.

[English](README.md) · [Русский](README.RU.md) · [Українська](README.UA.md)

[Change log](change.log) · [Quick reference](read.me) · [Acceptance report](docs/ACCEPTANCE.md) · [Plan](docs/PLAN.md)

A .NET 10 / React SPA with PostgreSQL 17, packaged with Docker. Domain, Application, Infrastructure and API follow Onion Architecture, OOP and SOLID.

## Features

- Comments and replies at any thread depth; 25 roots per page, newest first by default; username/e-mail/date sorting in both directions.
- Required Latin-alphanumeric user name, e-mail, text and image CAPTCHA; optional full HTTP(S) homepage. Submitted e-mail is public; an author is not an authenticated account.
- Safe XHTML: `a` (`href`, `title`), `i`, `strong`, `code`; formatting toolbar, nesting checks and asynchronous preview. Drafts remain after failures.
- One optional image **or** TXT. JPG/JPEG/GIF/PNG: maximum 5 MiB, 16 million decoded pixels and 10,000 pixels per side; proportional fit within 320×240, no upscale, EXIF orientation, first GIF frame, canonical PNG without original metadata. TXT: 1–102400 bytes, strict UTF-8; BOM/Unicode/CRLF/tabs remain exact.
- Image/TXT modal viewer, effects, arrows/Escape, focus management, exact downloads and reduced motion. EN/RU/UA, mobile stacked rows and mixed RTL text.
- Server/client validation, parameterized EF queries, bounded requests, CSP, safe file headers and generic errors. [Security scope](docs/SECURITY.md).

## First installation

Requires Docker Engine, Compose v2.24+ and Python 3. Docker builds the application; host .NET/Node are needed only for host-side tests. Run from the repository root:

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

Open [localhost:8080](http://localhost:8080/) on the Docker host. Use sudo for Docker where needed. Credentials are generated once in the private `.env`; never publish it or overwrite credentials for existing volumes. To select unused ports **before first generation**, use `python3 scripts/init-env.py --port 8081 --test-db-port 55435`. An existing environment file is preserved, including its ports.

Keep the same Compose project name and environment file on updates. Development uses `commentsservice_development_data`; tests use the separate `commentsservice_test_data`. Both contain persistent PostgreSQL data. The app uses the non-superuser `comments_app` role. Startup never applies migrations automatically.

## Updates and recovery

1. For code-only updates, rebuild and restart the app with the same environment, project and volumes. Version 1.1.1.16 has no new migration or dependency.
2. Before schema/data-changing updates, stop every writer, run `scripts/db-checkpoint.py`, then explicitly migrate every used database. Add `--include-benchmark` if the retained benchmark database exists. Restart with the same volumes.
3. Retain one restore-verified previous checkpoint and the current databases. Never reset shared data for routine tests or remove persistent volumes. Stop services with `docker compose -p commentsservice --profile testing down` when needed; do not delete their volumes.
4. Verify an existing checkpoint without replacing it:

```bash
python3 scripts/verify-checkpoint.py artifacts/checkpoints/database.dump --upgrade
```

This restores only into a uniquely named temporary database, checks manifest/SHA-256 and row fingerprints, optionally applies the current idempotent SQL export, then removes only its own temporary database. It never switches the application database. A restore returns checkpoint-time data; later writes are absent. Read [migration/recovery details](docs/database/MIGRATIONS.md).

All four migrations remain current:

- `20261029195935_InitialDataModel`
- `20261030101720_CommentThreads`
- `20261030122207_ImageAttachments`
- `20261030130243_TextAttachments`

## Verification

Host tests need .NET 10 SDK and Node.js 24; install frontend dependencies with `npm --prefix src/comments-web ci` if absent. Keep the testing profile running.

```bash
python3 scripts/db-command.py check
python3 scripts/db-command.py check --service test-database
python3 scripts/verify-schema.py
python3 scripts/verify-schema.py --service test-database --database comments_test
python3 scripts/test-backend.py
npm --prefix src/comments-web test
npm --prefix src/comments-web run build
```

Database tests roll back; HTTP fixtures remove only their own rows. `verify-schema.py` compares the live PostgreSQL catalog with the shipped Workbench projection; it does not certify GUI import. Deployment/checkpoint/schema commands accept `--env-file`, `--project` and repeated `--compose-file`; use the same options on every command. Backend tests additionally accept `--host`/`--port`.

In Visual Studio 2026 open `CommentsService.slnx`; it contains backend/test projects and frontend source items. Solution builds backend/tests; Docker builds the complete app. Windows/Linux build outputs are separate.

## Runtime and acceptance

`GET /api/status` returns `{"application":"CommentsService","version":"1.1.1.16","stage":"Deployment"}`. `/health` checks the process; `/health/database` checks connectivity and current migrations (503 when unavailable/outdated). [API contract](docs/API.md).

JSON create/preview requires UTF-8, exact camelCase fields and at most 1 MiB, including chunked bodies. Unknown/duplicate/case-variant fields and invalid input return 400; oversized JSON 413; unsupported media/charset 415. CAPTCHA is process-local, expires after five minutes and is consumed by a verification attempt. Refresh after uncertain/failed submission. File bytes are stored transactionally in PostgreSQL, included in its backups, and omitted from list DTOs.

Automated acceptance passed: 268 backend and 26 frontend tests; frontend/Docker builds; all four migration stages and downgrade guards; restore/upgrade of three preserved checkpoints; independent empty installation; least-privilege roles; current-data recovery with 7 exact attachments and container recreation; browser viewers/RTL/security/pagination; 18 retained-million paging scenarios. Original databases, corpus, volumes, environment and checkpoint hashes remained unchanged.

Current limits, verified behavior and remaining manual-tool caveats: [RuntimeLimits](docs/RUNTIME_LIMITS.md).

[PostgreSQL schema / Workbench projection](docs/database/SCHEMA.md) · [Architecture](docs/ARCHITECTURE.md) · [Benchmark](docs/BENCHMARK.md)
