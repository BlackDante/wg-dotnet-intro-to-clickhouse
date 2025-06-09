-- Load NYC Taxi sample data from ClickHouse public dataset
-- We'll load a smaller subset for demo purposes

INSERT INTO taxi_db.trips
SELECT
    rowNumberInAllBlocks() AS trip_id,
    vendor_id,
    toDate(pickup_datetime) AS pickup_date,
    pickup_datetime,
    toDate(dropoff_datetime) AS dropoff_date,
    dropoff_datetime,
    store_and_fwd_flag,
    rate_code_id,
    pickup_longitude,
    pickup_latitude,
    dropoff_longitude,
    dropoff_latitude,
    passenger_count,
    trip_distance,
    fare_amount,
    extra,
    mta_tax,
    tip_amount,
    tolls_amount,
    ehail_fee,
    improvement_surcharge,
    total_amount,
    payment_type,
    trip_type,
    congestion_surcharge
FROM s3(
    'https://datasets-documentation.s3.eu-west-3.amazonaws.com/nyc-taxi/trips_*.gz',
    'TabSeparatedWithNames'
)
WHERE pickup_datetime >= '2019-01-01 00:00:00' 
  AND pickup_datetime < '2019-01-08 00:00:00'  -- Just one week of data
  AND trip_distance > 0
  AND fare_amount > 0
LIMIT 100000;

-- Export data to CSV for PostgreSQL import
-- This ensures both databases have identical data
SELECT 
    trip_id,
    vendor_id,
    pickup_datetime,
    dropoff_datetime,
    store_and_fwd_flag,
    rate_code_id,
    pickup_longitude,
    pickup_latitude,
    dropoff_longitude,
    dropoff_latitude,
    passenger_count,
    trip_distance,
    fare_amount,
    extra,
    mta_tax,
    tip_amount,
    tolls_amount,
    ehail_fee,
    improvement_surcharge,
    total_amount,
    payment_type,
    trip_type,
    congestion_surcharge
FROM taxi_db.trips
INTO OUTFILE '/var/lib/clickhouse/user_files/taxi_trips.csv'
FORMAT CSV
SETTINGS format_csv_delimiter = ',';

-- Populate aggregated daily statistics
INSERT INTO taxi_db.daily_stats
SELECT
    pickup_date AS date,
    count() AS total_trips,
    sum(passenger_count) AS total_passengers,
    sum(trip_distance) AS total_distance,
    sum(total_amount) AS total_fare,
    avg(trip_distance) AS avg_trip_distance,
    avg(fare_amount) AS avg_fare_amount,
    avg(if(fare_amount > 0, tip_amount / fare_amount * 100, 0)) AS avg_tip_percentage
FROM taxi_db.trips
GROUP BY pickup_date;

-- Populate payment type summary
INSERT INTO taxi_db.payment_summary
SELECT
    pickup_date AS date,
    payment_type,
    count() AS trip_count,
    sum(total_amount) AS total_amount
FROM taxi_db.trips
GROUP BY pickup_date, payment_type;