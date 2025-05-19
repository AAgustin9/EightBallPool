#!/bin/bash
set -e

# Check for required DB environment variables
if [[ -z "$DB_HOST" || -z "$DB_PORT" || -z "$DB_USER" ]]; then
  echo "Missing one or more required DB environment variables: DB_HOST, DB_PORT, DB_USER"
  exit 1
fi

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL at $DB_HOST:$DB_PORT..."
until pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER"; do
  sleep 2
done
echo "PostgreSQL is available!"

# Migrations are applied inside Program.cs
echo "Migrations will be applied programmatically at app startup..."

# Run the app
exec dotnet 8-ball-pool.dll
