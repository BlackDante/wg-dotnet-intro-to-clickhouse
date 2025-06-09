-- Create schema for taxi data
CREATE SCHEMA IF NOT EXISTS taxi;

-- Create trips table
CREATE TABLE IF NOT EXISTS taxi.trips (
    trip_id INTEGER,
    vendor_id VARCHAR(10),
    pickup_datetime TIMESTAMP,
    dropoff_datetime TIMESTAMP,
    store_and_fwd_flag VARCHAR(1),
    rate_code_id SMALLINT,
    pickup_longitude DOUBLE PRECISION,
    pickup_latitude DOUBLE PRECISION,
    dropoff_longitude DOUBLE PRECISION,
    dropoff_latitude DOUBLE PRECISION,
    passenger_count SMALLINT,
    trip_distance DOUBLE PRECISION,
    fare_amount DECIMAL(10,2),
    extra DECIMAL(10,2),
    mta_tax DECIMAL(10,2),
    tip_amount DECIMAL(10,2),
    tolls_amount DECIMAL(10,2),
    ehail_fee DECIMAL(10,2),
    improvement_surcharge DECIMAL(10,2),
    total_amount DECIMAL(10,2),
    payment_type VARCHAR(3),
    trip_type SMALLINT,
    congestion_surcharge DECIMAL(10,2)
);

-- Create indexes for better query performance
CREATE INDEX idx_trips_pickup_datetime ON taxi.trips(pickup_datetime);
CREATE INDEX idx_trips_vendor_id ON taxi.trips(vendor_id);
CREATE INDEX idx_trips_payment_type ON taxi.trips(payment_type);

-- Create daily statistics table
CREATE TABLE IF NOT EXISTS taxi.daily_stats (
    date DATE PRIMARY KEY,
    total_trips INTEGER,
    total_passengers BIGINT,
    total_distance DOUBLE PRECISION,
    total_fare DECIMAL(15,2),
    avg_trip_distance DOUBLE PRECISION,
    avg_fare_amount DECIMAL(10,2),
    avg_tip_percentage DECIMAL(5,2)
);

-- Create payment summary table
CREATE TABLE IF NOT EXISTS taxi.payment_summary (
    date DATE,
    payment_type VARCHAR(3),
    trip_count INTEGER,
    total_amount DECIMAL(15,2),
    PRIMARY KEY (date, payment_type)
);