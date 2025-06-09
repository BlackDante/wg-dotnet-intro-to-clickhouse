-- Import data from ClickHouse export
-- This ensures PostgreSQL has the exact same dataset as ClickHouse
-- NOTE: This script should be run manually after ClickHouse data is loaded

-- First, truncate existing data
TRUNCATE TABLE taxi.trips;

-- Import CSV data exported from ClickHouse
-- Note: Run this manually after ClickHouse export is complete
/*
COPY taxi.trips (
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
)
FROM '/data/taxi_trips.csv'
WITH (FORMAT csv, HEADER false);
*/

-- The following will be run by the Python sync script
-- Populate daily statistics (same logic as ClickHouse)
TRUNCATE TABLE taxi.daily_stats;

INSERT INTO taxi.daily_stats
SELECT
    DATE(pickup_datetime) AS date,
    COUNT(*) AS total_trips,
    SUM(passenger_count) AS total_passengers,
    SUM(trip_distance) AS total_distance,
    SUM(total_amount) AS total_fare,
    AVG(trip_distance) AS avg_trip_distance,
    AVG(fare_amount) AS avg_fare_amount,
    AVG(CASE WHEN fare_amount > 0 THEN (tip_amount / fare_amount * 100) ELSE 0 END) AS avg_tip_percentage
FROM taxi.trips
GROUP BY DATE(pickup_datetime);

-- Populate payment summary (same logic as ClickHouse)
TRUNCATE TABLE taxi.payment_summary;

INSERT INTO taxi.payment_summary
SELECT
    DATE(pickup_datetime) AS date,
    payment_type,
    COUNT(*) AS trip_count,
    SUM(total_amount) AS total_amount
FROM taxi.trips
GROUP BY DATE(pickup_datetime), payment_type;

-- Add same indexes as before
CREATE INDEX IF NOT EXISTS idx_trips_pickup_datetime ON taxi.trips(pickup_datetime);
CREATE INDEX IF NOT EXISTS idx_trips_vendor_id ON taxi.trips(vendor_id);
CREATE INDEX IF NOT EXISTS idx_trips_payment_type ON taxi.trips(payment_type);