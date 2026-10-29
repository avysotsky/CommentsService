-- CommentsService 1.1.0.3: MySQL Workbench visualization only.
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
  `Text` MEDIUMTEXT NOT NULL COMMENT 'PostgreSQL VARCHAR(65536)',
  `CreatedAt` DATETIME(6) NOT NULL COMMENT 'UTC; PostgreSQL timestamptz',
  `ClientIp` VARCHAR(45) NOT NULL COMMENT 'PostgreSQL inet',
  `UserAgent` VARCHAR(2048) NOT NULL,
  PRIMARY KEY (`Id`),
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
