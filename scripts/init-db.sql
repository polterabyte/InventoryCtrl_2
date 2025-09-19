-- Database initialization script
-- This script runs when the PostgreSQL container starts for the first time

-- Create database if it doesn't exist (usually handled by POSTGRES_DB env var)
-- CREATE DATABASE IF NOT EXISTS inventorydb;

-- Set timezone
SET timezone = 'UTC';

-- Create extensions if needed
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- The actual database schema will be created by Entity Framework migrations
-- when the API starts up
