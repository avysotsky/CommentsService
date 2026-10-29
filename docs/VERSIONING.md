# Versioning / Версионирование

## English

Release format: `w.x.y.z`.

- w: global application version; change only when explicitly requested.
- x: feature version; change only when explicitly requested.
- y: configuration version; remains 0 until configuration extraction is requested.
- z: revision; increment for the next iteration.

Start: `1.1.0.1`. Branches follow `CommentsService/v_w.x.y.z_description`.
Each iteration starts from the previous completed version, adds its declared scope,
updates read.me, README and change.log, and is pushed to its own branch.
English comes first, Russian second in supporting documents.
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
в свою ветку. В документах сначала английский, затем русский.
Результаты проверок записываются фактически; неподтверждённый PASS запрещён.
