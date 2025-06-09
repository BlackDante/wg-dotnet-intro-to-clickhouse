-- Create NYC Taxi trips table with MergeTree engine
CREATE TABLE IF NOT EXISTS taxi_db.trips
(
    trip_id UInt32,
    vendor_id Enum8('1' = 1, '2' = 2, '3' = 3, '4' = 4, 'CMT' = 5, 'VTS' = 6, 'DDS' = 7, 'B02512' = 10, 'B02598' = 11, 'B02617' = 12, 'B02682' = 13, 'B02764' = 14, '' = 15),
    pickup_date Date,
    pickup_datetime DateTime,
    dropoff_date Date,
    dropoff_datetime DateTime,
    store_and_fwd_flag Enum8('' = 0, '0' = 1, '1' = 2, 'N' = 3, 'Y' = 4),
    rate_code_id UInt8,
    pickup_longitude Float64,
    pickup_latitude Float64,
    dropoff_longitude Float64,
    dropoff_latitude Float64,
    passenger_count UInt8,
    trip_distance Float64,
    fare_amount Float32,
    extra Float32,
    mta_tax Float32,
    tip_amount Float32,
    tolls_amount Float32,
    ehail_fee Float32,
    improvement_surcharge Float32,
    total_amount Float32,
    payment_type Enum8('UNK' = 0, 'CSH' = 1, 'CRE' = 2, 'NOC' = 3, 'DIS' = 4),
    trip_type UInt8,
    congestion_surcharge Float32
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(pickup_date)
ORDER BY (pickup_date, pickup_datetime)
SETTINGS index_granularity = 8192;

-- Create aggregated daily stats table
CREATE TABLE IF NOT EXISTS taxi_db.daily_stats
(
    date Date,
    total_trips UInt32,
    total_passengers UInt64,
    total_distance Float64,
    total_fare Float64,
    avg_trip_distance Float64,
    avg_fare_amount Float64,
    avg_tip_percentage Float64
)
ENGINE = SummingMergeTree()
ORDER BY date;

-- Create payment type summary table
CREATE TABLE IF NOT EXISTS taxi_db.payment_summary
(
    date Date,
    payment_type Enum8('UNK' = 0, 'CSH' = 1, 'CRE' = 2, 'NOC' = 3, 'DIS' = 4),
    trip_count UInt32,
    total_amount Float64
)
ENGINE = SummingMergeTree()
ORDER BY (date, payment_type);