#!/usr/bin/env python3
"""
Sync data from ClickHouse to PostgreSQL to ensure identical datasets
"""

import clickhouse_connect
import psycopg2
from psycopg2.extras import execute_values
import sys
from datetime import datetime

# Connection parameters
CLICKHOUSE_HOST = 'localhost'
CLICKHOUSE_PORT = 8123
CLICKHOUSE_USER = 'demo_user'
CLICKHOUSE_PASSWORD = 'demo_password'

POSTGRES_HOST = 'localhost'
POSTGRES_PORT = 5432
POSTGRES_USER = 'postgres'
POSTGRES_PASSWORD = 'postgres'
POSTGRES_DB = 'demo_db'

def sync_data():
    print("Connecting to databases...")
    
    # Connect to ClickHouse
    ch_client = clickhouse_connect.get_client(
        host=CLICKHOUSE_HOST,
        port=CLICKHOUSE_PORT,
        username=CLICKHOUSE_USER,
        password=CLICKHOUSE_PASSWORD
    )
    
    # Connect to PostgreSQL
    pg_conn = psycopg2.connect(
        host=POSTGRES_HOST,
        port=POSTGRES_PORT,
        user=POSTGRES_USER,
        password=POSTGRES_PASSWORD,
        database=POSTGRES_DB
    )
    pg_cursor = pg_conn.cursor()
    
    try:
        # Check if ClickHouse has data
        count_result = ch_client.query('SELECT count() FROM taxi_db.trips')
        ch_count = count_result.result_rows[0][0]
        print(f"Found {ch_count} records in ClickHouse")
        
        if ch_count == 0:
            print("No data in ClickHouse. Please load data first.")
            return
        
        # Check how many records are already in PostgreSQL
        pg_cursor.execute("SELECT count(*) FROM taxi.trips")
        pg_count = pg_cursor.fetchone()[0]
        print(f"PostgreSQL already has {pg_count} records")
        
        # Fetch data from ClickHouse in batches
        batch_size = 50000  # Larger batches for faster transfer
        offset = pg_count   # Start from where we left off
        
        print(f"Continuing from record {offset:,}...")
        while offset < ch_count:
            batch_number = (offset // batch_size) + 1
            print(f"Processing batch {batch_number} (records {offset:,} to {min(offset + batch_size, ch_count):,})...")
            
            query = f"""
            SELECT 
                VendorID,
                tpep_pickup_datetime,
                tpep_dropoff_datetime,
                passenger_count,
                trip_distance,
                RatecodeID,
                store_and_fwd_flag,
                PULocationID,
                DOLocationID,
                payment_type,
                fare_amount,
                extra,
                mta_tax,
                tip_amount,
                tolls_amount,
                improvement_surcharge,
                total_amount,
                congestion_surcharge,
                airport_fee
            FROM taxi_db.trips
            ORDER BY tpep_pickup_datetime
            LIMIT {batch_size} OFFSET {offset}
            """
            
            result = ch_client.query(query)
            
            if not result.result_rows:
                break
            
            # Insert into PostgreSQL
            insert_query = """
            INSERT INTO taxi.trips (
                vendorid, tpep_pickup_datetime, tpep_dropoff_datetime,
                passenger_count, trip_distance, ratecodeid, store_and_fwd_flag,
                pulocationid, dolocationid, payment_type, fare_amount, extra,
                mta_tax, tip_amount, tolls_amount, improvement_surcharge,
                total_amount, congestion_surcharge, airport_fee
            ) VALUES %s
            """
            
            execute_values(pg_cursor, insert_query, result.result_rows)
            pg_conn.commit()
            
            offset += batch_size
        
        # Note: Skipping aggregated tables for now due to large dataset size
        
        pg_conn.commit()
        
        # Verify counts
        pg_cursor.execute("SELECT COUNT(*) FROM taxi.trips")
        pg_count = pg_cursor.fetchone()[0]
        
        print(f"\nSync completed!")
        print(f"ClickHouse records: {ch_count}")
        print(f"PostgreSQL records: {pg_count}")
        print(f"Match: {'✓' if ch_count == pg_count else '✗'}")
        
    except Exception as e:
        print(f"Error: {e}")
        pg_conn.rollback()
        raise
    finally:
        pg_cursor.close()
        pg_conn.close()
        ch_client.close()

if __name__ == "__main__":
    sync_data()