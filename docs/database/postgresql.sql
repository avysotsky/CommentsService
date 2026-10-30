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

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030101720_CommentThreads') THEN
    ALTER TABLE comments."Comments" ADD "ParentId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030101720_CommentThreads') THEN
    ALTER TABLE comments."Comments" ADD "RootId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030101720_CommentThreads') THEN
    UPDATE comments."Comments" SET "RootId" = "Id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030101720_CommentThreads') THEN
    ALTER TABLE comments."Comments" ALTER COLUMN "RootId" SET NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030101720_CommentThreads') THEN
    ALTER TABLE comments."Comments" ADD CONSTRAINT "AK_Comments_Id_RootId" UNIQUE ("Id", "RootId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030101720_CommentThreads') THEN
    CREATE INDEX "IX_Comments_ParentId_CreatedAt_Id" ON comments."Comments" ("ParentId", "CreatedAt", "Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030101720_CommentThreads') THEN
    CREATE INDEX "IX_Comments_ParentId_RootId" ON comments."Comments" ("ParentId", "RootId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030101720_CommentThreads') THEN
    ALTER TABLE comments."Comments" ADD CONSTRAINT "CK_Comments_Thread" CHECK (("ParentId" IS NULL AND "RootId" = "Id") OR ("ParentId" IS NOT NULL AND "ParentId" <> "Id" AND "RootId" <> "Id"));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030101720_CommentThreads') THEN
    ALTER TABLE comments."Comments" ADD CONSTRAINT "FK_Comments_Comments_ParentId_RootId" FOREIGN KEY ("ParentId", "RootId") REFERENCES comments."Comments" ("Id", "RootId") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030101720_CommentThreads') THEN
    INSERT INTO comments."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261030101720_CommentThreads', '10.0.14');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030122207_ImageAttachments') THEN
    CREATE TABLE comments."ImageAttachments" (
        "Id" uuid NOT NULL,
        "CommentId" uuid NOT NULL,
        "FileName" character varying(128) NOT NULL,
        "Width" integer NOT NULL,
        "Height" integer NOT NULL,
        "Data" bytea NOT NULL,
        CONSTRAINT "PK_ImageAttachments" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_Images_Data" CHECK (octet_length("Data") BETWEEN 1 AND 524288),
        CONSTRAINT "CK_Images_Dimensions" CHECK ("Width" BETWEEN 1 AND 320 AND "Height" BETWEEN 1 AND 240),
        CONSTRAINT "FK_ImageAttachments_Comments_CommentId" FOREIGN KEY ("CommentId") REFERENCES comments."Comments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030122207_ImageAttachments') THEN
    CREATE UNIQUE INDEX "IX_ImageAttachments_CommentId" ON comments."ImageAttachments" ("CommentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030122207_ImageAttachments') THEN
    INSERT INTO comments."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261030122207_ImageAttachments', '10.0.14');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030130243_TextAttachments') THEN
    CREATE TABLE comments."TextAttachments" (
        "Id" uuid NOT NULL,
        "CommentId" uuid NOT NULL,
        "FileName" character varying(128) NOT NULL,
        "SizeBytes" integer NOT NULL,
        "Data" bytea NOT NULL,
        CONSTRAINT "PK_TextAttachments" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_TextAttachments_Size" CHECK (octet_length("Data") BETWEEN 1 AND 102400 AND "SizeBytes" = octet_length("Data")),
        CONSTRAINT "FK_TextAttachments_Comments_CommentId" FOREIGN KEY ("CommentId") REFERENCES comments."Comments" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030130243_TextAttachments') THEN
    CREATE UNIQUE INDEX "IX_TextAttachments_CommentId" ON comments."TextAttachments" ("CommentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM comments."__EFMigrationsHistory" WHERE "MigrationId" = '20261030130243_TextAttachments') THEN
    INSERT INTO comments."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261030130243_TextAttachments', '10.0.14');
    END IF;
END $EF$;
COMMIT;
