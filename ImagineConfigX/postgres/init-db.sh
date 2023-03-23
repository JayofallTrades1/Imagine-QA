#!/bin/bash

set -e

echo "Creating keycloak and content databases"

psql -v ON_ERROR_STOP=1 --username postgres --dbname postgres <<-EOSQL
    CREATE DATABASE mcp_build  
EOSQL

echo "Databases created"
echo "Setting up content database"

psql -v ON_ERROR_STOP=1 --username postgres --dbname mcp_build --file=/docker-entrypoint-initdb.d/scripts/mcp_build.sql # --echo-all

## Adding this to create workflow table which doesn't appear to be added with existing scripts - or hermes bug

psql -v ON_ERROR_STOP=1 --username postgres --dbname postgres <<-EOSQL
CREATE TABLE IF NOT EXISTS public.workflow
(
  id bigint PRIMARY KEY NOT NULL,
  key varchar,
  data jsonb
);
EOSQL

echo "Database Setup script completed"
