-- Sample queries for ClickHouse demonstration

-- 1. Top 10 busiest days
SELECT 
    toDate(tpep_pickup_datetime) as pickup_date,
    count() as trip_count,
    sum(total_amount) as revenue,
    avg(trip_distance) as avg_distance
FROM taxi_db.trips
GROUP BY pickup_date
ORDER BY trip_count DESC
LIMIT 10;

-- 2. Payment type distribution
SELECT 
    payment_type,
    count() as trips,
    sum(total_amount) as total_revenue,
    avg(tip_amount) as avg_tip
FROM taxi_db.trips
GROUP BY payment_type
ORDER BY trips DESC;

-- 3. Hourly trip patterns
SELECT 
    toHour(tpep_pickup_datetime) as hour,
    count() as trips,
    avg(passenger_count) as avg_passengers,
    avg(trip_distance) as avg_distance
FROM taxi_db.trips
GROUP BY hour
ORDER BY hour;

-- 4. Trip distance distribution
SELECT 
    floor(trip_distance / 5) * 5 as distance_bucket,
    count() as trip_count,
    avg(total_amount) as avg_fare
FROM taxi_db.trips
WHERE trip_distance > 0 AND trip_distance < 100
GROUP BY distance_bucket
ORDER BY distance_bucket;

-- 5. Revenue by vendor
SELECT 
    VendorID,
    count() as total_trips,
    sum(total_amount) as total_revenue,
    avg(total_amount) as avg_revenue_per_trip
FROM taxi_db.trips
GROUP BY VendorID
ORDER BY total_revenue DESC;

-- 6. Window function example - Running totals
SELECT 
    pickup_date,
    daily_trips,
    daily_revenue,
    sum(daily_trips) OVER (ORDER BY pickup_date) as cumulative_trips,
    sum(daily_revenue) OVER (ORDER BY pickup_date) as cumulative_revenue
FROM (
    SELECT 
        toDate(tpep_pickup_datetime) as pickup_date,
        count() as daily_trips,
        sum(total_amount) as daily_revenue
    FROM taxi_db.trips
    GROUP BY pickup_date
) as daily_data
ORDER BY pickup_date
LIMIT 30;