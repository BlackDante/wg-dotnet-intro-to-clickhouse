#!/bin/bash
# ClickHouse initialization script

echo "Waiting for ClickHouse to be ready..."
until clickhouse-client --user demo_user --password demo_password --query "SELECT 1" > /dev/null 2>&1; do
    sleep 1
done

echo "Initializing ClickHouse database..."
clickhouse-client --user demo_user --password demo_password < /docker-entrypoint-initdb.d/01_create_database.sql
clickhouse-client --user demo_user --password demo_password < /docker-entrypoint-initdb.d/02_create_tables.sql
echo "ClickHouse initialization complete!"