# GraphQL - 1.3.3.1

## English

This release implements the Graph choice of the Middle task with Hot Chocolate 16.7.3. It does not complete all Middle requirements. GraphQL runs over HTTP and offers field selection and combined reads; it does not inherently reduce SQL work or outperform REST. The current UI had no critical REST limitation. Only root-page reading in the SPA now uses GraphQL; replies, CAPTCHA, creation, preview and file downloads retain REST. `Runtime.GraphQL.Enabled=false` disables the endpoint and makes the SPA use its previous REST page request after configuration reload.

`POST /api/graphql` accepts one UTF-8 JSON object with `query`, optional object/null `variables` and optional `operationName`. Duplicate/unknown envelope fields, arrays, variable batches, extensions/APQ and blank queries are rejected. GET/HEAD/other methods return 405; disabled returns 404; unsupported media/charset returns 415; oversized actual bytes (including chunked requests) return 413; exhausted concurrency returns 429. Introspection through POST describes only public types and shares the budgets; no web IDE, SDL download, upload, mutation, subscription or GraphQL WebSocket endpoint is enabled. Syntax, argument, complexity and execution errors use the GraphQL `errors` envelope; storage failures expose `STORAGE_UNAVAILABLE` with no provider details. Clients must check `errors` even on HTTP 200. The SPA rejects partial/absent page data and retains previous results/drafts for retry; cancellation is propagated.

Queries: `rootPage(page: Int = 1, sort: String = "createdAt", direction: String = "desc")`, `comment(id: UUID!)`, `replies(parentId: UUID!, limit: Int, after: UUID)`. Page size and reply bounds come from existing Application policies; sorts remain userName/email/createdAt, asc/desc. Missing comment is null; invalid page, sort, reply target/cursor/limit produces an error. These are shallow public objects: there is no recursive per-comment replies resolver. Combined reads use explicit root fields/aliases, bounded by cost and parser limits. Each root uses existing Application use cases; DTOs belong to API, and Domain/Application contain no GraphQL dependency. Attachment objects contain metadata only. The existing SQL projection is reused regardless of selected fields, so fewer response fields do not imply fewer columns fetched. A page uses the existing page query, not a query per row. Combined roots need not share one database snapshot.

Post-commit Events still invalidate the same process-local read cache. Queue and existing WebSocket hints are unchanged; browser refresh/reconnect reloads the page through GraphQL. No broker, NoSQL, cloud, shared CAPTCHA, durability or replica consistency is introduced. No PostgreSQL schema/data migration is required. Upgrade with your existing Compose project, environment, overrides and volumes, rebuild/restart the app only. Old JSON files use compatible GraphQL defaults. Rollback uses the previous app image/config and a browser reload; original data and schema remain compatible. Never restore an older database merely to roll back this code.

## Русский

Выпуск реализует пункт Graph уровня Middle через Hot Chocolate 16.7.3; остальные пункты Middle ещё не выполнены. GraphQL работает поверх HTTP, позволяет выбирать поля и объединять чтения, но сам по себе не уменьшает SQL-работу и не обещает ускорение. Критического ограничения REST в нынешнем UI не было. Только чтение корневой страницы SPA переведено на GraphQL; ответы, CAPTCHA, запись, preview и файлы используют REST. `Runtime.GraphQL.Enabled=false` отключает endpoint и возвращает SPA к прежнему чтению страницы после повторной загрузки конфигурации.

`POST /api/graphql` принимает один UTF-8 JSON-объект: `query`, необязательные object/null `variables` и `operationName`. Дубли/неизвестные поля, массивы, пакеты переменных, extensions/APQ и пустые запросы запрещены. Другие методы дают 405, отключение 404, MIME/charset 415, превышение фактических байтов (включая chunked) 413, исчерпание одновременных запросов 429. POST introspection описывает только публичные типы и ограничивается бюджетами. IDE, скачивание SDL, загрузки, mutations/subscriptions и GraphQL WS отключены. Ошибки синтаксиса, аргументов, сложности и исполнения передаются в `errors`; хранилище даёт безопасный `STORAGE_UNAVAILABLE`. Проверять `errors` необходимо и при HTTP 200. SPA отвергает частичные/отсутствующие данные страницы, сохраняет предыдущие результаты и черновик для повтора; отмена передаётся серверу.

Операции: `rootPage(page,sort,direction)`, `comment(id)` и `replies(parentId,limit,after)`; точные типы и значения выше. Пагинация, шесть сортировок и лимиты ответов берутся из Application. Отсутствующий комментарий даёт null; неверные аргументы/родитель/cursor дают ошибку. Рекурсивного resolver ответов у каждого комментария нет. Комбинации задаются корневыми полями/алиасами и ограничены стоимостью и парсером. DTO принадлежат API; ядро не зависит от GraphQL. Вложения содержат только метаданные. Выбор полей не меняет существующую SQL-проекцию. Страница не делает отдельный SQL на каждую строку; разные корневые чтения не обещают общий снимок БД.

Events сбрасывают прежний кеш после сохранения. Queue/WS прежние, обновление/переподключение SPA читает страницу через GraphQL. Брокер, NoSQL, Cloud, общая CAPTCHA, долговечность и согласованность реплик не добавлены. Миграции PostgreSQL нет. Обновлять только app с прежними Compose-проектом, окружением, overrides и томами. Старые JSON получают совместимые значения GraphQL. Откат - прежний образ/конфиг и перезагрузка страницы; схема и данные совместимы. Откат кода не требует восстановления старой БД.

## Українська

Випуск реалізує пункт Graph рівня Middle через Hot Chocolate 16.7.3; решту пунктів Middle ще не виконано. GraphQL працює поверх HTTP, дозволяє вибирати поля й поєднувати читання, але сам по собі не зменшує SQL-роботу й не обіцяє прискорення. Критичного обмеження REST у поточному UI не було. Лише читання кореневої сторінки SPA переведено на GraphQL; відповіді, CAPTCHA, запис, preview та файли використовують REST. `Runtime.GraphQL.Enabled=false` вимикає endpoint і повертає SPA до попереднього читання сторінки після повторного завантаження конфігурації.

`POST /api/graphql` приймає один UTF-8 JSON-об'єкт: `query`, необов'язкові object/null `variables` та `operationName`. Дублікати/невідомі поля, масиви, пакети змінних, extensions/APQ та порожні запити заборонені. Інші методи дають 405, вимкнення 404, MIME/charset 415, перевищення фактичних байтів (включно з chunked) 413, вичерпання одночасних запитів 429. POST introspection описує лише публічні типи й обмежується бюджетами. IDE, завантаження SDL, файлів, mutations/subscriptions і GraphQL WS вимкнені. Помилки синтаксису, аргументів, складності й виконання передаються в `errors`; сховище дає безпечний `STORAGE_UNAVAILABLE`. Перевіряти `errors` потрібно і за HTTP 200. SPA відхиляє часткові/відсутні дані сторінки, зберігає попередні результати й чернетку для повтору; скасування передається серверу.

Операції: `rootPage(page,sort,direction)`, `comment(id)` та `replies(parentId,limit,after)`; точні типи й значення вище. Пагінація, шість сортувань і межі відповідей беруться з Application. Відсутній коментар дає null; хибні аргументи/батько/cursor дають помилку. Рекурсивного resolver відповідей кожного коментаря немає. Комбінації задаються кореневими полями/аліасами й обмежені вартістю та парсером. DTO належать API; ядро не залежить від GraphQL. Вкладення містять лише метадані. Вибір полів не змінює попередню SQL-проєкцію. Сторінка не робить окремий SQL на кожен рядок; різні кореневі читання не обіцяють спільного знімка БД.

Events скидають попередній кеш після збереження. Queue/WS попередні, оновлення/перепідключення SPA читає сторінку через GraphQL. Брокер, NoSQL, Cloud, спільна CAPTCHA, довговічність та узгодженість реплік не додані. Міграції PostgreSQL немає. Оновлювати лише app з попередніми Compose-проєктом, оточенням, overrides і томами. Старі JSON отримують сумісні значення GraphQL. Відкат - попередній образ/конфіг і перезавантаження сторінки; схема й дані сумісні. Відкат коду не потребує відновлення старої БД.

## Configuration / Настройки / Налаштування

All keys are under `Runtime.GraphQL`; defaults and accepted ranges / значения и диапазоны / значення й діапазони:

| Key | Default | Range |
| --- | --- | --- |
| Enabled | true | boolean |
| MaximumRequestBytes | 32768 | 256..1048576; also bounded by Runtime.Requests.JsonMaxBytes |
| MaximumDepth | 8 | 2..16 |
| MaximumFields | 256 | 16..1024 |
| MaximumFieldCost | 1000 | 1..10000 |
| MaximumTypeCost | 2000 | 1..100000 |
| ExecutionTimeoutSeconds | 10 | 1..30 |
| MaximumConcurrentRequests | 16 | 1..64 |

EN: Parser ceilings additionally bound recursion32/nodes2048/tokens4096. Parsed/compiled/cost caches each hold64 entries; cost estimates conservatively use max(RootPageSize, MaximumListLimit) as list size. Only `graphQLEnabled` is added to the public allowlist. Restart to apply changes. Execution deadlines are cooperative; SQL timeouts and request cancellation remain in force.

RU: Дополнительно парсер ограничен recursion32/nodes2048/tokens4096. Кеши документов/операций/стоимости - по64 записи; размер списка для оценки стоимости консервативно равен max(RootPageSize, MaximumListLimit). В публичный список добавлен только `graphQLEnabled`. Изменения требуют перезапуска. Срок исполнения кооперативный; SQL-таймауты и отмена запроса сохраняются.

UA: Додатково парсер обмежено recursion32/nodes2048/tokens4096. Кеші документів/операцій/вартості - по64 записи; розмір списку для оцінки вартості консервативно дорівнює max(RootPageSize, MaximumListLimit). До публічного списку додано лише `graphQLEnabled`. Зміни потребують перезапуску. Строк виконання кооперативний; SQL-таймаути та скасування запиту зберігаються.

## Example / Пример / Приклад

```graphql
query PageAndReplies($id: UUID!) {
  rootPage(page: 1, sort: "createdAt", direction: "desc") {
    page pageSize totalCount
    items { id userName text image { id fileName width height } }
  }
  replies(parentId: $id, limit: 10) {
    items { id text }
    nextAfter
  }
}
```

Send the query string in JSON with `variables: {"id":"<existing-comment-uuid>"}` / Передать query в JSON с variables / Передати query в JSON зі variables.

Sources: [Hot Chocolate package](https://www.nuget.org/packages/HotChocolate.AspNetCore/16.7.3), [request limits](https://chillicream.com/docs/hotchocolate/security/request-limits), [cost analysis](https://chillicream.com/docs/hotchocolate/security/cost-analysis), [HTTP options](https://chillicream.com/docs/hotchocolate/server/options).

## Verification / Проверки / Перевірки

EN: Passed: 370 distinct backend tests (27 Domain / 85 Application / 67 Data / 191 API, including the final focused rerun), 33 frontend tests, frontend/Docker builds, real Edge with three windows/CAPTCHA/root/reply/WS refresh/reconnect/draft/query retention/EN-RU-UA/360px, and 18 retained-million GraphQL cases with two samples and REST order parity. Native HTTP byte/encoding/depth/route guards and public HTTPS/GraphQL/WSS checks passed. Original data, schema, checkpoints, environment, volumes and existing preview were preserved. No reboot or repeat of the prior heartbeat acceptance.

RU: Прошли 370 различных backend-тестов (27 Domain / 85 Application / 67 Data / 191 API, включая финальный целевой повтор), 33 frontend-теста, сборки frontend/Docker, Edge с тремя окнами/CAPTCHA/корнем/ответом/WS-обновлением/переподключением/сохранением черновика и запроса/EN-RU-UA/360px и 18 сценариев GraphQL сохранённого миллиона по два замера с паритетом порядка REST. Прошли реальные HTTP-ограничения байтов/кодировки/глубины/вариантов маршрута и публичные HTTPS/GraphQL/WSS. Исходные данные, схема, копии, окружение, тома и прежний preview сохранены. Reboot и прежняя приёмка heartbeat не повторялись.

UA: Пройшли 370 різних backend-тестів (27 Domain / 85 Application / 67 Data / 191 API, включно з фінальним цільовим повтором), 33 frontend-тести, збирання frontend/Docker, Edge з трьома вікнами/CAPTCHA/коренем/відповіддю/WS-оновленням/перепідключенням/збереженням чернетки й запиту/EN-RU-UA/360px та 18 сценаріїв GraphQL збереженого мільйона по два вимірювання з паритетом порядку REST. Пройшли справжні HTTP-межі байтів/кодування/глибини/варіантів маршруту та публічні HTTPS/GraphQL/WSS. Початкові дані, схема, копії, оточення, томи й попередній preview збережено. Reboot і попереднє приймання heartbeat не повторювались.
