# Migrations and recovery / Миграции и восстановление / Міграції та відновлення

## English

Version 1.1.0.15 adds no migration. The four additive migrations below are current; API startup checks readiness but never migrates automatically.

Before changing a retained schema or existing data, stop all writers and use `python3 scripts/db-checkpoint.py` with the same project/environment/Compose files. Add `--include-benchmark` only when that persistent database exists. The command pg_dumps each database, restores into one uniquely named temporary database, compares all table row hashes before/after, and only then replaces that database's previous dump/manifest. Keep both files private. A checkpoint is a recovery point, not a scheduled off-host backup.

To reverify an existing checkpoint without replacing it, run `python3 scripts/verify-checkpoint.py artifacts/checkpoints/database.dump --upgrade`. This checks SHA-256 before any DB creation, restores with no original ownership/ACLs under comments_app, compares the manifest, applies the shipped idempotent export, preserves all previous rows/history, reapplies it for idempotency, then removes only its own named DB. It never rewrites source archives or changes the application's connection. The testing-profile database service must be running. Options include --manifest, --service, --project, --env-file, repeated --compose-file and --output.

For a real recovery, retain the current DB, keep writers stopped, restore into a separate retained recovery database, verify row/file hashes and role ownership/grants, then deliberately switch the application's database connection only after review. Recovered data comes from checkpoint time: later writes are absent. Do not drop the current database to rehearse recovery.

Rollback guards: CommentThreads refuses Down when replies exist; ImageAttachments refuses Down when image rows exist; TextAttachments refuses Down when TXT rows exist. Empty-feature downgrade is not a routine cleanup method. InitialDataModel.Down drops data and must not be used on retained application data. Prefer a verified checkpoint when backward migration would lose information.

Acceptance verified all four upgrades on preserved fixtures, three populated-feature guards and empty-feature downs, recovery/upgrading of all three previous checkpoints including the million-row corpus, and current-data recovery with seven exact attachments on an isolated fresh installation. Original live DBs/checkpoints were unchanged. See [acceptance](../ACCEPTANCE.md).

| Migration / Миграция / Міграція | Added / Добавлено / Додано |
|---|---|
| `20261029195935_InitialDataModel` | Authors / Comments |
| `20261030101720_CommentThreads` | ParentId / RootId / composite FK |
| `20261030122207_ImageAttachments` | ImageAttachments / bytea |
| `20261030130243_TextAttachments` | TextAttachments / bytea |

## Русский

Версия 1.1.0.15 не добавляет миграцию. Актуальны четыре расширяющие миграции ниже; API при старте проверяет готовность, но автоматически схему не меняет.

Перед изменением сохранённой схемы или данных остановите всех писателей и используйте `python3 scripts/db-checkpoint.py` с прежними проектом/окружением/Compose-файлами. Добавляйте `--include-benchmark`, только если такая постоянная БД существует. Команда делает pg_dump каждой БД, восстанавливает по одной в временную БД с уникальным именем, сравнивает хеши всех строк до/после и только затем заменяет предыдущие dump/manifest этой БД. Оба файла приватные. Контрольная копия — точка восстановления, а не плановый внешний backup.

Чтобы проверить существующую копию без замены, выполните `python3 scripts/verify-checkpoint.py artifacts/checkpoints/database.dump --upgrade`. SHA-256 проверяется до создания БД; копия восстанавливается без исходных владельцев/ACL под comments_app, сравнивается manifest, применяется текущий идемпотентный экспорт с сохранением прежних строк/истории и повторяется проверка идемпотентности. Удаляется только собственная БД. Архивы и подключение приложения не меняются. Сервис тестовой БД профиля testing должен работать. Параметры: --manifest, --service, --project, --env-file, повторяемый --compose-file и --output.

Для реального восстановления сохраните текущую БД, держите писателей остановленными, восстановите в отдельную сохраняемую БД, проверьте хеши строк/файлов и владельцев/права роли. Только после проверки явно переключайте подключение приложения. Данные соответствуют моменту копии: последующие записи отсутствуют. Не удаляйте текущую БД ради репетиции.

Защита отката: CommentThreads запрещает Down при наличии ответов; ImageAttachments — картинок; TextAttachments — TXT. Откат пустой фичи не является обычной очисткой. InitialDataModel.Down удаляет данные и недопустим для сохранённых данных приложения. Когда обратная миграция теряет информацию, предпочитайте проверенную копию.

Приёмка проверила четыре обновления на сохранённых фикстурах, три запрета отката заполненной фичи и откаты пустых фич, восстановление/обновление трёх прежних копий с миллионным набором, восстановление текущих данных с семью точными вложениями на отдельной новой установке. Исходные БД/копии сохранены. См. [приёмку](../ACCEPTANCE.md).

| Migration / Миграция / Міграція | Added / Добавлено / Додано |
|---|---|
| `20261029195935_InitialDataModel` | Authors / Comments |
| `20261030101720_CommentThreads` | ParentId / RootId / composite FK |
| `20261030122207_ImageAttachments` | ImageAttachments / bytea |
| `20261030130243_TextAttachments` | TextAttachments / bytea |

## Українська

Версія 1.1.0.15 не додає міграцію. Актуальні чотири розширювальні міграції нижче; API під час старту перевіряє готовність, але автоматично схему не змінює.

Перед зміною збереженої схеми або даних зупиніть усіх писачів і використовуйте `python3 scripts/db-checkpoint.py` із попередніми проєктом/оточенням/Compose-файлами. Додавайте `--include-benchmark`, лише якщо така постійна БД існує. Команда робить pg_dump кожної БД, відновлює по одній у тимчасову БД з унікальною назвою, порівнює хеші всіх рядків до/після й лише потім замінює попередні dump/manifest цієї БД. Обидва файли приватні. Контрольна копія — точка відновлення, не плановий зовнішній backup.

Щоб перевірити наявну копію без заміни, виконайте `python3 scripts/verify-checkpoint.py artifacts/checkpoints/database.dump --upgrade`. SHA-256 перевіряється до створення БД; копія відновлюється без початкових власників/ACL під comments_app, порівнюється manifest, застосовується поточний ідемпотентний експорт зі збереженням попередніх рядків/історії та повторюється перевірка ідемпотентності. Видаляється лише власна БД. Архіви та підключення застосунку не змінюються. Сервіс тестової БД профілю testing має працювати. Параметри: --manifest, --service, --project, --env-file, повторюваний --compose-file та --output.

Для реального відновлення збережіть поточну БД, тримайте писачів зупиненими, відновіть в окрему збережувану БД, перевірте хеші рядків/файлів і власників/права ролі. Лише після перевірки явно перемикайте підключення застосунку. Дані відповідають моменту копії: подальших записів немає. Не видаляйте поточну БД заради репетиції.

Захист відкочування: CommentThreads забороняє Down за наявності відповідей; ImageAttachments — зображень; TextAttachments — TXT. Відкочування порожньої фічі не є звичайним очищенням. InitialDataModel.Down видаляє дані й неприпустимий для збережених даних застосунку. Коли зворотна міграція втрачає інформацію, віддавайте перевагу перевіреній копії.

Приймання перевірило чотири оновлення на збережених фікстурах, три заборони відкочування заповненої фічі та відкочування порожніх фіч, відновлення/оновлення трьох попередніх копій із мільйонним набором, відновлення поточних даних із сімома точними вкладеннями на окремому новому встановленні. Початкові БД/копії збережено. Див. [приймання](../ACCEPTANCE.md).

| Migration / Миграция / Міграція | Added / Добавлено / Додано |
|---|---|
| `20261029195935_InitialDataModel` | Authors / Comments |
| `20261030101720_CommentThreads` | ParentId / RootId / composite FK |
| `20261030122207_ImageAttachments` | ImageAttachments / bytea |
| `20261030130243_TextAttachments` | TextAttachments / bytea |
