#!/bin/bash
set -e

# Optional: Wait for PostgreSQL to be ready
if [ -n "$DB_HOST" ]; then
  echo "Waiting for PostgreSQL at $DB_HOST:$DB_PORT..."
  until pg_isready -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER"; do
    sleep 2
  done
  echo "PostgreSQL is available!"
fi

# Apply migrations programmatically on startup instead of using EF CLI
echo "Migrations will be applied programmatically on application startup"

# Run the app
exec dotnet 8-ball-pool.dll
