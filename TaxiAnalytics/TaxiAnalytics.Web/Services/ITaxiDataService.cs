using TaxiAnalytics.Web.Models;

namespace TaxiAnalytics.Web.Services
{
    public interface ITaxiDataService
    {
        Task<IEnumerable<DailyStats>> GetTop10BusiestDaysAsync();
        Task<IEnumerable<PaymentTypeStats>> GetPaymentTypeDistributionAsync();
        Task<IEnumerable<HourlyStats>> GetHourlyPatternsAsync();
        Task<IEnumerable<DistanceDistribution>> GetDistanceDistributionAsync();
        Task<IEnumerable<VendorStats>> GetVendorRevenueAsync();
        Task<IEnumerable<CumulativeStats>> GetCumulativeStatsAsync();
        
        // New advanced analytics queries
        Task<IEnumerable<GeospatialStats>> GetTopPickupLocationsAsync();
        Task<IEnumerable<TimeSeriesData>> GetHourlyRevenueTimeSeriesAsync();
        Task<IEnumerable<ComplexAnalytics>> GetAdvancedTripAnalyticsAsync();
        Task<AdvancedMetrics> GetAdvancedMetricsAsync();
        Task<IEnumerable<ComplexAnalytics>> GetWeekdayVsWeekendAnalysisAsync();
        
        // Performance tracking
        Task<(T Result, double ExecutionTimeMs)> ExecuteWithTimingAsync<T>(Func<Task<T>> operation, string queryName);
        string GetDatabaseName();
        
        // Get total record count
        Task<long> GetTotalRecordCountAsync();
    }
}