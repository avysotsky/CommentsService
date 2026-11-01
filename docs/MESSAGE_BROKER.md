# Message broker - 1.3.3.2

## English

The Middle broker item uses RabbitMQ 4.4.0 and RabbitMQ.Client 7.2.2. GraphQL and all earlier functionality remain. No NoSQL, cloud, outbox, replica deployment or Middle+ load architecture is included. RabbitMQ is optional and disabled in backend defaults/old JSON files. PostgreSQL remains authoritative; neither comment saving nor cache invalidation waits for a broker connection or confirmation. The public configuration allowlist has no broker fields or credentials.

After a successful database commit, Events immediately invalidate the existing cache. The broker router makes a synchronous, bounded, non-blocking enqueue when a subscription is ready. A background publisher sends a versioned envelope through the topic exchange; its exclusive temporary queue receives the notification, then forwards it to the existing coalescing local queue and WebSocket worker. An application event is not republished by the consumer. Network work never executes inside the post-commit subscriber.

When disabled/disconnected, the router immediately uses the old local notification path. Buffer overflow also uses that path. If a connection appears healthy but no own consumer receipt arrives within FallbackDelayMilliseconds, a separate100ms watchdog supplies the local hint; this covers a stalled/paused broker and lost consumer delivery, not just clean disconnects. The deadline is approximate and subject to process scheduling, not a hard SLA. Connection loss flushes pending local hints. Confirmations alone do not remove pending fallback entries; a consumer receipt does. Duplicate/late own receipts are ignored. Foreign metadata events invalidate local cache and enqueue a hint, with a capacity-bounded duplicate window. No multi-instance correctness claim is made: CAPTCHA and other process state remain local.

The worker reconnects after a bounded delay and redeclares topology without restarting the app. It uses one sequential publisher, a separate consumer channel, manual acknowledgements after local enqueue, mandatory routing and publisher confirms. Publish/connect/continuation operations are bounded; heartbeat detects dead links. The server queue is non-durable, exclusive, auto-deleted, bounded by message count/bytes, with a15s message TTL. Broker storage uses128MiB tmpfs; no persistent or anonymous broker volume. Broker restarts discard broker data. Neither startup nor application/database health depends on RabbitMQ; no app depends_on broker is added.

Envelope: version1, random process-origin UUID and the existing event's EventId/CommentId/ParentId/RootId/CreatedAt. No comment text, author/email/IP/User-Agent, files or secrets. Strict UTF-8 JSON decoding rejects duplicate/unknown fields and missing/invalid required fields and unsupported versions; maximum body1024bytes, also enforced by the client before large body allocation. Unsupported deliveries are rejected without requeue; exceptions are not logged with provider messages/payloads. Queue pressure and repeated hints do not mutate stored comments.

This is disposable signalling, not durable end-to-end messaging. Events occurring offline, after buffer overflow, after fallback expiry, or during a process crash may never reach RabbitMQ or future consumers. No replay is promised; browser reconnect/manual refresh restores current data from SQL. Publisher confirms and consumer acknowledgements cover different stages and do not close the SQL-commit-to-publish gap. A transactional outbox, durable queues/messages, recoverable consumers and an explicit migration/recovery plan require separate approval.

## Русский

Пункт брокера Middle реализован через RabbitMQ4.4.0 и RabbitMQ.Client7.2.2. GraphQL и прежние функции сохранены. NoSQL, Cloud, outbox, реплики и Middle+ не добавлены. RabbitMQ необязателен и по умолчанию отключён, в том числе для старых JSON. Источник истины - PostgreSQL; запись комментария и сброс кеша не ждут связи с брокером или подтверждения. Публичный конфиг не содержит настроек/секретов брокера.

После фиксации БД Events сразу сбрасывают прежний кеш. При готовой подписке router синхронно и без ожидания помещает событие в ограниченный буфер. Фоновый издатель отправляет версионированное сообщение в topic exchange; временная эксклюзивная очередь принимает его и передаёт в прежнюю локальную очередь объединяемых сигналов и WebSocket worker. Потребитель не публикует событие повторно. Сетевых операций внутри обработчика после сохранения нет.

Отключённый/недоступный брокер и переполнение буфера сразу включают прежний локальный путь. Если соединение выглядит живым, но сообщение не вернулось собственному потребителю за FallbackDelayMilliseconds, отдельный watchdog с шагом100мс отправляет локальный сигнал. Это покрывает зависший/приостановленный брокер и потерю доставки. Срок приблизительный, зависит от планирования процесса и не является жёстким SLA. Разрыв соединения сбрасывает ожидающие сигналы в локальную очередь. Одного publisher confirm недостаточно для отмены fallback: нужен возврат потребителю. Повторы/поздние собственные сообщения игнорируются; внешние сбрасывают кеш и сигнализируют с ограниченным окном дедупликации. Согласованность нескольких инстансов не заявляется: CAPTCHA и другое состояние прежние, в процессе.

Worker переподключается с задержкой и восстанавливает topology без перезапуска app. Один последовательный издатель, отдельный канал потребителя, ручной ack после локального enqueue, mandatory routing и publisher confirms. Сроки публикации/подключения/continuation ограничены, heartbeat обнаруживает разрыв. Серверная очередь недолговечная, эксклюзивная, auto-delete, с пределами количества/байтов и TTL15с. Хранилище брокера - tmpfs128МиБ, постоянных/анонимных томов нет; после перезапуска данные брокера исчезают. Запуск и health приложения/БД не зависят от RabbitMQ; depends_on брокера у app нет.

Сообщение: version1, случайный UUID процесса и EventId/CommentId/ParentId/RootId/CreatedAt существующего события. Текста, автора/e-mail/IP/User-Agent, файлов и секретов нет. Строгий UTF-8 JSON отвергает дубли/неизвестные поля и отсутствующие/неверные обязательные поля и версии; тело до1024байтов, ограничено также клиентом до выделения большого буфера. Неподдерживаемая доставка отклоняется без requeue; исключения/тело/параметры провайдера не журналируются. Повторы сигналов не меняют комментарии.

Это одноразовые сигналы, не долговечная доставка от SQL до потребителя. События при недоступности, переполнении, истечении fallback или аварии процесса могут не попасть в брокер/будущим потребителям. Replay не обещан; reconnect/ручное обновление читает актуальные данные SQL. Confirms и ack относятся к разным этапам и не устраняют разрыв commit-to-publish. Transactional outbox, долговечные очереди/сообщения, восстановление потребителей и план миграции/отката требуют отдельного согласования.

## Українська

Пункт брокера Middle реалізовано через RabbitMQ4.4.0 та RabbitMQ.Client7.2.2. GraphQL і попередні функції збережено. NoSQL, Cloud, outbox, репліки й Middle+ не додані. RabbitMQ необов'язковий і типово вимкнений, зокрема для старих JSON. Джерело істини - PostgreSQL; запис коментаря й скидання кешу не чекають зв'язку з брокером чи підтвердження. Публічний конфіг не містить налаштувань/секретів брокера.

Після фіксації БД Events одразу скидають попередній кеш. За готової підписки router синхронно й без очікування додає подію до обмеженого буфера. Фоновий видавець надсилає версіоноване повідомлення в topic exchange; тимчасова ексклюзивна черга приймає його й передає до попередньої локальної черги об'єднуваних сигналів і WebSocket worker. Споживач не публікує подію повторно. Мережевих операцій усередині обробника після збереження немає.

Вимкнений/недоступний брокер і переповнення буфера одразу вмикають попередній локальний шлях. Якщо з'єднання виглядає живим, але повідомлення не повернулося власному споживачеві за FallbackDelayMilliseconds, окремий watchdog із кроком100мс надсилає локальний сигнал. Це покриває завислий/призупинений брокер і втрату доставки. Строк приблизний, залежить від планування процесу й не є жорстким SLA. Розрив з'єднання скидає очікувані сигнали до локальної черги. Одного publisher confirm недостатньо для скасування fallback: потрібне повернення споживачеві. Повтори/пізні власні повідомлення ігноруються; зовнішні скидають кеш і сигналізують з обмеженим вікном дедуплікації. Узгодженість кількох інстансів не заявляється: CAPTCHA й інший стан попередні, у процесі.

Worker перепідключається із затримкою й відновлює topology без перезапуску app. Один послідовний видавець, окремий канал споживача, ручний ack після локального enqueue, mandatory routing і publisher confirms. Строки публікації/підключення/continuation обмежені, heartbeat виявляє розрив. Серверна черга недовговічна, ексклюзивна, auto-delete, з межами кількості/байтів і TTL15с. Сховище брокера - tmpfs128МіБ, постійних/анонімних томів немає; після перезапуску дані брокера зникають. Запуск і health застосунку/БД не залежать від RabbitMQ; depends_on брокера в app немає.

Повідомлення: version1, випадковий UUID процесу й EventId/CommentId/ParentId/RootId/CreatedAt наявної події. Тексту, автора/e-mail/IP/User-Agent, файлів і секретів немає. Строгий UTF-8 JSON відхиляє дублікати/невідомі поля й відсутні/хибні обов’язкові поля та версії; тіло до1024байтів, обмежене також клієнтом до виділення великого буфера. Непідтримувана доставка відхиляється без requeue; винятки/тіло/параметри провайдера не журналюються. Повтори сигналів не змінюють коментарі.

Це одноразові сигнали, не довговічна доставка від SQL до споживача. Події за недоступності, переповнення, спливання fallback чи аварії процесу можуть не потрапити до брокера/майбутніх споживачів. Replay не обіцяно; reconnect/ручне оновлення читає актуальні дані SQL. Confirms і ack належать різним етапам і не усувають розрив commit-to-publish. Transactional outbox, довговічні черги/повідомлення, відновлення споживачів і план міграції/відкату потребують окремого погодження.

## Configuration / Настройки / Налаштування

Backend keys under Runtime.Broker; defaults and allowed ranges / значения и диапазоны / значення й діапазони:

| Key | Default | Range |
| --- | --- | --- |
| Enabled | false | boolean |
| Host | broker | nonblank, <=253 characters; no whitespace/control/slashes |
| Port | 5672 | 1..65535 |
| Username | comments_app | nonblank, <=64 characters, no controls |
| Exchange | commentsservice.comments.v1 | 1..100 ASCII letters/digits/dot/hyphen/underscore |
| Capacity | 128 | 1..1024; sender, pending, broker queue and duplicate window |
| OperationTimeoutSeconds | 2 | 1..10 |
| ReconnectDelayMilliseconds | 2000 | 100..30000 |
| FallbackDelayMilliseconds | 1000 | 100..5000 |

EN: Password comes only from BROKER_PASSWORD, not JSON/public settings. Missing/bad credentials leave the app in local fallback and allow later retries. Settings use the established JSON/environment/CLI priority; restart to change credentials/settings. Compose includes an optional .env.broker for app and broker. The broker refuses startup without generated credentials. No host AMQP/management ports are published. The supplied local topology uses plain AMQP inside the private Docker network; it is not a configuration for exposing a remote broker.

RU: Пароль только из BROKER_PASSWORD, не из JSON/публичных настроек. Отсутствующие/неверные данные оставляют app на локальном fallback с повторами подключения. Приоритет JSON/окружения/CLI прежний; для смены параметров/секретов нужен перезапуск. Compose подключает необязательный .env.broker к app и брокеру. Брокер отказывается стартовать без сгенерированных данных. AMQP/management-порты хосту не публикуются. Внутри приватной Docker-сети используется обычный AMQP; это не конфигурация внешнего брокера.

UA: Пароль лише з BROKER_PASSWORD, не з JSON/публічних налаштувань. Відсутні/хибні дані залишають app на локальному fallback з повторами підключення. Пріоритет JSON/оточення/CLI попередній; для зміни параметрів/секретів потрібен перезапуск. Compose додає необов'язковий .env.broker до app і брокера. Брокер відмовляється стартувати без згенерованих даних. AMQP/management-порти хосту не публікуються. Усередині приватної Docker-мережі використовується звичайний AMQP; це не конфігурація зовнішнього брокера.

## Optional setup / Необязательная установка / Необов'язкове встановлення

Keep your existing project/env/overrides/volumes / Сохраните прежние проект/env/overrides/тома / Збережіть попередні проєкт/env/overrides/томи. The following is the portable default example / Переносимый пример / Переносний приклад:

```bash
docker compose -p commentsservice -f compose.yaml -f compose.tools.yaml run --rm --no-deps tools init-broker-env
# Existing app/database project from README; generate broker credentials once.
docker compose -p commentsservice --profile broker up -d --no-deps --wait broker
docker compose -p commentsservice build app
docker compose -p commentsservice up -d --no-deps --no-build --wait app
```

EN: No PostgreSQL migration or data move. Rollback: use the previous application image, existing environment/volumes and browser reload; stop only the owned broker if no longer wanted. The previous app ignores extra broker env fields. Do not delete database volumes or restore an older DB for a code rollback. Broker credentials must remain private/ignored and persist across service recreation; broker state itself is disposable. Default setup needs no external account, paid cloud service or new domain/DNS.

RU: Миграции PostgreSQL и переноса данных нет. Откат: прежний образ app, сохранённые окружение/тома и перезагрузка страницы; при необходимости остановить только свой брокер. Прежний app игнорирует дополнительные переменные брокера. Нельзя удалять тома БД или восстанавливать старую БД ради отката кода. Секреты брокера хранить приватно/вне Git и сохранять между пересозданиями; состояние брокера одноразовое. Внешние аккаунты/платные облака/новый домен/DNS не нужны.

UA: Міграції PostgreSQL і перенесення даних немає. Відкат: попередній образ app, збережені оточення/томи й перезавантаження сторінки; за потреби зупинити лише власний брокер. Попередній app ігнорує додаткові змінні брокера. Не можна видаляти томи БД або відновлювати стару БД задля відкату коду. Секрети брокера зберігати приватно/поза Git та зберігати між перествореннями; стан брокера одноразовий. Зовнішні акаунти/платні хмари/новий домен/DNS не потрібні.

Sources: [RabbitMQ release information](https://www.rabbitmq.com/release-information), [client API](https://www.rabbitmq.com/client-libraries/dotnet-api-guide), [confirms/acknowledgements](https://www.rabbitmq.com/docs/confirms), [RabbitMQ.Client7.2.2](https://www.nuget.org/packages/RabbitMQ.Client/7.2.2).

## Verification / Проверки / Перевірки

EN: Passed: 387 backend tests (27/85/67/208), 33 frontend tests, frontend/Docker builds, real Edge with RabbitMQ healthy/paused/stopped/recovered and no app restart, six GraphQL sorts, real CAPTCHA/root/reply, preview, exact TXT, WebSocket refresh/reconnect, drafts, EN/RU/UA and 360px. Startup with the broker already stopped and later recovery also passed. Original data/schema/history/corpus/checkpoints/env/volumes/preview preserved. The earlier million-row paging acceptance was not repeated because read queries are unchanged.

RU: Прошли 387 backend-тестов (27/85/67/208), 33 frontend, сборки frontend/Docker, Edge с работающим/приостановленным/остановленным/восстановленным RabbitMQ без перезапуска app, шесть сортировок GraphQL, реальные CAPTCHA/корень/ответ, preview, точный TXT, WS-обновление/переподключение, черновики, EN/RU/UA и 360px. Запуск при уже остановленном брокере и последующее восстановление также прошли. Исходные данные/схема/история/миллион/копии/env/тома/preview сохранены. Прежняя приёмка пагинации миллиона не повторялась: запросы чтения не менялись.

UA: Пройшли 387 backend-тестів (27/85/67/208), 33 frontend, збирання frontend/Docker, Edge з працюючим/призупиненим/зупиненим/відновленим RabbitMQ без перезапуску app, шість сортувань GraphQL, справжні CAPTCHA/корінь/відповідь, preview, точний TXT, WS-оновлення/перепідключення, чернетки, EN/RU/UA та 360px. Запуск за вже зупиненого брокера й подальше відновлення також пройшли. Початкові дані/схема/історія/мільйон/копії/env/томи/preview збережено. Попереднє приймання пагінації мільйона не повторювалось: запити читання не змінювались.

EN: Deployment verification also passed public read-only HTTPS/HSTS, GraphQL200, database200, WebSocket reconnect, draft retention and360px rendering. Both live services are healthy and the broker subscription is connected. No public comments were written for this verification; previous reboot/heartbeat acceptance was not repeated.

RU: После развёртывания прошли публичные проверки только чтением: HTTPS/HSTS, GraphQL200, БД200, переподключение WS, сохранение черновика и360px. Оба сервиса здоровы, подписка брокера подключена. Публичные комментарии для проверки не создавались; прежние reboot/heartbeat не повторялись.

UA: Після розгортання пройшли публічні перевірки лише читанням: HTTPS/HSTS, GraphQL200, БД200, перепідключення WS, збереження чернетки й360px. Обидва сервіси здорові, підписка брокера підключена. Публічні коментарі для перевірки не створювались; попередні reboot/heartbeat не повторювались.
