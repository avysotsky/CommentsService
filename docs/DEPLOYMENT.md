# Deployment 1.1.1.16 / Развёртывание / Розгортання

EN Junior+ update: WebSocket /api/comments/live requires HTTP/1.1 Upgrade forwarding, original Host/Origin, trusted overwritten X-Forwarded-Proto/For, buffering disabled and heartbeat-compatible timeouts. Current public WSS handshake, reconnect and45s heartbeat were verified without public database writes. This extends the application virtual host only. [Protocol/configuration](JUNIOR_PLUS.md).

RU Обновление Junior+: /api/comments/live требует передачи Upgrade через HTTP/1.1, исходных Host/Origin, доверенных заменённых X-Forwarded-Proto/For, отключённой буферизации и таймаутов для heartbeat. Публичные WSS/переподключение/heartbeat45с проверены без записи в публичную БД. Расширен только виртуальный хост приложения. [Протокол/конфиг](JUNIOR_PLUS.md).

UA Оновлення Junior+: /api/comments/live потребує передавання Upgrade через HTTP/1.1, початкових Host/Origin, довірених замінених X-Forwarded-Proto/For, вимкненої буферизації й таймаутів для heartbeat. Публічні WSS/перепідключення/heartbeat45с перевірено без запису в публічну БД. Розширено лише віртуальний хост застосунку. [Протокол/конфіг](JUNIOR_PLUS.md).

## English

Live demo: https://comments.alexvysotsky.com. The user verified a real CAPTCHA-backed comment from mobile Internet with Wi-Fi disabled. The application runs in Docker inside WSL; the existing edge nginx terminates HTTPS and selects this subdomain by hostname. Domain registration, authoritative DNS provider, mail records and existing port-forwarding rules remain unchanged. Cloudflare is not used.

Topology: public HTTPS → edge nginx → edge loopback SSH reverse port → application host loopback → WSL/Docker API. PostgreSQL and the API's origin port are not published to the Internet. Use a separate nginx virtual host and certificate for the application; preserve existing virtual hosts/default server/certificates. Point only the new DNS A record to the reachable public IPv4. With changing IPv4, the DNS record must be updated. An occupied public 80/443 should stay on the existing edge; DNS alone cannot select a different LAN destination for those ports.

An example private transport is:
```sh
ssh -NT -o BatchMode=yes -o StrictHostKeyChecking=yes -o ExitOnForwardFailure=yes \
  -o ServerAliveInterval=30 -o ServerAliveCountMax=3 \
  -R 127.0.0.1:18081:127.0.0.1:8081 user@edge.example.net
```
The command runs on the application host. Keep the remote bind on loopback. Authenticate with a protected SSH key and verify the edge host key. Supervise reconnection and start the existing Docker application before the tunnel. The current Windows task starts at interactive user logon; it does not promise unattended startup before logon or prove a reboot test. Do not reboot the development computer without arranging it with its owner.

Configure nginx's new hostname with a separate webroot ACME challenge, HTTP→HTTPS redirect, a certificate for that hostname and upstream http://127.0.0.1:18081. Forward Host, overwrite X-Forwarded-For with the socket peer and X-Forwarded-Proto with the scheme; do not append untrusted client values. Keep client_max_body_size at 6m and proxy_cache off. A hostname-specific certificate renewal hook should run nginx -t and reload nginx. Never disable TLS verification.

Runtime.Proxy.KnownProxies contains explicit IP addresses of the immediate transport peers observed by the API; it defaults to empty (forwarding disabled). Supply deployment-specific values through a private config or environment, for example Runtime__Proxy__KnownProxies__0. Do not blindly trust all networks. The API consumes one symmetric X-Forwarded-For/Proto hop and adds HSTS for forwarded HTTPS. The peer seen through Docker/SSH may be a local bridge gateway rather than the edge machine. Header trust is checked before storing client metadata; IP is not exposed by public DTOs or application logs.

Preserve the same Compose project, environment, database/log volumes and private overrides. Rebuild only the app for this release; no schema migration is added. Keep verified checkpoints and the retained corpus. Do not reset databases or delete user comments for tests. Stop only the owned tunnel or remove only the owned virtual host to withdraw public access. CAPTCHA is not authentication or a general rate limiter; configure edge abuse/rate controls for sustained public traffic. No independent penetration test or automatic host reboot was performed. See [verification](DEPLOYMENT_VERIFICATION.md).

## Русский

Демо: https://comments.alexvysotsky.com. Пользователь проверил запись с настоящей CAPTCHA через мобильный интернет без Wi-Fi. Приложение работает в Docker внутри WSL; существующий nginx на входном сервере завершает HTTPS и выбирает поддомен по имени. Регистратор, DNS-провайдер, почтовые записи и прежние правила роутера сохранены. Cloudflare не используется.

Схема: публичный HTTPS → nginx → loopback SSH reverse port → loopback компьютера приложения → WSL/Docker API. PostgreSQL и порт origin API не открыты в интернет. Нужны отдельные virtual host/сертификат; прежние сайты/default server/сертификаты не менять. Только новая A-запись указывает на доступный публичный IPv4. При смене IPv4 запись обновляется. Занятые80/443 остаются на существующем входном сервере: DNS сам по себе не выбирает другое устройство LAN для этих портов.

Пример SSH выше выполняется на компьютере приложения. Remote bind оставлять loopback; использовать защищённый ключ SSH и проверку host key. Нужны переподключение и запуск прежнего Docker-приложения перед туннелем. Текущая задача Windows запускается при интерактивном входе пользователя; запуск до входа и фактическая перезагрузка не подтверждены. Перезагрузка компьютера согласуется отдельно.

Для нового hostname nginx задайте отдельный webroot ACME, редирект HTTP→HTTPS, свой сертификат и upstream http://127.0.0.1:18081. Передавайте Host, заменяйте X-Forwarded-For адресом socket peer, X-Forwarded-Proto — схемой; не добавляйте недоверенные значения клиента. client_max_body_size6m, proxy_cache off. Свой renewal hook выполняет nginx -t и reload. Проверку TLS не отключать.

Runtime.Proxy.KnownProxies содержит явные адреса непосредственных доверенных transport peers; по умолчанию пусто, forwarding выключен. Значения задаются приватным конфигом/окружением, например Runtime__Proxy__KnownProxies__0. Не доверять всем сетям. API обрабатывает один симметричный hop X-Forwarded-For/Proto и добавляет HSTS для forwarded HTTPS. Через Docker/SSH peer может быть локальным gateway, а не IP входного сервера. Проверка доверия выполняется до записи клиентских метаданных; IP не попадает в публичные DTO и логи.

Сохраняйте прежние Compose-проект, окружение, тома БД/логов и приватные overrides. Выпуск требует пересборки приложения, миграции схемы нет. Сохраняйте копии/большой набор; не сбрасывайте БД и не удаляйте пользовательские комментарии ради тестов. Отключение публикации — остановка своего туннеля либо удаление только своего virtual host. CAPTCHA не заменяет аутентификацию/rate limiter; для длительного публичного использования нужны edge-ограничения. Независимый пентест и автоматическая перезагрузка не выполнялись. [Проверки](DEPLOYMENT_VERIFICATION.md).

## Українська

Демо: https://comments.alexvysotsky.com. Користувач перевірив запис зі справжньою CAPTCHA через мобільний інтернет без Wi-Fi. Застосунок працює в Docker усередині WSL; наявний nginx вхідного сервера завершує HTTPS та обирає піддомен за назвою. Реєстратор, DNS-провайдер, поштові записи й попередні правила роутера збережено. Cloudflare не використовується.

Схема: публічний HTTPS → nginx → loopback SSH reverse port → loopback комп'ютера застосунку → WSL/Docker API. PostgreSQL і порт origin API не відкриті в інтернет. Потрібні окремі virtual host/сертифікат; попередні сайти/default server/сертифікати не змінювати. Лише новий A-запис указує на доступний публічний IPv4. За зміни IPv4 запис оновлюється. Зайняті80/443 лишаються на наявному вхідному сервері: DNS сам не обирає інший пристрій LAN для цих портів.

Приклад SSH вище виконується на комп'ютері застосунку. Remote bind лишати loopback; використовувати захищений SSH-ключ і перевірку host key. Потрібні перепідключення й запуск попереднього Docker-застосунку перед тунелем. Поточна задача Windows запускається під час інтерактивного входу користувача; запуск до входу та фактичне перезавантаження не підтверджено. Перезавантаження комп'ютера погоджується окремо.

Для нового hostname nginx задайте окремий webroot ACME, редирект HTTP→HTTPS, власний сертифікат та upstream http://127.0.0.1:18081. Передавайте Host, замінюйте X-Forwarded-For адресою socket peer, X-Forwarded-Proto — схемою; не додавайте недовірені значення клієнта. client_max_body_size6m, proxy_cache off. Власний renewal hook виконує nginx -t та reload. Перевірку TLS не вимикати.

Runtime.Proxy.KnownProxies містить явні адреси безпосередніх довірених transport peers; типово порожньо, forwarding вимкнено. Значення задаються приватним конфігом/оточенням, наприклад Runtime__Proxy__KnownProxies__0. Не довіряти всім мережам. API обробляє один симетричний hop X-Forwarded-For/Proto й додає HSTS для forwarded HTTPS. Через Docker/SSH peer може бути локальним gateway, а не IP вхідного сервера. Перевірка довіри виконується до запису клієнтських метаданих; IP не потрапляє до публічних DTO й логів.

Зберігайте попередні Compose-проєкт, оточення, томи БД/логів та приватні overrides. Випуск потребує перезбирання застосунку, міграції схеми немає. Зберігайте копії/великий набір; не скидайте БД та не видаляйте користувацькі коментарі заради тестів. Вимкнення публікації — зупинка власного тунелю чи видалення лише власного virtual host. CAPTCHA не замінює автентифікацію/rate limiter; для тривалого публічного використання потрібні edge-обмеження. Незалежний пентест і автоматичне перезавантаження не виконувалися. [Перевірки](DEPLOYMENT_VERIFICATION.md).

## References / Источники / Джерела

- [ASP.NET Core trusted proxies](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0)
- [nginx reverse proxy](https://nginx.org/en/docs/http/ngx_http_proxy_module.html)
