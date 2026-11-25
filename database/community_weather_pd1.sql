-- Community Weather Monitoring and Disaster Information System
-- Run in MySQL Workbench (File > Open SQL Script > Execute). Optional: the app also creates these tables
-- automatically on first run (EnsureCreated), so use this script if you want to create the schema yourself.

CREATE DATABASE IF NOT EXISTS community_weather_pd1 CHARACTER SET utf8mb4;
USE community_weather_pd1;

CREATE TABLE IF NOT EXISTS Users (
    Id                  INT AUTO_INCREMENT PRIMARY KEY,
    FullName            VARCHAR(100) NOT NULL,
    Email               VARCHAR(150) NOT NULL,          -- stored lower-case
    PasswordHash        VARCHAR(255) NOT NULL,          -- PBKDF2-SHA256 with per-user salt; never the plain password
    Role                VARCHAR(20)  NOT NULL DEFAULT 'Resident',
    IsActive            TINYINT(1)   NOT NULL DEFAULT 1,
    EmailAlerts         TINYINT(1)   NOT NULL DEFAULT 1,
    FailedLoginAttempts INT          NOT NULL DEFAULT 0,
    LockoutEnd          DATETIME(6)  NULL,              -- UTC
    LastLoginAt         DATETIME(6)  NULL,
    CreatedAt           DATETIME(6)  NOT NULL,
    UNIQUE KEY IX_Users_Email (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Admin', 'Resident'))
);

CREATE TABLE IF NOT EXISTS Advisories (
    Id            INT AUTO_INCREMENT PRIMARY KEY,
    Title         VARCHAR(200) NOT NULL,
    Message       TEXT         NOT NULL,
    AffectedArea  VARCHAR(150) NOT NULL,
    Category      VARCHAR(50)  NOT NULL,
    Priority      VARCHAR(10)  NOT NULL,
    Status        VARCHAR(15)  NOT NULL,
    PublishedAt   DATETIME(6)  NULL,
    ValidUntil    DATETIME(6)  NULL,
    EmailSent     TINYINT(1)   NOT NULL DEFAULT 0,
    CreatedAt     DATETIME(6)  NOT NULL,
    KEY IX_Advisories_Status_PublishedAt (Status, PublishedAt)
);

CREATE TABLE IF NOT EXISTS EvacuationCenters (
    Id               INT AUTO_INCREMENT PRIMARY KEY,
    Name             VARCHAR(150) NOT NULL,
    Address          VARCHAR(255) NOT NULL,
    ContactNumber    VARCHAR(100) NOT NULL,
    OperationalNotes TEXT         NOT NULL,
    FacilityType     VARCHAR(50)  NOT NULL,
    Latitude         DOUBLE NULL,
    Longitude        DOUBLE NULL,
    UpdatedAt        DATETIME(6)  NOT NULL
);

CREATE TABLE IF NOT EXISTS PreparednessGuides (
    Id         INT AUTO_INCREMENT PRIMARY KEY,
    Title      VARCHAR(200) NOT NULL,
    Hazard     VARCHAR(50)  NOT NULL,
    Content    TEXT         NOT NULL,
    UpdatedAt  DATETIME(6)  NOT NULL,
    KEY IX_PreparednessGuides_Hazard (Hazard)
);
-- The first administrator is created by the app on first run from the SeedAdmin configuration
-- (password hashed by the app). Sample guides and locations are also inserted by the app.
