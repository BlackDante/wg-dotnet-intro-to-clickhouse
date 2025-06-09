using Npgsql;
using Dapper;
using TaxiAnalytics.Web.Models;
using System.Diagnostics;

namespace TaxiAnalytics.Web.Services
{
    public class PostgreSqlTaxiDataService : ITaxiDataService
    {
        private readonly string _connectionString;

        public PostgreSqlTaxiDataService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL")!;
        }

        public string GetDatabaseName() => "PostgreSQL";

        public async Task<(T Result, double ExecutionTimeMs)> ExecuteWithTimingAsync<T>(Func<Task<T>> operation, string queryName)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await operation();
            stopwatch.Stop();
            return (result, stopwatch.Elapsed.TotalMilliseconds);
        }

        public async Task<IEnumerable<DailyStats>> GetTop10BusiestDaysAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                SELECT 
                    DATE(tpep_pickup_datetime) as PickupDate,
                    COUNT(*) as TripCount,
                    SUM(total_amount) as Revenue,
                    AVG(trip_distance) as AvgDistance
                FROM taxi.trips
                GROUP BY DATE(tpep_pickup_datetime)
                ORDER BY TripCount DESC
                LIMIT 10";

            return await connection.QueryAsync<DailyStats>(query);
        }

        public async Task<IEnumerable<PaymentTypeStats>> GetPaymentTypeDistributionAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                SELECT 
                    payment_type as PaymentType,
                    COUNT(*) as Trips,
                    SUM(total_amount) as TotalRevenue,
                    AVG(tip_amount) as AvgTip
                FROM taxi.trips
                GROUP BY payment_type
                ORDER BY Trips DESC";

            return await connection.QueryAsync<PaymentTypeStats>(query);
        }

        public async Task<IEnumerable<HourlyStats>> GetHourlyPatternsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                SELECT 
                    EXTRACT(HOUR FROM tpep_pickup_datetime)::int as Hour,
                    COUNT(*) as Trips,
                    AVG(passenger_count) as AvgPassengers,
                    AVG(trip_distance) as AvgDistance
                FROM taxi.trips
                GROUP BY EXTRACT(HOUR FROM tpep_pickup_datetime)
                ORDER BY Hour";

            return await connection.QueryAsync<HourlyStats>(query);
        }

        public async Task<IEnumerable<DistanceDistribution>> GetDistanceDistributionAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                SELECT 
                    (FLOOR(trip_distance / 5) * 5)::int as DistanceBucket,
                    COUNT(*) as TripCount,
                    AVG(total_amount) as AvgFare
                FROM taxi.trips
                WHERE trip_distance > 0 AND trip_distance < 100
                GROUP BY FLOOR(trip_distance / 5) * 5
                ORDER BY DistanceBucket";

            return await connection.QueryAsync<DistanceDistribution>(query);
        }

        public async Task<IEnumerable<VendorStats>> GetVendorRevenueAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                SELECT 
                    vendorid as VendorId,
                    COUNT(*) as TotalTrips,
                    SUM(total_amount) as TotalRevenue,
                    AVG(total_amount) as AvgRevenuePerTrip
                FROM taxi.trips
                GROUP BY vendorid
                ORDER BY TotalRevenue DESC";

            return await connection.QueryAsync<VendorStats>(query);
        }

        public async Task<IEnumerable<CumulativeStats>> GetCumulativeStatsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                SELECT 
                    pickup_date as PickupDate,
                    daily_trips as DailyTrips,
                    daily_revenue as DailyRevenue,
                    SUM(daily_trips) OVER (ORDER BY pickup_date) as CumulativeTrips,
                    SUM(daily_revenue) OVER (ORDER BY pickup_date) as CumulativeRevenue
                FROM (
                    SELECT 
                        DATE(tpep_pickup_datetime) as pickup_date,
                        COUNT(*) as daily_trips,
                        SUM(total_amount) as daily_revenue
                    FROM taxi.trips
                    GROUP BY DATE(tpep_pickup_datetime)
                ) as daily_data
                ORDER BY pickup_date
                LIMIT 30";

            return await connection.QueryAsync<CumulativeStats>(query);
        }

        public async Task<IEnumerable<GeospatialStats>> GetTopPickupLocationsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                SELECT 
                    pulocationid as LocationId,
                    CONCAT('Location ', pulocationid::text) as LocationName,
                    COUNT(*) as TripCount,
                    AVG(total_amount) as AvgFare,
                    AVG(trip_distance) as AvgDistance
                FROM taxi.trips
                WHERE pulocationid > 0
                GROUP BY pulocationid
                ORDER BY TripCount DESC
                LIMIT 15";

            return await connection.QueryAsync<GeospatialStats>(query);
        }

        public async Task<IEnumerable<TimeSeriesData>> GetHourlyRevenueTimeSeriesAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                SELECT 
                    DATE_TRUNC('hour', tpep_pickup_datetime) as TimeStamp,
                    SUM(total_amount) as Value,
                    COUNT(*) as Count,
                    AVG(SUM(total_amount)) OVER (
                        ORDER BY DATE_TRUNC('hour', tpep_pickup_datetime) 
                        ROWS BETWEEN 23 PRECEDING AND CURRENT ROW
                    ) as MovingAverage
                FROM taxi.trips
                WHERE tpep_pickup_datetime >= CURRENT_DATE - INTERVAL '7 days'
                GROUP BY DATE_TRUNC('hour', tpep_pickup_datetime)
                ORDER BY TimeStamp
                LIMIT 168";

            return await connection.QueryAsync<TimeSeriesData>(query);
        }

        public async Task<IEnumerable<ComplexAnalytics>> GetAdvancedTripAnalyticsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
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
                    FROM taxi.trips
                    WHERE trip_distance > 0 AND total_amount > 0
                )
                SELECT 
                    Category,
                    AVG(total_amount) as Value,
                    COUNT(*) as Count,
                    (COUNT(*) * 100.0 / (SELECT COUNT(*) FROM trip_categories)) as Percentage
                FROM trip_categories
                GROUP BY Category
                ORDER BY Value DESC";

            return await connection.QueryAsync<ComplexAnalytics>(query);
        }

        public async Task<AdvancedMetrics> GetAdvancedMetricsAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                SELECT 
                    AVG(total_amount / NULLIF(trip_distance, 0)) as RevenuePerMile,
                    AVG(trip_distance / NULLIF(EXTRACT(EPOCH FROM (tpep_dropoff_datetime - tpep_pickup_datetime))/60, 0)) as TripEfficiency,
                    (SELECT AVG(total_amount) FROM taxi.trips WHERE EXTRACT(HOUR FROM tpep_pickup_datetime) IN (7,8,17,18,19)) / 
                    (SELECT AVG(total_amount) FROM taxi.trips WHERE EXTRACT(HOUR FROM tpep_pickup_datetime) NOT IN (7,8,17,18,19)) as PeakHourPremium,
                    (SELECT AVG(total_amount) FROM taxi.trips WHERE EXTRACT(DOW FROM tpep_pickup_datetime) IN (0,6)) / 
                    (SELECT AVG(total_amount) FROM taxi.trips WHERE EXTRACT(DOW FROM tpep_pickup_datetime) NOT IN (0,6)) as WeekendBoost,
                    COUNT(*) as TotalRecords
                FROM taxi.trips
                WHERE trip_distance > 0 AND total_amount > 0";

            return await connection.QuerySingleAsync<AdvancedMetrics>(query);
        }

        public async Task<IEnumerable<ComplexAnalytics>> GetWeekdayVsWeekendAnalysisAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = @"
                SELECT 
                    CASE 
                        WHEN EXTRACT(DOW FROM tpep_pickup_datetime) IN (0,6) THEN 'Weekend'
                        ELSE 'Weekday'
                    END as Category,
                    AVG(total_amount) as Value,
                    COUNT(*) as Count,
                    (COUNT(*) * 100.0 / (SELECT COUNT(*) FROM taxi.trips)) as Percentage
                FROM taxi.trips
                WHERE total_amount > 0
                GROUP BY Category
                ORDER BY Value DESC";

            return await connection.QueryAsync<ComplexAnalytics>(query);
        }

        public async Task<long> GetTotalRecordCountAsync()
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var query = "SELECT COUNT(*) FROM taxi.trips";
            return await connection.QuerySingleAsync<long>(query);
        }
    }
}