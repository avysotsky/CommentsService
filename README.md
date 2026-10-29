# CommentsService

Version: **1.1.0.2** · Branch: `CommentsService/v_1.1.0.2_VisualStudio`

[English](README.md) · [Русский](README.RU.md) · [Українська](README.UA.md)

[Change log](change.log) · [Quick reference](read.me) · [Plan](docs/PLAN.md)

This release runs the React frontend and .NET API together in one Docker container.
A dedicated PostgreSQL database uses persistent storage. An optional, separate
PostgreSQL service provides persistent integration-test storage.

Comments, Entity Framework mappings and application database access are not
implemented yet; the next iteration is DataModel. The English start page displays
the API connection state and release. This is a development milestone, not the completed comments service.

## Deploy on your own infrastructure

Requires Docker Engine, Docker Compose v2.24+, Git and Python 3 for generating
credentials. Docker builds the .NET and React components; no host .NET/Node
installation is needed. From the cloned repository root:

```bash
python3 scripts/init-env.py
docker compose -p commentsservice up -d --build --wait
curl --fail http://127.0.0.1:8080/health
```

Open http://localhost:8080 on the Docker host. Use sudo for Docker commands if
required by your installation. The first start initializes the empty database
volume; later starts retain it. `--wait` waits for the container health checks.

`commentsservice` is an example project name. Choose your own if needed and keep
it unchanged across updates so the same data volumes are reused. Choose another
available application port when generating the environment file, for example
`python3 scripts/init-env.py --port 8081`.

## Persistent databases

- `database`: database `comments`, volume `commentsservice_development_data`.
- `test-database`: database `comments_test`, volume `commentsservice_test_data`;
  available through the optional `testing` profile.
- Each service has a separate password and a non-superuser `comments_app` role.
- No application tables or EF migrations exist in this milestone.

To start the persistent test database as well:

```bash
docker compose -p commentsservice --profile testing up -d --wait test-database
```

The test database binds only to 127.0.0.1:55434. Override this port with
`--test-db-port` when generating credentials. The main database port is not
published. Tests must preserve their database, roll back changes or remove only
their own records. Do not recreate databases or truncate shared tables for routine tests.

Stop containers while retaining data:

```bash
docker compose -p commentsservice --profile testing down
```

Do not use `down -v` if the data must be retained. To update, select the desired
release branch and repeat the startup command with the same project name and environment file.
Before future schema/data changes, keep a verified previous recovery point.

## Credentials and access

The initializer creates a private .env with random credentials once; it does not
overwrite an existing file. Do not commit or share that file. If an existing file
is incomplete, generate a separate one using `--output` and pass it to Compose
with `--env-file`. Changing passwords in .env does not change existing PostgreSQL roles.

The app port binds to 127.0.0.1. Connect your HTTPS reverse proxy for external
access; this release does not provision a domain or public hosting.

## Status endpoints

`GET /api/status` returns:

```json
{"application":"CommentsService","version":"1.1.0.2","stage":"Docker"}
```

`GET /health` reports API process readiness. PostgreSQL readiness is checked
separately by Compose; the application does not connect to the databases yet.
The app runs as an unprivileged container user. Dockerfiles and initialization
scripts contain no embedded credentials or workstation-specific instructions.

## Open in Visual Studio

Open `CommentsService.slnx` in Visual Studio 2026 with the ASP.NET and web development workload and .NET 10 SDK installed. The solution includes the API project and a Frontend solution folder with the React source and build files. The frontend remains an npm/Vite application; building the solution builds the API only. Use the Docker instructions above to build and run the complete application.
