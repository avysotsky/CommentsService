# Deployment 1.3.3.4

## English

Current runtime:1.3.3.4/CloudAttachments. [Live demo](https://comments.alexvysotsky.com). Deploy the accepted Docker app with PostgreSQL17. GraphQL serves SPA root pages; RabbitMQ and Elasticsearch are optional private services, and Supabase is an optional external private mirror. Existing SQL/comment/file/CAPTCHA/preview/cache/queue/WS functionality remains independent of those providers. [Combined installation and final acceptance](MIDDLE_ACCEPTANCE.md).

Terminate HTTPS at a reverse proxy and forward the original Host plus overwritten trusted X-Forwarded-For/Proto. Configure Runtime.Proxy.KnownProxies with actual immediate peers; never trust arbitrary networks. Keep database, broker, search and origin ports off the public Internet. Use a valid certificate, HTTP-to-HTTPS redirect, body limit6MiB and no proxy cache for API responses. For /api/comments/live forward HTTP/1.1 Upgrade/Connection, disable buffering and allow heartbeat-compatible timeouts. Preserve existing hostnames/certificates when adding the application's virtual host. Never disable TLS verification.

Keep the same Compose project, private environment, all deployment overrides and persistent volumes on updates. Preserve SQL, checkpoints, credentials and derived search storage. Rebuild/recreate only app for application changes; do not use down -v or global prune. The MiddleAcceptance correction changes docs/read-only verifier only and needs no working app rebuild. Rollback uses the previous app image with the same environment/volumes; do not restore an older SQL checkpoint for a code-only rollback. [Recovery](database/MIGRATIONS.md).

Generate broker/search secrets using their Docker/.NET commands; app receives only scoped search credentials, never the Elasticsearch admin password. Keep Supabase keys only in backend runtime environment. Cloud consent/replay is empty unless an owner explicitly selects own test IDs; never bulk-upload existing user attachments. Free-only setup needs no paid upgrades or artificial keepalive. Account pause/quota/connection failures fall back to SQL. See [Cloud](CLOUD_ATTACHMENTS.md), [Broker](MESSAGE_BROKER.md), [Search](COMMENT_SEARCH.md) and [GraphQL](GRAPHQL.md).

Public HTTPS/HSTS/DB/GraphQL/search/WSS/reconnect are verified. Hosting availability depends on its operator and host/network uptime; this demonstration does not promise24/7 SLA. CAPTCHA is not authentication or a general rate limiter. Configure appropriate edge abuse controls for long-lived public hosting. Native Workbench's documented shutdown caveat does not change SQL schema parity. [Full acceptance and remaining scope limits](MIDDLE_ACCEPTANCE.md).

## Русский

Текущий runtime:1.3.3.4/CloudAttachments. [Демо](https://comments.alexvysotsky.com). Развёртываются Docker app и PostgreSQL17. GraphQL читает корневые страницы SPA; RabbitMQ/Elasticsearch — дополнительные приватные сервисы, Supabase — дополнительная внешняя приватная копия. Прежние SQL/комментарии/файлы/CAPTCHA/preview/кеш/очередь/WS не зависят от доступности провайдеров. [Совместная установка и финальная приёмка](MIDDLE_ACCEPTANCE.md).

HTTPS завершается на reverse proxy, передающем исходный Host и заменённые доверенные X-Forwarded-For/Proto. В Runtime.Proxy.KnownProxies указать фактические ближайшие узлы, не доверять произвольным сетям. Порты БД/брокера/поиска/origin не публиковать в интернет. Нужны действительный сертификат, редирект HTTP->HTTPS, лимит тела6МиБ и отсутствие proxy cache API. Для /api/comments/live передать HTTP/1.1 Upgrade/Connection, выключить buffering и задать таймауты для heartbeat. При добавлении своего virtual host сохранить существующие имена/сертификаты. Проверку TLS не отключать.

Обновлять с теми же Compose-проектом, приватным окружением, всеми overrides и постоянными томами. Сохранять SQL/копии/учётные данные/производное хранилище поиска. При изменении приложения пересобирать/пересоздавать только app, без down -v/global prune. Исправление MiddleAcceptance меняет только документы/проверку чтением и не требует пересборки рабочего app. Откат — предыдущий образ с прежними env/томами; не восстанавливать старую SQL ради отката кода. [Восстановление](database/MIGRATIONS.md).

Секреты брокера/поиска генерировать Docker/.NET-командами; app получает ограниченные поисковые права без admin-пароля ES. Supabase-ключ — только runtime backend. Списки Cloud разрешения/replay пусты, пока владелец явно не выберет свои тестовые ID; массовая отправка пользовательских вложений запрещена. Только Free, без оплаты и искусственного keepalive. Пауза/квота/сбой облака дают fallbackSQL. См.[Cloud](CLOUD_ATTACHMENTS.md), [брокер](MESSAGE_BROKER.md), [поиск](COMMENT_SEARCH.md), [GraphQL](GRAPHQL.md).

Публичные HTTPS/HSTS/БД/GraphQL/search/WSS/reconnect проверены. Доступность зависит от владельца хостинга, компьютера и сети; демо не обещает SLA24/7. CAPTCHA не заменяет аутентификацию/rate limiter. Для постоянного публичного хостинга настроить соответствующие edge-ограничения. Документированная оговорка завершения Workbench не меняет паритет SQL-схемы. [Полная приёмка и границы объёма](MIDDLE_ACCEPTANCE.md).

## Українська

Поточний runtime:1.3.3.4/CloudAttachments. [Демо](https://comments.alexvysotsky.com). Розгортаються Docker app і PostgreSQL17. GraphQL читає кореневі сторінки SPA; RabbitMQ/Elasticsearch — додаткові приватні сервіси, Supabase — додаткова зовнішня приватна копія. Попередні SQL/коментарі/файли/CAPTCHA/preview/кеш/черга/WS не залежать від доступності провайдерів. [Спільна інсталяція та фінальне приймання](MIDDLE_ACCEPTANCE.md).

HTTPS завершується на reverse proxy, що передає початковий Host та замінені довірені X-Forwarded-For/Proto. У Runtime.Proxy.KnownProxies указати фактичні найближчі вузли, не довіряти довільним мережам. Порти БД/брокера/пошуку/origin не публікувати в інтернет. Потрібні чинний сертифікат, редирект HTTP->HTTPS, межа тіла6МіБ і відсутність proxy cache API. Для /api/comments/live передати HTTP/1.1 Upgrade/Connection, вимкнути buffering і задати таймаути для heartbeat. Додаючи свій virtual host, зберегти попередні імена/сертифікати. Перевірку TLS не вимикати.

Оновлювати з тими самими Compose-проєктом, приватним оточенням, усіма overrides і постійними томами. Зберігати SQL/копії/облікові дані/похідне сховище пошуку. За зміни застосунку перебудовувати/перестворювати лише app, без down -v/global prune. Виправлення MiddleAcceptance змінює лише документи/перевірку читанням і не потребує перебудови робочого app. Відкат — попередній образ із попередніми env/томами; не відновлювати стару SQL заради відкату коду. [Відновлення](database/MIGRATIONS.md).

Секрети брокера/пошуку генерувати Docker/.NET-командами; app отримує обмежені пошукові права без admin-пароля ES. Supabase-ключ — лише runtime backend. Списки Cloud дозволу/replay порожні, доки власник явно не обере власні тестові ID; масове передавання користувацьких вкладень заборонене. Лише Free, без оплати й штучного keepalive. Пауза/квота/збій хмари дають fallbackSQL. Див.[Cloud](CLOUD_ATTACHMENTS.md), [брокер](MESSAGE_BROKER.md), [пошук](COMMENT_SEARCH.md), [GraphQL](GRAPHQL.md).

Публічні HTTPS/HSTS/БД/GraphQL/search/WSS/reconnect перевірені. Доступність залежить від власника хостингу, комп'ютера й мережі; демо не обіцяє SLA24/7. CAPTCHA не замінює автентифікацію/rate limiter. Для постійного публічного хостингу налаштувати відповідні edge-обмеження. Документоване застереження завершення Workbench не змінює паритет SQL-схеми. [Повне приймання й межі обсягу](MIDDLE_ACCEPTANCE.md).


EN: Deployment/maintenance no longer needs host Python or .NET SDK; [Docker/.NET tools](DOCKER_TOOLS.md). Runtime/app image remains unchanged for this tooling-only correction.

RU: Для установки/обслуживания больше не нужны Python/.NET SDK на хосте; [инструменты Docker/.NET](DOCKER_TOOLS.md). Рабочий образ app прежний: исправление касается только инструментов.

UA: Для інсталяції/обслуговування більше не потрібні Python/.NET SDK на хості; [інструменти Docker/.NET](DOCKER_TOOLS.md). Робочий образ app попередній: виправлення стосується лише інструментів.
