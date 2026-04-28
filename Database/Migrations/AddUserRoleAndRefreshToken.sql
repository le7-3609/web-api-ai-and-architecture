-- Migration: Add Role and RefreshToken columns to the Users table
-- Run this script against your database before starting the application.

ALTER TABLE Users
    ADD [Role] NVARCHAR(50) NOT NULL DEFAULT 'User';

ALTER TABLE Users
    ADD [RefreshToken] NVARCHAR(512) NULL;

ALTER TABLE Users
    ADD [RefreshTokenExpiry] DATETIME NULL;
