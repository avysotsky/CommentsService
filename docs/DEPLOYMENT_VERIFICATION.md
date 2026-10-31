# Deployment 1.1.1.16 verification — 2026-10-31

## English

Public URL: https://comments.alexvysotsky.com. A user confirmed a successful comment submission from mobile Internet with Wi-Fi disabled. HTTPS uses a separate Let's Encrypt certificate (reported expiry 2027-01-29), with certificate renewal enabled and a hostname-specific nginx reload hook. Existing edge sites, domain registration, authoritative DNS/mail and router rules were not replaced. The app and database remain in existing Docker/WSL storage.

Passed: 288 backend tests (27 Domain/architecture,84 Application,56 Data,121 API), including trusted/untrusted proxy and invalid trust settings; 26 frontend tests; frontend/Docker builds. The API trusts only explicitly configured immediate peers and one symmetric forwarded hop; forwarding is disabled by default. No dependency/schema/migration change was introduced.

Public Edge tests passed on 1.1.1.16: HTTPS/HSTS, desktop/360px SPA, actual CAPTCHA-backed creation, replay rejection, literal UTF-8 TXT exact download, preview and no-store API responses. A forged client IP header was overwritten by nginx; the owned fixture stored the real edge peer, not the forged value or local transport gateway. Only owned fixtures were removed. Before/after fingerprints retain current user comments, all three databases' rows/schema/history, corpus metadata, original volumes/private environment/checkpoints and the existing benchmark preview.

The Windows-owned SSH supervisor starts with user logon and retries disconnected sessions; its owned child was deliberately terminated and public HTTPS recovered automatically. Docker application recreation preserved user data/logs. This does not prove startup before logon or a Windows reboot; no computer reboot was performed. Workbench GUI remains pending. CAPTCHA is not general abuse protection; separate edge rate limits/independent penetration testing were not claimed. [Operation and deployment](DEPLOYMENT.md).

## Русский

Адрес: https://comments.alexvysotsky.com. Пользователь подтвердил успешную запись через мобильный интернет без Wi-Fi. Используется отдельный сертификат Let's Encrypt (сообщённый срок до2027-01-29), включено продление и свой nginx reload hook. Прежние сайты, регистратор, DNS/почта и правила роутера не заменялись. Приложение/БД остаются в прежнем Docker/WSL-хранилище.

Прошли288backend (27 Domain/архитектура,84 Application,56 Data,121 API), включая доверенный/недоверенный прокси и неверные параметры доверия;26frontend; сборки frontend/Docker. API доверяет только явно заданным непосредственным peers и одному симметричному forwarded hop; по умолчанию forwarding выключен. Зависимости/схема/миграции не менялись.

Публичный Edge на1.1.1.16 проверил HTTPS/HSTS, desktop/360px SPA, запись с настоящей CAPTCHA, отказ повтору, точное скачивание буквального UTF-8 TXT, preview и no-store API. Поддельный IP клиента заменён nginx; своя фикстура сохранила реальный edge peer, а не поддельное значение/gateway. Удалены только свои фикстуры. Отпечатки до/после сохраняют нынешние пользовательские комментарии, строки/схемы/историю трёх БД, набор, прежние тома/окружение/копии/benchmark preview.

Свой supervisor SSH запускается при входе Windows и повторяет подключения; его дочерний процесс намеренно остановлен, публичный HTTPS восстановился автоматически. Пересоздание Docker-приложения сохранило данные/логи. Это не доказывает запуск до входа или после перезагрузки Windows; компьютер не перезагружался. Workbench GUI pending. CAPTCHA не заменяет общую защиту; отдельные edge rate limits/независимый пентест не заявлены. [Эксплуатация](DEPLOYMENT.md).

## Українська

Адреса: https://comments.alexvysotsky.com. Користувач підтвердив успішний запис через мобільний інтернет без Wi-Fi. Використовується окремий сертифікат Let's Encrypt (повідомлений строк до2027-01-29), увімкнено поновлення й власний nginx reload hook. Попередні сайти, реєстратор, DNS/пошта та правила роутера не замінювалися. Застосунок/БД лишаються в попередньому Docker/WSL-сховищі.

Пройшли288backend (27 Domain/архітектура,84 Application,56 Data,121 API), включно з довіреним/недовіреним проксі та хибними параметрами довіри;26frontend; збирання frontend/Docker. API довіряє лише явно заданим безпосереднім peers та одному симетричному forwarded hop; типово forwarding вимкнено. Залежності/схема/міграції не змінювалися.

Публічний Edge на1.1.1.16 перевірив HTTPS/HSTS, desktop/360px SPA, запис зі справжньою CAPTCHA, відмову повтору, точне завантаження буквального UTF-8 TXT, preview і no-store API. Підроблений IP клієнта замінено nginx; власна фікстура зберегла реальний edge peer, а не підроблене значення/gateway. Видалено лише власні фікстури. Відбитки до/після зберігають поточні користувацькі коментарі, рядки/схеми/історію трьох БД, набір, попередні томи/оточення/копії/benchmark preview.

Власний supervisor SSH запускається під час входу Windows і повторює підключення; його дочірній процес навмисно зупинено, публічний HTTPS відновився автоматично. Перестворення Docker-застосунку зберегло дані/логи. Це не доводить запуск до входу чи після перезавантаження Windows; комп'ютер не перезавантажувався. Workbench GUI pending. CAPTCHA не замінює загальний захист; окремі edge rate limits/незалежний пентест не заявлені. [Експлуатація](DEPLOYMENT.md).
