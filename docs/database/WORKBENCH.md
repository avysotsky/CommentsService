# Workbench model / Модель Workbench / Модель Workbench

## English

Open [commentsservice.mwb](commentsservice.mwb) in classic MySQL Workbench 8.0.47. [PNG preview](workbench-schema.png) contains five named table figures and four foreign-key connections, including the comment self-reference. This is a visualization of PostgreSQL, not a MySQL database deployment. [SQL projection](workbench.sql) and [type mappings](SCHEMA.md) explain differences; PostgreSQL migrations/checks remain authoritative.

Verified in real Workbench 8.0.47 via its supported scripting interface: import SQL with zero parser errors, save native .mwb, open that file in a fresh Workbench process, inspect the loaded model and export its diagram. Counts:5tables/25columns/12indexes/5primary keys/4foreign keys/5figures/4connections. The exported image was visually inspected. No database connection, migration or user-data write was involved. Author metadata is CommentsService; archive contents were checked for machine paths/hostnames.

To regenerate, run Workbench with `--run-script scripts/export-workbench-model.py --quit-when-done` using the script's absolute path if your working directory differs. It overwrites only the model/preview and creates a new unsaved document; use a separate Workbench process. It is not a standalone Python script: `grt` is provided by Workbench. Review layout/count guards if the schema changes.

Limitation: this extracted Windows Workbench reports process exit `0xC0020001` after logging successful shutdown. The import/save/reopen/export reports and files passed independent checks; a clean native process exit and manual menu-click workflow are not claimed. UI automation could not initialize. No application/backend rebuild was needed for this artifact-only correction; Windows reboot acceptance remains separate.

## Русский

Откройте [commentsservice.mwb](commentsservice.mwb) в классическом MySQL Workbench8.0.47. [Превью PNG](workbench-schema.png) содержит пять подписанных таблиц и четыре связи, включая самоссылку комментариев. Это визуализация PostgreSQL, не развёртывание MySQL. [SQL-проекция](workbench.sql) и [соответствия типов](SCHEMA.md) описывают различия; миграции/проверки PostgreSQL остаются эталоном.

В настоящем Workbench8.0.47 через штатный scripting проверены импорт SQL без ошибок парсера, сохранение .mwb, открытие файла новым процессом, состав загруженной модели и экспорт диаграммы. Итоги:5таблиц/25колонок/12индексов/5PK/4FK/5фигур/4связи. Картинка проверена визуально. Подключений к БД, миграций и записи пользовательских данных не было. Автор модели CommentsService; содержимое архива проверено на пути/имя компьютера.

Генерация: Workbench `--run-script scripts/export-workbench-model.py --quit-when-done`, при другом рабочем каталоге укажите абсолютный путь скрипта. Перезаписываются только модель/превью; создаётся новый документ, используйте отдельный процесс Workbench. Обычный Python не подходит: `grt` предоставляет Workbench. При изменении схемы пересмотрите layout/проверки количества.

Ограничение: распакованный Windows Workbench после сообщения об успешном завершении возвращает `0xC0020001`. Импорт/сохранение/повторное открытие/экспорт и файлы прошли независимые проверки; чистый код завершения и ручные клики меню не заявлены. Управление UI не инициализировалось. Для дополнения только артефактов сборка backend не требовалась; проверка перезагрузки Windows остаётся отдельно.

## Українська

Відкрийте [commentsservice.mwb](commentsservice.mwb) у класичному MySQL Workbench8.0.47. [Прев'ю PNG](workbench-schema.png) містить п'ять підписаних таблиць і чотири зв'язки, включно із самопосиланням коментарів. Це візуалізація PostgreSQL, не розгортання MySQL. [SQL-проєкція](workbench.sql) та [відповідності типів](SCHEMA.md) описують відмінності; міграції/перевірки PostgreSQL лишаються еталоном.

У справжньому Workbench8.0.47 через штатний scripting перевірено імпорт SQL без помилок парсера, збереження .mwb, відкриття файла новим процесом, склад завантаженої моделі й експорт діаграми. Підсумки:5таблиць/25колонок/12індексів/5PK/4FK/5фігур/4зв'язки. Зображення перевірено візуально. Підключень до БД, міграцій і запису користувацьких даних не було. Автор моделі CommentsService; вміст архіву перевірено на шляхи/назву комп'ютера.

Генерація: Workbench `--run-script scripts/export-workbench-model.py --quit-when-done`, за іншого робочого каталогу вкажіть абсолютний шлях скрипту. Перезаписуються лише модель/прев'ю; створюється новий документ, використовуйте окремий процес Workbench. Звичайний Python не підходить: `grt` надає Workbench. За зміни схеми перегляньте layout/перевірки кількості.

Обмеження: розпакований Windows Workbench після повідомлення про успішне завершення повертає `0xC0020001`. Імпорт/збереження/повторне відкриття/експорт і файли пройшли незалежні перевірки; чистий код завершення та ручні кліки меню не заявлено. Керування UI не ініціалізувалося. Для доповнення лише артефактів збирання backend не вимагалося; перевірка перезавантаження Windows лишається окремо.
