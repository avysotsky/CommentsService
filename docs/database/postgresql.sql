DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'comments') THEN
        CREATE SCHEMA comments;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS comments."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261029195935_InitialDataModel') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'comments') THEN
            CREATE SCHEMA comments;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261029195935_InitialDataModel') THEN
    CREATE TABLE comments."Authors" (
        "Id" uuid NOT NULL,
        "UserName" character varying(100) NOT NULL,
        "Email" character varying(254) NOT NULL,
        "HomePage" character varying(2048),
        CONSTRAINT "PK_Authors" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Authors_UserName" CHECK ("UserName" ~ '^[A-Za-z0-9]+$')
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261029195935_InitialDataModel') THEN
    CREATE TABLE comments."Comments" (
        "Id" uuid NOT NULL,
        "AuthorId" uuid NOT NULL,
        "Text" character varying(65536) NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ClientIp" inet NOT NULL,
        "UserAgent" character varying(2048) NOT NULL,
        CONSTRAINT "PK_Comments" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Comments_Text" CHECK (length(btrim("Text")) > 0),
        CONSTRAINT "FK_Comments_Authors_AuthorId" FOREIGN KEY ("AuthorId") REFERENCES comments."Authors" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261029195935_InitialDataModel') THEN
    CREATE INDEX "IX_Comments_AuthorId" ON comments."Comments" ("AuthorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261029195935_InitialDataModel') THEN
    CREATE INDEX "IX_Comments_CreatedAt_Id" ON comments."Comments" ("CreatedAt" DESC, "Id" DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261029195935_InitialDataModel') THEN
    INSERT INTO comments."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261029195935_InitialDataModel', '10.0.14');
    END IF;
END $EF$;
COMMIT;
