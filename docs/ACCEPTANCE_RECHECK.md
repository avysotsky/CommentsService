# AcceptanceRecheck 1.1.1.15 — 2026-10-31

## English

Completed sequence: DtoContracts (1.1.0.15), Configuration, Logging and AcceptanceRecheck (1.1.1.15). Application/API own 17/5 DTO files; explicit entity mapping is separate. Wire fields, routes and schema are preserved. Configuration supplies validated per-instance identity, nonsecret database/pool/timeouts, health and logging settings; environment/CLI override JSON, with existing PG compatibility. Serilog writes structured console/file events through one DI pipeline, with levels/category overrides, InstanceId/TraceId, daily/size rolling, retention, write checks and shutdown disposal. Inner layers remain framework-free; no migration was added.

The recheck corrected PG/CLI precedence: PG variables are mapped into typed settings before explicit CLI overrides, which now win consistently. The regression test checks host and port overrides.

Passed on the final code:

- 283 backend tests: Domain/architecture 27, Application 84, Data 56, API 116; 26 frontend tests; frontend and Docker builds.
- Exact DTO JSON contracts; typed config defaults/CLI validation; malformed/missing/invalid explicit files fail startup. Two concurrent Docker instances use distinct read-only config files/ports and matching console/file events with independent identities and request traces.
- Logging Information/Warning filtering, category overrides, concurrent writes, file-size rotation/retention, shutdown flush and invalid log storage. Existing file events and prior host shutdown survived application recreation. Tested secrets, e-mail/body/provider payloads were absent from logs; JSON timestamps were UTC.
- Edge desktop/360px: real CAPTCHA, XHTML/preview, TXT/image writes/replies/viewer/exact downloads, replay and invalid input rejection, CSP, safe hostile-DTO fallback and blocked inline script/framing.
- All 18 read-only retained-million cases: three sort fields, two directions and first/middle/last pages, three samples each; stable IDs, 25 roots/page, correct totals. The corpus was reused without reseeding.
- PostgreSQL/Workbench projection metadata parity; final fingerprints preserve all original rows/schema/history in three databases, corpus metadata, both database volumes, private environment, checkpoint hashes and existing preview identity/state. A separate application log volume was added.

The original [Acceptance report](ACCEPTANCE.md) remains historical evidence for 1.1.0.15. Restore/fresh-install/downgrade suites were not repeated: no schema/model change, with schema parity and original checkpoints preserved. Workbench GUI import, manual screen-reader/IDE checks and external deployment remain unverified. Deployment 1.1.1.16 requires a target server. [Configuration](CONFIGURATION.md) · [Logging](LOGGING.md).

## Русский

Завершены DtoContracts (1.1.0.15), Configuration, Logging и AcceptanceRecheck (1.1.1.15). Application/API владеют 17/5 файлами DTO; маппинг сущности вынесен отдельно. JSON, маршруты и схема сохранены. Конфигурация задаёт проверяемые инстанс, несекретные параметры БД/пула/таймаутов, health и логи; окружение/CLI имеют приоритет над JSON, PG-совместимость сохранена. Единая DI-система Serilog пишет console/file с уровнями/категориями, InstanceId/TraceId, ротацией по дате/размеру, хранением, проверкой записи и завершением. Внутренние слои независимы от фреймворков; миграции нет.

При повторной приёмке исправлен приоритет PG/CLI: PG отображаются в типизированные параметры до явных CLI, которые имеют окончательный приоритет. Регрессионный тест проверяет host и port.

На итоговом коде прошли:

- 283 backend-теста: Domain/архитектура 27, Application 84, Data 56, API 116; 26 frontend; сборки frontend/Docker.
- Точные JSON-контракты DTO, defaults/CLI/валидация конфига; отказ при отсутствующем/повреждённом/неверном явно выбранном файле. Два одновременных Docker-инстанса с разными read-only файлами/портами, совпадающими console/file событиями и независимыми идентификаторами/трассировкой.
- Фильтрация Information/Warning и категорий, конкурентная запись, ротация/хранение, flush при завершении, неверное хранилище логов. Файлы и запись остановки предыдущего host сохранились после пересоздания. Проверяемые секреты, e-mail/тела/диагностика провайдера отсутствуют; время JSON — UTC.
- Edge desktop/360px: настоящая CAPTCHA, XHTML/preview, TXT/изображение/ответ/просмотр/точные загрузки, отказ повтору и неверному вводу, CSP, безопасный вывод вредоносного DTO, блокировка inline-скрипта/фрейма.
- Все 18 read-only сценариев сохранённого миллиона: 3 поля сортировки, 2 направления, первая/средняя/последняя страницы, по 3 замера; стабильные ID, 25 корней, верные итоги. Набор не генерировался повторно.
- Паритет PostgreSQL/проекции Workbench; итоговые отпечатки сохранили все исходные строки/схемы/историю трёх БД, метаданные набора, два тома БД, приватное окружение, хеши копий и ID/состояние прежнего просмотра. Добавлен отдельный том логов приложения.

[Прежняя приёмка](ACCEPTANCE.md) остаётся историческим свидетельством 1.1.0.15. Восстановление/пустая установка/откаты не повторялись: модель/схема прежние, паритет проверен, исходные копии сохранены. GUI Workbench, ручной диктор/IDE и внешнее развёртывание не проверены. Deployment 1.1.1.16 требует целевой сервер. [Конфигурация](CONFIGURATION.md) · [Логи](LOGGING.md).

## Українська

Завершено DtoContracts (1.1.0.15), Configuration, Logging та AcceptanceRecheck (1.1.1.15). Application/API володіють 17/5 файлами DTO; маппінг сутності винесено окремо. JSON, маршрути й схему збережено. Конфігурація задає перевірювані інстанс, несекретні параметри БД/пулу/таймаутів, health і логи; оточення/CLI мають пріоритет над JSON, PG-сумісність збережено. Єдина DI-система Serilog пише console/file з рівнями/категоріями, InstanceId/TraceId, ротацією за датою/розміром, зберіганням, перевіркою запису й завершенням. Внутрішні шари незалежні від фреймворків; міграції немає.

Під час повторного приймання виправлено пріоритет PG/CLI: PG відображаються в типізовані параметри до явних CLI, які мають остаточний пріоритет. Регресійний тест перевіряє host і port.

На підсумковому коді пройшли:

- 283 backend-тести: Domain/архітектура 27, Application 84, Data 56, API 116; 26 frontend; збирання frontend/Docker.
- Точні JSON-контракти DTO, defaults/CLI/валідація конфігу; відмова за відсутнього/пошкодженого/хибного явно вибраного файла. Два одночасні Docker-інстанси з різними read-only файлами/портами, однаковими console/file подіями й незалежними ідентифікаторами/трасуванням.
- Фільтрація Information/Warning і категорій, конкурентний запис, ротація/зберігання, flush під час завершення, хибне сховище логів. Файли й запис зупинки попереднього host збереглися після перестворення. Перевірювані секрети, e-mail/тіла/діагностика провайдера відсутні; час JSON — UTC.
- Edge desktop/360px: справжня CAPTCHA, XHTML/preview, TXT/зображення/відповідь/перегляд/точні завантаження, відмова повтору й хибному вводу, CSP, безпечне відображення шкідливого DTO, блокування inline-скрипту/фрейму.
- Усі 18 read-only сценаріїв збереженого мільйона: 3 поля сортування, 2 напрями, перша/середня/остання сторінки, по 3 виміри; стабільні ID, 25 коренів, правильні підсумки. Набір повторно не генерувався.
- Паритет PostgreSQL/проєкції Workbench; підсумкові відбитки зберегли всі початкові рядки/схеми/історію трьох БД, метадані набору, два томи БД, приватне оточення, хеші копій та ID/стан попереднього перегляду. Додано окремий том логів застосунку.

[Попереднє приймання](ACCEPTANCE.md) лишається історичним свідченням 1.1.0.15. Відновлення/порожня установка/відкочування не повторювалися: модель/схема попередні, паритет перевірено, початкові копії збережено. GUI Workbench, ручний диктор/IDE та зовнішнє розгортання не перевірено. Deployment 1.1.1.16 потребує цільового сервера. [Конфігурація](CONFIGURATION.md) · [Логи](LOGGING.md).
