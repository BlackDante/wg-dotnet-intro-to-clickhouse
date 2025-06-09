-- Load 100 million records from NYC Taxi dataset
-- Using ClickHouse's official NYC taxi dataset

INSERT INTO taxi_db.trips
SELECT
    rand() AS trip_id,
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
    toFloat64(trip_distance) AS trip_distance,
    toFloat32(fare_amount) AS fare_amount,
    toFloat32(extra) AS extra,
    toFloat32(mta_tax) AS mta_tax,
    toFloat32(tip_amount) AS tip_amount,
    toFloat32(tolls_amount) AS tolls_amount,
    toFloat32(ehail_fee) AS ehail_fee,
    toFloat32(improvement_surcharge) AS improvement_surcharge,
    toFloat32(total_amount) AS total_amount,
    payment_type,
    trip_type,
    0 AS congestion_surcharge
FROM url(
    'https://datasets-documentation.s3.eu-west-3.amazonaws.com/nyc-taxi/trips_*.gz',
    'TabSeparatedWithNames'
)
WHERE pickup_datetime >= '2015-01-01' 
  AND pickup_datetime < '2016-01-01'
  AND toFloat64(trip_distance) > 0
  AND toFloat32(fare_amount) > 0
  AND pickup_longitude != 0
  AND pickup_latitude != 0
  AND abs(pickup_longitude) < 180
  AND abs(pickup_latitude) < 90
LIMIT 100000000;