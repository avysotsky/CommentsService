#!/usr/bin/env bash
set -euo pipefail

# Executed by the PostgreSQL image only when initializing an empty volume.
psql --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  -v ON_ERROR_STOP=1 -v app_password="$APP_DB_PASSWORD" -v db_name="$POSTGRES_DB" <<'SQL'
CREATE ROLE comments_app LOGIN PASSWORD :'app_password' NOSUPERUSER NOCREATEDB NOCREATEROLE;
ALTER DATABASE :"db_name" OWNER TO comments_app;
ALTER SCHEMA public OWNER TO comments_app;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
SQL
