using ClickHouse.Client.ADO;
using Dapper;
using TaxiAnalytics.Web.Models;
using System.Diagnostics;

namespace TaxiAnalytics.Web.Services
{
    public class ClickHouseTaxiDataService : ITaxiDataService
    {
        private readonly string _connectionString;

        public ClickHouseTaxiDataService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("ClickHouse")!;
        }

        public string GetDatabaseName() => "ClickHouse";

        public async Task<(T Result, double ExecutionTimeMs)> ExecuteWithTimingAsync<T>(Func<Task<T>> operation, string queryName)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await operation();
            stopwatch.Stop();
            return (result, stopwatch.Elapsed.TotalMilliseconds);
        }

        public async Task<IEnumerable<DailyStats>> GetTop10BusiestDaysAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = @"
                SELECT 
                    toDate(tpep_pickup_datetime) as PickupDate,
                    count() as TripCount,
                    sum(total_amount) as Revenue,
                    avg(trip_distance) as AvgDistance
                FROM taxi_db.trips
                GROUP BY PickupDate
                ORDER BY TripCount DESC
                LIMIT 10";

            return await connection.QueryAsync<DailyStats>(query);
        }

        public async Task<IEnumerable<PaymentTypeStats>> GetPaymentTypeDistributionAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = @"
                SELECT 
                    payment_type as PaymentType,
                    count() as Trips,
                    sum(total_amount) as TotalRevenue,
                    avg(tip_amount) as AvgTip
                FROM taxi_db.trips
                GROUP BY payment_type
                ORDER BY Trips DESC";

            return await connection.QueryAsync<PaymentTypeStats>(query);
        }

        public async Task<IEnumerable<HourlyStats>> GetHourlyPatternsAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = @"
                SELECT 
                    toHour(tpep_pickup_datetime) as Hour,
                    count() as Trips,
                    avg(passenger_count) as AvgPassengers,
                    avg(trip_distance) as AvgDistance
                FROM taxi_db.trips
                GROUP BY Hour
                ORDER BY Hour";

            return await connection.QueryAsync<HourlyStats>(query);
        }

        public async Task<IEnumerable<DistanceDistribution>> GetDistanceDistributionAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = @"
                SELECT 
                    floor(trip_distance / 5) * 5 as DistanceBucket,
                    count() as TripCount,
                    avg(total_amount) as AvgFare
                FROM taxi_db.trips
                WHERE trip_distance > 0 AND trip_distance < 100
                GROUP BY DistanceBucket
                ORDER BY DistanceBucket";

            return await connection.QueryAsync<DistanceDistribution>(query);
        }

        public async Task<IEnumerable<VendorStats>> GetVendorRevenueAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = @"
                SELECT 
                    VendorID as VendorId,
                    count() as TotalTrips,
                    sum(total_amount) as TotalRevenue,
                    avg(total_amount) as AvgRevenuePerTrip
                FROM taxi_db.trips
                GROUP BY VendorID
                ORDER BY TotalRevenue DESC";

            return await connection.QueryAsync<VendorStats>(query);
        }

        public async Task<IEnumerable<CumulativeStats>> GetCumulativeStatsAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = @"
                SELECT 
                    pickup_date as PickupDate,
                    daily_trips as DailyTrips,
                    daily_revenue as DailyRevenue,
                    sum(daily_trips) OVER (ORDER BY pickup_date) as CumulativeTrips,
                    sum(daily_revenue) OVER (ORDER BY pickup_date) as CumulativeRevenue
                FROM (
                    SELECT 
                        toDate(tpep_pickup_datetime) as pickup_date,
                        count() as daily_trips,
                        sum(total_amount) as daily_revenue
                    FROM taxi_db.trips
                    GROUP BY pickup_date
                ) as daily_data
                ORDER BY pickup_date
                LIMIT 30";

            return await connection.QueryAsync<CumulativeStats>(query);
        }

        public async Task<IEnumerable<GeospatialStats>> GetTopPickupLocationsAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = @"
                SELECT 
                    PULocationID as LocationId,
                    concat('Location ', toString(PULocationID)) as LocationName,
                    count() as TripCount,
                    avg(total_amount) as AvgFare,
                    avg(trip_distance) as AvgDistance
                FROM taxi_db.trips
                WHERE PULocationID > 0
                GROUP BY PULocationID
                ORDER BY TripCount DESC
                LIMIT 15";

            return await connection.QueryAsync<GeospatialStats>(query);
        }

        public async Task<IEnumerable<TimeSeriesData>> GetHourlyRevenueTimeSeriesAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = @"
                SELECT 
                    toStartOfHour(tpep_pickup_datetime) as TimeStamp,
                    sum(total_amount) as Value,
                    count() as Count,
                    avg(sum(total_amount)) OVER (
                        ORDER BY toStartOfHour(tpep_pickup_datetime) 
                        ROWS BETWEEN 23 PRECEDING AND CURRENT ROW
                    ) as MovingAverage
                FROM taxi_db.trips
                WHERE tpep_pickup_datetime >= subtractDays(now(), 7)
                GROUP BY TimeStamp
                ORDER BY TimeStamp
                LIMIT 168";

            return await connection.QueryAsync<TimeSeriesData>(query);
        }

        public async Task<IEnumerable<ComplexAnalytics>> GetAdvancedTripAnalyticsAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = @"
                WITH trip_categories AS (
                    SELECT 
                        CASE 
                            WHEN trip_distance <= 1 THEN 'Short (≤1mi)'
                            WHEN trip_distance <= 5 THEN 'Medium (1-5mi)'
                            WHEN trip_distance <= 15 THEN 'Long (5-15mi)'
                            ELSE 'Very Long (>15mi)'
                        END as Category,
                        total_amount,
                        trip_distance
                    FROM taxi_db.trips
                    WHERE trip_distance > 0 AND total_amount > 0
                )
                SELECT 
                    Category,
                    avg(total_amount) as Value,
                    count() as Count,
                    (count() * 100.0 / (SELECT count() FROM trip_categories)) as Percentage
                FROM trip_categories
                GROUP BY Category
                ORDER BY Value DESC";

            return await connection.QueryAsync<ComplexAnalytics>(query);
        }

        public async Task<AdvancedMetrics> GetAdvancedMetricsAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = @"
                SELECT 
                    avg(total_amount / nullIf(trip_distance, 0)) as RevenuePerMile,
                    avg(trip_distance / nullIf(date_diff('minute', tpep_pickup_datetime, tpep_dropoff_datetime), 0)) as TripEfficiency,
                    (SELECT avg(total_amount) FROM taxi_db.trips WHERE toHour(tpep_pickup_datetime) IN (7,8,17,18,19)) / 
                    (SELECT avg(total_amount) FROM taxi_db.trips WHERE toHour(tpep_pickup_datetime) NOT IN (7,8,17,18,19)) as PeakHourPremium,
                    (SELECT avg(total_amount) FROM taxi_db.trips WHERE toDayOfWeek(tpep_pickup_datetime) IN (6,7)) / 
                    (SELECT avg(total_amount) FROM taxi_db.trips WHERE toDayOfWeek(tpep_pickup_datetime) NOT IN (6,7)) as WeekendBoost,
                    count() as TotalRecords
                FROM taxi_db.trips
                WHERE trip_distance > 0 AND total_amount > 0";

            return await connection.QuerySingleAsync<AdvancedMetrics>(query);
        }

        public async Task<IEnumerable<ComplexAnalytics>> GetWeekdayVsWeekendAnalysisAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = @"
                SELECT 
                    CASE 
                        WHEN toDayOfWeek(tpep_pickup_datetime) IN (6,7) THEN 'Weekend'
                        ELSE 'Weekday'
                    END as Category,
                    avg(total_amount) as Value,
                    count() as Count,
                    (count() * 100.0 / (SELECT count() FROM taxi_db.trips)) as Percentage
                FROM taxi_db.trips
                WHERE total_amount > 0
                GROUP BY Category
                ORDER BY Value DESC";

            return await connection.QueryAsync<ComplexAnalytics>(query);
        }

        public async Task<long> GetTotalRecordCountAsync()
        {
            using var connection = new ClickHouseConnection(_connectionString);
            var query = "SELECT COUNT(*) FROM taxi_db.trips";
            return await connection.QuerySingleAsync<long>(query);
        }
    }
}