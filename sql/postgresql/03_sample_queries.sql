-- Sample queries for PostgreSQL (equivalent to ClickHouse queries)

-- 1. Top 10 busiest days
SELECT 
    DATE(tpep_pickup_datetime) as pickup_date,
    COUNT(*) as trip_count,
    SUM(total_amount) as revenue,
    AVG(trip_distance) as avg_distance
FROM taxi.trips
GROUP BY DATE(tpep_pickup_datetime)
ORDER BY trip_count DESC
LIMIT 10;

-- 2. Payment type distribution
SELECT 
    payment_type,
    COUNT(*) as trips,
    SUM(total_amount) as total_revenue,
    AVG(tip_amount) as avg_tip
FROM taxi.trips
GROUP BY payment_type
ORDER BY trips DESC;

-- 3. Hourly trip patterns
SELECT 
    EXTRACT(HOUR FROM tpep_pickup_datetime) as hour,
    COUNT(*) as trips,
    AVG(passenger_count) as avg_passengers,
    AVG(trip_distance) as avg_distance
FROM taxi.trips
GROUP BY EXTRACT(HOUR FROM tpep_pickup_datetime)
ORDER BY hour;

-- 4. Trip distance distribution
SELECT 
    FLOOR(trip_distance / 5) * 5 as distance_bucket,
    COUNT(*) as trip_count,
    AVG(total_amount) as avg_fare
FROM taxi.trips
WHERE trip_distance > 0 AND trip_distance < 100
GROUP BY FLOOR(trip_distance / 5) * 5
ORDER BY distance_bucket;

-- 5. Revenue by vendor
SELECT 
    vendorid,
    COUNT(*) as total_trips,
    SUM(total_amount) as total_revenue,
    AVG(total_amount) as avg_revenue_per_trip
FROM taxi.trips
GROUP BY vendorid
ORDER BY total_revenue DESC;

-- 6. Window function example - Running totals
SELECT 
    pickup_date,
    daily_trips,
    daily_revenue,
    SUM(daily_trips) OVER (ORDER BY pickup_date) as cumulative_trips,
    SUM(daily_revenue) OVER (ORDER BY pickup_date) as cumulative_revenue
FROM (
    SELECT 
        DATE(tpep_pickup_datetime) as pickup_date,
        COUNT(*) as daily_trips,
        SUM(total_amount) as daily_revenue
    FROM taxi.trips
    GROUP BY DATE(tpep_pickup_datetime)
) as daily_data
ORDER BY pickup_date
LIMIT 30;