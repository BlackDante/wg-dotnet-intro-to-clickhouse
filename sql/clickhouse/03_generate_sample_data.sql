-- Generate sample NYC taxi data for demonstration
-- This creates realistic data without relying on external sources

INSERT INTO taxi_db.trips
SELECT
    number AS trip_id,
    if(rand() % 3 = 0, '1', if(rand() % 3 = 1, '2', '3')) AS vendor_id,
    toDate('2019-01-01') + (number % 7) AS pickup_date,
    toDateTime('2019-01-01 00:00:00') + (number * 60) AS pickup_datetime,
    toDate('2019-01-01') + (number % 7) AS dropoff_date,
    toDateTime('2019-01-01 00:00:00') + (number * 60) + (rand() % 3600 + 300) AS dropoff_datetime,
    if(rand() % 2 = 0, 'N', 'Y') AS store_and_fwd_flag,
    (rand() % 5 + 1) AS rate_code_id,
    -74.0 + (rand() % 10000) / 10000.0 AS pickup_longitude,
    40.7 + (rand() % 10000) / 10000.0 AS pickup_latitude,
    -74.0 + (rand() % 10000) / 10000.0 AS dropoff_longitude,
    40.7 + (rand() % 10000) / 10000.0 AS dropoff_latitude,
    (rand() % 4 + 1) AS passenger_count,
    (rand() % 200 + 10) / 10.0 AS trip_distance,
    (rand() % 500 + 50) / 10.0 AS fare_amount,
    if(rand() % 10 > 7, (rand() % 20) / 10.0, 0) AS extra,
    0.5 AS mta_tax,
    if(rand() % 2 = 0, (rand() % 100) / 10.0, 0) AS tip_amount,
    if(rand() % 10 > 8, (rand() % 50) / 10.0, 0) AS tolls_amount,
    0 AS ehail_fee,
    0.3 AS improvement_surcharge,
    0 AS total_amount,  -- Will be calculated
    if(rand() % 3 = 0, 'CSH', if(rand() % 3 = 1, 'CRE', 'NOC')) AS payment_type,
    1 AS trip_type,
    if(rand() % 10 > 7, 2.5, 0) AS congestion_surcharge
FROM numbers(100000);

-- Update total_amount
ALTER TABLE taxi_db.trips UPDATE 
    total_amount = fare_amount + extra + mta_tax + tip_amount + tolls_amount + improvement_surcharge + congestion_surcharge
WHERE total_amount = 0;

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