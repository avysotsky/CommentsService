-- CommentsService 1.1.0.10: MySQL Workbench visualization only.
-- PostgreSQL migrations/postgresql.sql are authoritative for deployment.
-- uuid -> CHAR(36), inet -> VARCHAR(45), timestamptz -> DATETIME(6) in UTC.
-- PostgreSQL check constraints are documented in SCHEMA.md.
CREATE SCHEMA IF NOT EXISTS `comments`;
USE `comments`;
CREATE TABLE `Authors` (
  `Id` CHAR(36) NOT NULL,
  `UserName` VARCHAR(100) NOT NULL,
  `Email` VARCHAR(254) NOT NULL,
  `HomePage` VARCHAR(2048) NULL,
  PRIMARY KEY (`Id`)
);
CREATE TABLE `Comments` (
  `Id` CHAR(36) NOT NULL,
  `AuthorId` CHAR(36) NOT NULL,
  `ParentId` CHAR(36) NULL,
  `RootId` CHAR(36) NOT NULL,
  `Text` MEDIUMTEXT NOT NULL COMMENT 'PostgreSQL VARCHAR(65536)',
  `CreatedAt` DATETIME(6) NOT NULL COMMENT 'UTC; PostgreSQL timestamptz',
  `ClientIp` VARCHAR(45) NOT NULL COMMENT 'PostgreSQL inet',
  `UserAgent` VARCHAR(2048) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `AK_Comments_Id_RootId` (`Id`, `RootId`),
  INDEX `IX_Comments_ParentId_CreatedAt_Id` (`ParentId`, `CreatedAt`, `Id`),
  INDEX `IX_Comments_ParentId_RootId` (`ParentId`, `RootId`),
  CONSTRAINT `FK_Comments_Parent_Root` FOREIGN KEY (`ParentId`, `RootId`)
    REFERENCES `Comments` (`Id`, `RootId`) ON DELETE RESTRICT,
  INDEX `IX_Comments_AuthorId` (`AuthorId`),
  INDEX `IX_Comments_CreatedAt_Id` (`CreatedAt` DESC, `Id` DESC),
  CONSTRAINT `FK_Comments_Authors_AuthorId` FOREIGN KEY (`AuthorId`)
    REFERENCES `Authors` (`Id`) ON DELETE RESTRICT
);
CREATE TABLE `__EFMigrationsHistory` (
  `MigrationId` VARCHAR(150) NOT NULL,
  `ProductVersion` VARCHAR(32) NOT NULL,
  PRIMARY KEY (`MigrationId`)
);

CREATE TABLE `ImageAttachments` (
  `Id` CHAR(36) NOT NULL,
  `CommentId` CHAR(36) NOT NULL,
  `FileName` VARCHAR(128) NOT NULL,
  `Width` INT NOT NULL,
  `Height` INT NOT NULL,
  `Data` MEDIUMBLOB NOT NULL COMMENT 'PostgreSQL bytea; 1..524288 bytes',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_ImageAttachments_CommentId` (`CommentId`),
  CONSTRAINT `FK_ImageAttachments_Comments` FOREIGN KEY (`CommentId`)
    REFERENCES `Comments` (`Id`) ON DELETE CASCADE
);
