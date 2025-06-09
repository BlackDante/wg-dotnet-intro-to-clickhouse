# ClickHouse Data Warehouse Demo

This repository contains demo code for a presentation about ClickHouse, PostgreSQL, and Kafka integration.

## Services

- **ClickHouse**: Column-oriented OLAP database (port 8123 for HTTP, 9000 for native)
- **PostgreSQL**: Traditional relational database (port 5432)
- **Kafka**: Distributed streaming platform (port 9092)
- **Kafka UI**: Web interface for Kafka management (port 8080)

## Quick Start

1. Start all services:
```bash
docker-compose up -d
```

2. Load sample data into ClickHouse:
```bash
# Connect to ClickHouse
docker exec -it clickhouse-demo clickhouse-client --user demo_user --password demo_password

# Run the SQL scripts
:) source /docker-entrypoint-initdb.d/01_create_database.sql
:) source /docker-entrypoint-initdb.d/02_create_tables.sql
:) source /docker-entrypoint-initdb.d/03_load_sample_data.sql
```

3. Create PostgreSQL schema:
```bash
# Connect to PostgreSQL
docker exec -it postgres-demo psql -U postgres -d demo_db

# Create schema
\i /docker-entrypoint-initdb.d/01_create_schema.sql
```

4. Sync data from ClickHouse to PostgreSQL (ensures identical datasets):
```bash
# Install Python dependencies
pip install -r scripts/requirements.txt

# Run sync script
python scripts/sync_data.py
```

This ensures both databases have exactly the same data for accurate performance comparisons.

## Access Points

- ClickHouse HTTP: http://localhost:8123
- PostgreSQL: localhost:5432
- Kafka: localhost:9092
- Kafka UI: http://localhost:8080

## Credentials

- ClickHouse: `demo_user` / `demo_password`
- PostgreSQL: `postgres` / `postgres`

## Sample Queries

Check the `sql/clickhouse/04_sample_queries.sql` and `sql/postgresql/03_sample_queries.sql` files for example queries demonstrating various features.

## Dataset

The demo uses the NYC Taxi dataset, which contains taxi trip records including:
- Pickup/dropoff times and locations
- Trip distances and durations
- Fare amounts and payment types
- Passenger counts

## Stop Services

```bash
docker-compose down -v  # -v removes volumes
```