# CommentsService

Version: **1.1.0.12** · Branch: `CommentsService/v_1.1.0.12_FileViewer`

[English](README.md) · [Русский](README.RU.md) · [Українська](README.UA.md)

[Change log](change.log) · [Quick reference](read.me) · [Plan](docs/PLAN.md)

FileViewer (1.1.0.12): click an image or TXT filename to open a modal with fade/scale effects. Previous/Next and Left/Right cycle through attachments in currently visible comments, including expanded replies; hidden/unloaded replies and other pages are not fetched. Escape, Close viewer or the backdrop closes it. Focus stays inside and returns to the opener; background scrolling is locked. Reduced-motion preferences disable animations. Downloads preserve the stored normalized PNG or the original TXT bytes, using generated UUID filenames. TXT is decoded as strict UTF-8 and rendered as literal text, never HTML; a leading BOM is not displayed. Loading, failure and Retry states preserve the comment draft. Requests are bounded and canceled on navigation/close; temporary image URLs are revoked.

`GET /api/images/{id}?download=true` adds attachment disposition; without it the existing inline PNG route is unchanged. TXT continues to use `/api/text-files/{id}` with its safe headers and exact bytes. FileClient separates the viewer from HTTP; backend Domain/Application/Infrastructure boundaries and Onion/OOP/SOLID remain intact. No new dependency or migration. Upgrade 1.1.0.11 → 1.1.0.12 by rebuilding/restarting the app with the same environment, Compose overrides and volumes; retain existing databases and verified checkpoints. Latest migration remains `20261030130243_TextAttachments`.

Validation for 1.1.0.12: 235 backend tests (27/84/56/68), 23 frontend tests, frontend/Docker builds and Edge desktop/360px passed. Browser coverage includes real CAPTCHA-backed image/TXT/reply fixtures, 100 KiB, literal Unicode/HTML, exact downloads, arrows/wrap, close paths, focus, draft retention, 404/503/retry, stale requests, URL cleanup, EN/RU/UA, reduced motion and reload. All 18 paging scenarios passed on the retained million-comment corpus without reseeding. All three databases, volumes and checkpoint hashes were preserved. No migration/restore rehearsal was needed for this code-only change. VS/Workbench GUI was not checked. Next: 1.1.0.13 ResponsiveUi.

TXT attachments (1.1.0.11): roots and replies accept one optional attachment: an image OR a `.txt` file. TXT must contain 1..102400 bytes (100 KiB), strict UTF-8; BOM, Unicode, CR/LF and tabs are preserved byte-for-byte. Invalid encoding and control characters other than CR/LF/tab are rejected; HTML/script text is accepted as literal text. Filename/MIME do not establish content validity. Multipart uses `textFile` with the usual comment/CAPTCHA fields; never send both file parts. JSON text-only creation remains available. Invalid TXT returns 400/errors.textFile. CAPTCHA is consumed before content processing; retry with a fresh code. The localized picker keeps draft/file on errors, clears on success and permits removing/replacing a selection.

DTOs add nullable `textFile: {id,fileName,sizeBytes}`; list/page/reply projections omit file bytes. `GET /api/text-files/{id}` downloads exact bytes as `text/plain; charset=utf-8`, generated UUID `.txt` filename, attachment disposition, nosniff, no-store and sandbox CSP; missing files return 404. HTML is never rendered. FileViewer is available in 1.1.0.12.

Upgrade 1.1.0.10 → 1.1.0.11: stop all writers, restore-verify a current checkpoint (include the benchmark when present), build, explicitly migrate every used database, then restart using the existing volumes. Migration `20261030130243_TextAttachments` adds TextAttachments: Id, unique CommentId FK CASCADE, sanitized FileName varchar128, SizeBytes, Data bytea; checks enforce 1..102400 bytes and matching size. Existing authors/comments/images and migration IDs are preserved. Down refuses while TXT rows exist. Bytes and comment are committed atomically in the same PostgreSQL database/backup; no filesystem volume. Preserve verified pre-upgrade copies, accounting for changes after their checkpoint.

Onion: Domain enforces immutable ownership/defensive bytes and one attachment per comment; Application defines processing/read ports and coordinates the transaction; Infrastructure validates UTF-8 and implements EF storage/queries; API binds streams and sets download headers. No new external dependency.

TextAttachments 1.1.0.11 verification: 235 backend tests (27 Domain,84 Application,56 Infrastructure/database,68 HTTP),16 frontend tests, frontend/Docker builds, restore upgrade/downgrade round trip and populated-TXT downgrade guard passed. Edge desktop/360px passed real CAPTCHA, TXT root/reply, boundary size, exact Unicode/BOM/CRLF/HTML download, restart persistence, safe headers, rejection/draft retention/removal, exclusive selection and EN/RU/UA without JavaScript errors. All 18 retained-million-corpus paging scenarios passed without reseeding; original row fingerprints, metadata, volumes and restore-verified checkpoints were retained. GUI import in VS/Workbench is not checked.


React and .NET 10 run in one Docker container with persistent PostgreSQL 17.
This release creates and reads text/XHTML comments through the API, validates input,
and captures server-side client metadata. Onion boundaries isolate Domain/Application
from EF and HTTP. The SPA provides a form, latest-comments table, client validation,
async submission, draft retention on failure and EN/RU/UA interface selection.
TXT attachments and the file viewer are implemented.

Upgrade 1.1.0.9 → 1.1.0.10 requires the image migration and a restore-verified current checkpoint. See Image attachments below.

[API contract and examples](docs/API.md) · [Architecture](docs/ARCHITECTURE.md)

## Deploy on your own infrastructure

Requires Docker Engine, Compose v2.24+, Git and Python 3. Docker builds .NET/React;
host SDK/Node installations are not required for deployment. From the repository root:

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

Open http://localhost:8080 on the Docker host. Use sudo for Docker if required.
The example project name is `commentsservice`; keep it and the environment file
unchanged across updates to reuse the same volumes. An alternative app port can
be selected once with `python3 scripts/init-env.py --port 8081`.

The test database is a separate persistent service under the `testing` profile.
The checkpoint command verifies both databases, so start both before running it.
Migrations are explicit; application startup never applies them automatically.
Before schema/data-changing upgrades, stop writes and verify a checkpoint before migration.
The 1.1.0.6 → 1.1.0.7 upgrade added migration `20261030101720_CommentThreads`.
Stop the app (`docker compose -p commentsservice stop app`), verify a current checkpoint,
build the release, migrate both databases explicitly, then restart with the same volumes.
Existing comments retain all original fields and become roots (`parentId=null`, `rootId=id`).
The reverse migration refuses to remove thread metadata while replies exist; restore a
verified pre-upgrade checkpoint if necessary, accounting for changes since that checkpoint.

## Data and recovery

- Development: database `comments`, volume `commentsservice_development_data`.
- Tests: `comments_test`, volume `commentsservice_test_data`, loopback port 55434.
- Application role: `comments_app`, without superuser, role or database creation privileges.
- Tables: `comments.Authors`, `comments.Comments`; history: `comments.__EFMigrationsHistory`.
- Migrations: `20261029195935_InitialDataModel`, `20261030101720_CommentThreads`, `20261030122207_ImageAttachments` (additive).
- Checkpoints: `artifacts/checkpoints`, one verified previous dump/manifest per database.
  The script restores into uniquely named temporary databases, compares row fingerprints,
  then removes only those temporary databases. Existing checkpoints are replaced after verification.

Keep checkpoint files private and copy them to protected storage for important data.
Quiesce writers during a checkpoint; changes detected during verification cause failure.
Routine startup/tests retain databases and volumes. Never use `down -v` or truncate shared tables.
See [schema and diagrams](docs/database/SCHEMA.md) and [migration/recovery details](docs/database/MIGRATIONS.md).

## Verification

```bash
python3 scripts/db-command.py check
python3 scripts/db-command.py check --service test-database
python3 scripts/test-backend.py
```

The four test suites require a host .NET 10 SDK. Tests read the generated credential
file and use `comments_test`. Database tests roll back; HTTP tests delete only their
own comments and unreferenced author snapshots.
Override `--host`/`--port` on the test command if the database is on another host.
Database command/checkpoint scripts support `--env-file`, `--project` and repeated
`--compose-file` options. Use matching options on every deployment command.
The credential reader expects the literal KEY=value format generated by `init-env.py`.

`GET /api/status` returns `{"application":"CommentsService","version":"1.1.0.10","stage":"ImageAttachments"}`.
`GET /health` checks the API process; `/health/database` checks connectivity and pending
migrations, returning 503 when unavailable or outdated. Unknown API routes return 404; comments routes are documented in docs/API.md.

## Frontend verification

With Node.js 24, run `npm ci`, `npm test` and `npm run build` in `src/comments-web`.
The form submits plain text or allowed XHTML; required fields are marked. Emails are public. The table
shows 25 roots per page with local timestamps. Refresh retries failed reads.
Server field errors are displayed as returned; the interface supports EN/RU/UA.
A failed submission keeps the draft in the form; refresh before retrying an uncertain
request to avoid duplicates. CAPTCHA and image attachments are implemented; TextAttachments stores optional TXT files.

## Credentials, stop and IDE

`init-env.py` generates a private `.env` once and preserves existing files. Do not
publish credentials or checkpoints. Changing .env does not update existing database passwords.
The app binds to loopback; configure your own HTTPS reverse proxy for external access.
Stop containers while retaining data:

```bash
docker compose -p commentsservice --profile testing down
```

Open `CommentsService.slnx` in Visual Studio 2026 with the ASP.NET/web workload and
.NET 10 SDK. The solution builds API and tests; Frontend holds editable React files.
Docker builds the complete app. Windows and Linux intermediate outputs are separated.
Without database variables the API/status page still starts; database health returns 503.
To connect from the IDE, set `PGHOST`, `PGPORT`, `PGDATABASE`, `PGUSER`, `PGPASSWORD`
in your private launch environment. Never commit those values.

## Onion architecture

The backend has four projects: Domain, Application, Infrastructure and Api.
Application implements create/read use cases, validation and DTOs; EF adapters implement its persistence ports.
Infrastructure/Persistence contains Configurations, Repositories and Migrations.
HTTP endpoints use application/framework contracts; Infrastructure is connected in Program.cs.
See [architecture and dependency rules](docs/ARCHITECTURE.md). This release preserves
the initial migration and existing content; the thread migration extends the schema. The next milestone is 1.1.0.13 ResponsiveUi.

## XHTML editor

Allowed tags: `a`, `i`, `strong`, `code`. Links require an absolute HTTP(S) `href` without credentials; `title` is optional. Other attributes, namespaces, nested links, comments, DTD/CDATA, malformed tags and unsafe URLs are rejected. XHTML uses closed, correctly nested lowercase tags and XML entities (`&amp;`, `&lt;`, `&quot;`); plain text such as `1 < 2 && 3 > 1` still works. The limit is 65,536 UTF-16 units, depth 32 and 4096 parser nodes.

Select text and use Bold/Italic/Code/Link. Link inserts an editable HTTPS template. Preview calls `POST /api/comments/preview` with `{ "text": "<strong>Hello</strong>" }` and returns `{ "html": "<strong>Hello</strong>" }` without database access or writes. Saving uses the same Application validator. Editing hides stale previews; request failure preserves the draft. Preview and table render only allowlisted React elements, with safe links and escaped fallback text. Existing stored text is retained; the thread migration extends the schema.

The optional homepage field explains that a full address is required, for example `https://example.com`; `http://` is also supported. The form does not choose a protocol or check website availability.

## Comment threads

Replies can target any existing comment. Choose Reply beneath a comment; the form identifies the recipient. Cancel reply returns to root posting and preserves the draft. Successful replies appear under their parent; missing targets produce an error without creating an author or comment.

The root table shows 25 roots per page in the selected order. Show/Hide replies expands or collapses direct replies; More replies loads the next 25. Replies use descending UTC time and UUID; roots use the selected order. Expanding descendants has no application depth limit: rendering is iterative, and visual indentation stops growing after six levels while the numeric level continues. Refresh reloads roots and open branches; branch failures offer Retry and preserve loaded content. Full root pagination/sorting is implemented in this release.

API: POST /api/comments accepts optional parentId (UUID/null); rootId is derived by the server and cannot be supplied. GET /api/comments/{id}/replies?limit=25&after={replyId} returns {items,nextAfter}; omit after for the first page. Limit is 1–100; after must be a direct child of that parent. Missing parent is 404 for reads and a parentId validation error (400) for writes. Unknown/foreign cursors are 400. Pages limit direct siblings, not thread depth. No delete/edit API is introduced.

## Root sorting and pagination

The SPA loads `GET /api/comments/page?page=1&sort=createdAt&direction=desc`. Allowed sort fields: `userName`, `email`, `createdAt`; direction: `asc` or `desc`. Date descending is default LIFO. Each page contains **25 roots**, excluding replies. The response contains `items`, `page`, `pageSize`, `totalCount` (roots only), `totalPages`. Page must be an integer from 1 to 85,899,345; invalid fields/directions return 400. Beyond the last page, the API returns an empty items array with the real totals; the SPA corrects stale bookmarks to the last page, or page 1 for an empty list. An empty list has totalPages=0.

Sort controls offer all six combinations; sorting returns to page 1. First/Previous/Next/Last and page jump navigate; the address preserves page/sort/direction across reload. Drafts remain while browsing. A new root returns to page 1 with newest-first order; a reply retains its current page and does not change the root count. Replies keep their own Show/Hide/More controls. Failed refresh retains previous results and offers Refresh.

Equal sort values use UUID in the selected direction as a stable tie-breaker. Name/email use the database collation; dates use UTC. Count and rows share a repeatable-read snapshot within a request. Separate page requests are live: concurrent additions can shift offset boundaries. The compatibility route `GET /api/comments?limit=25` retains its bounded latest-root array response. Sorting itself adds no migration; image attachments add their migration in 1.1.0.10.

[Persistent million-row corpus and measurements](docs/BENCHMARK.md).

## CAPTCHA

Every comment/reply requires a fresh verification challenge. `GET /api/captcha` returns `{id,imageDataUrl,expiresAt}` with a raster PNG and no-store headers; the answer is never returned as text or metadata. Enter the six Latin letters/digits from the image, case-insensitively. Codes expire after **5 minutes**. `GET /api/captcha?replace={oldId}` invalidates that challenge and issues a new one.

POST /api/comments additionally requires `captchaId` (UUID) and `captchaCode`. A verification attempt consumes its ID, including a wrong answer; missing, expired, replaced or spent challenges return 400 with `errors.captchaCode` and never stage an author/comment. Other input is validated before verification; after successful verification, parent/storage errors still consume the challenge. Refresh after any uncertain or failed submission before retrying. Preview and read endpoints do not require CAPTCHA.

The form loads/refreshes the picture, blocks submission while loading/expired/unavailable, and preserves the draft after errors. It obtains a new challenge after every POST attempt. EN/RU/UA labels and errors are provided. The store is process-local, holds at most 2,048 challenges, reclaims expired entries, and returns 429 at capacity. Restarting/recreating the app invalidates outstanding challenges. Multiple app instances have separate challenges; use one instance for this release. The renderer uses portable .NET PNG primitives without platform graphics dependencies. Only answer hashes and expiry are retained in memory; no CAPTCHA data or migration is added to PostgreSQL.

## Image attachments

A root or reply can include **one optional JPG/JPEG, GIF or PNG**. Choose the image beside the comment; Remove image clears it. The source limit is 5 MiB, 16 million decoded pixels and 10,000 pixels per side. Images larger than 320×240 are reduced proportionally without cropping; small images are not enlarged. JPEG EXIF orientation is applied; GIF uses its first frame. Every accepted file is re-encoded as a static PNG, removing original metadata and trailing content. Transparency is preserved. These bounds are application choices; the assignment requires supported formats and the 320×240 result.

The server checks the extension against the detected codec, decodes the image and rejects corrupt/truncated data before staging any rows. Browser MIME and accept filters are not trusted. The normalized image and author/comment are committed by the same unit of work to PostgreSQL; no independent filesystem file or new Docker volume is needed. The image row contains bytes (at most 512 KiB), sanitized display filename, dimensions and a unique CommentId. Each comment has at most one image; deleting its owning comment cascades only that image. Parent-comment deletion remains restricted while replies exist. Existing comments receive no image automatically.

JSON POST /api/comments remains available for text-only writes. With an image, use multipart/form-data containing the same text/CAPTCHA fields and one file named `image`; let the browser/curl generate the boundary. Unknown/duplicate fields or multiple files are rejected. JSON retains its 1 MiB limit; multipart requests allow 6 MiB overall, with a 5 MiB file limit. Invalid images return 400 with errors.image; oversized request bodies can return 413. The CAPTCHA is consumed before image processing, so retry with a new code. Errors preserve the draft and selected file; successful submission clears them.

Comment DTOs include nullable `image: {id,fileName,width,height}`; list/page/reply projections never include the binary payload. `GET /api/images/{id}` serves only canonical PNG with image/png, inline generated filename and X-Content-Type-Options: nosniff, or 404. Thumbnails open the modal viewer implemented in 1.1.0.12. TXT attachments are implemented in 1.1.0.11. Processing uses [SkiaSharp 4.154.0](https://www.nuget.org/packages/SkiaSharp/4.154.0) and matching Linux native assets; notices are in [docs/third-party](docs/third-party/README.md) and the runtime image.

Upgrade 1.1.0.9 → 1.1.0.10: stop all writers, including any benchmark preview, restore-verify a current checkpoint (`--include-benchmark` when that DB exists), build the app, migrate development/tests and benchmark (`--benchmark`) explicitly, then restart with the same volumes. Migration `20261030122207_ImageAttachments` adds only the image table/index/checks/FK; original author/comment fields and IDs remain unchanged. Down refuses while image rows exist. Keep verified pre-upgrade copies for recovery; a restore loses changes made after its checkpoint. Backups automatically include image bytes because they reside in the same database.
