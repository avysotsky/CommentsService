# Versioning / Версионирование / Версіонування

## English

Release format: `w.x.y.z`.

- w: global application version; change only when explicitly requested.
- x: feature version; change only when explicitly requested.
- y: configuration version; remains 0 until configuration extraction is requested.
- z: revision; increment for the next iteration.

Start: `1.1.0.1`. Branches follow `CommentsService/v_w.x.y.z_description`.
Each iteration starts from the previous completed version, adds its declared scope,
updates read.me, README and change.log, and is pushed to its own branch.
README.md is English, README.RU.md is Russian, README.UA.md is Ukrainian.
Keep all three README translations current; do not create README.en.md.
Other supporting documents contain all three languages: English, Russian, Ukrainian, in that order.
Record actual verification results; never claim an unexecuted check passed.

## Русский

Формат версии: `w.x.y.z`.

- w — глобальная версия приложения; изменяется только по прямому указанию.
- x — версия фичи; изменяется только по прямому указанию.
- y — версия конфига; остаётся 0 до согласованного выноса данных в конфиг.
- z — ревизия; увеличивается для следующей итерации.

Начало: `1.1.0.1`. Ветки: `CommentsService/v_w.x.y.z_description`.
Каждая итерация создаётся от предыдущего завершённого выпуска, добавляет только
заявленную функциональность, обновляет read.me, README и change.log и отправляется
в свою ветку. README.md — английский, README.RU.md — русский, README.UA.md — украинский.
Все три перевода поддерживать актуальными; README.en.md не создавать.
В остальных сопроводительных документах три языка: английский, русский, украинский, в этом порядке.
Результаты проверок записываются фактически; неподтверждённый PASS запрещён.

## Українська

Формат версії: `w.x.y.z`.

- w — глобальна версія застосунку; змінюється лише за прямою вказівкою.
- x — версія функціональності; змінюється лише за прямою вказівкою.
- y — версія конфігурації; залишається 0 до погодженого винесення даних у конфігурацію.
- z — ревізія; збільшується для наступної ітерації.

Початок: `1.1.0.1`. Гілки: `CommentsService/v_w.x.y.z_description`.
Кожна ітерація створюється від попереднього завершеного випуску, додає лише
заявлену функціональність, оновлює read.me, README і change.log та публікується
у своїй гілці. README.md — англійською, README.RU.md — російською, README.UA.md — українською.
Усі три переклади підтримувати актуальними; README.en.md не створювати.
Решта супровідних документів містить три мови в такому порядку: англійська, російська, українська.
Результати перевірок записуються фактично; непідтверджений PASS заборонений.
