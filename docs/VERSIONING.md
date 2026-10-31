# Versioning / Версионирование / Версіонування

## English

Release format: `w.x.y.z`.

- w: global application version; change only when explicitly requested.
- x: feature version; change only when explicitly requested.
- y: configuration version; increased from 0 to 1 when config.json was implemented in 1.1.1.15 Configuration. Planning alone does not change it.
- z: revision; increment for the next planned iteration.

Additional work required within an iteration keeps the same w.x.y.z and gets
a different branch description, for example `CommentsService/v_1.1.0.2_DockerFix`.
It starts from the affected version and does not shift the planned version numbers.
Identify these updates by branch description and commit in the change logs.

Start: `1.1.0.1`. Branches follow `CommentsService/v_w.x.y.z_description`.
Each iteration starts from the previous completed version, adds its declared scope,
updates read.me, README and change.log, and is pushed to its own branch.
README.md is English, README.RU.md is Russian, README.UA.md is Ukrainian.
Keep all three README translations current; do not create README.en.md.
Change logs are separate: change.log (English), change.log.ru (Russian), change.log.ua (Ukrainian).
Keep their release entries synchronized. Other shared supporting documents contain
English, Russian and Ukrainian, in that order.
Record actual verification results; never claim an unexecuted check passed.

## Русский

Формат версии: `w.x.y.z`.

- w — глобальная версия приложения; изменяется только по прямому указанию.
- x — версия фичи; изменяется только по прямому указанию.
- y — версия конфига; повышена с 0 до 1 при реализации config.json в 1.1.1.15 Configuration. Планирование само по себе её не меняет.
- z — ревизия; увеличивается для следующей плановой итерации.

Дополнительная доработка внутри итерации сохраняет w.x.y.z и получает другое
описание ветки, например `CommentsService/v_1.1.0.2_DockerFix`. Она создаётся
от затронутой версии и не сдвигает плановую нумерацию. В журналах различать такие
доработки по описанию ветки и коммиту.

Начало: `1.1.0.1`. Ветки: `CommentsService/v_w.x.y.z_description`.
Каждая итерация создаётся от предыдущего завершённого выпуска, добавляет только
заявленную функциональность, обновляет read.me, README и change.log и отправляется
в свою ветку. README.md — английский, README.RU.md — русский, README.UA.md — украинский.
Все три перевода поддерживать актуальными; README.en.md не создавать.
Журналы разделены: change.log — английский, change.log.ru — русский, change.log.ua — украинский.
Записи выпусков синхронизировать. В остальных общих документах — английский, русский, украинский.
Результаты проверок записываются фактически; неподтверждённый PASS запрещён.

## Українська

Формат версії: `w.x.y.z`.

- w — глобальна версія застосунку; змінюється лише за прямою вказівкою.
- x — версія функціональності; змінюється лише за прямою вказівкою.
- y — версія конфігурації; підвищена з 0 до 1 під час реалізації config.json у 1.1.1.15 Configuration. Планування саме по собі її не змінює.
- z — ревізія; збільшується для наступної планової ітерації.

Додаткове доопрацювання всередині ітерації зберігає w.x.y.z та отримує інший опис
гілки, наприклад `CommentsService/v_1.1.0.2_DockerFix`. Воно починається від
відповідної версії та не зсуває планову нумерацію. У журналах розрізняти такі
доопрацювання за описом гілки й комітом.

Початок: `1.1.0.1`. Гілки: `CommentsService/v_w.x.y.z_description`.
Кожна ітерація створюється від попереднього завершеного випуску, додає лише
заявлену функціональність, оновлює read.me, README і change.log та публікується
у своїй гілці. README.md — англійською, README.RU.md — російською, README.UA.md — українською.
Усі три переклади підтримувати актуальними; README.en.md не створювати.
Журнали окремі: change.log — англійською, change.log.ru — російською, change.log.ua — українською.
Записи випусків синхронізувати. Решта спільних документів містить англійський, російський і український текст.
Результати перевірок записуються фактично; непідтверджений PASS заборонений.
