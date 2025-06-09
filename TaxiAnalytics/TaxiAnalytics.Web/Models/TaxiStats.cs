namespace TaxiAnalytics.Web.Models
{
    public class DailyStats
    {
        public DateTime PickupDate { get; set; }
        public long TripCount { get; set; }
        public decimal Revenue { get; set; }
        public double AvgDistance { get; set; }
    }

    public class PaymentTypeStats
    {
        public int PaymentType { get; set; }
        public long Trips { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgTip { get; set; }
    }

    public class HourlyStats
    {
        public int Hour { get; set; }
        public long Trips { get; set; }
        public double AvgPassengers { get; set; }
        public double AvgDistance { get; set; }
    }

    public class DistanceDistribution
    {
        public int DistanceBucket { get; set; }
        public long TripCount { get; set; }
        public decimal AvgFare { get; set; }
    }

    public class VendorStats
    {
        public int VendorId { get; set; }
        public long TotalTrips { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AvgRevenuePerTrip { get; set; }
    }

    public class CumulativeStats
    {
        public DateTime PickupDate { get; set; }
        public long DailyTrips { get; set; }
        public decimal DailyRevenue { get; set; }
        public long CumulativeTrips { get; set; }
        public decimal CumulativeRevenue { get; set; }
    }

    public class QueryPerformance
    {
        public string QueryName { get; set; } = string.Empty;
        public double ExecutionTimeMs { get; set; }
        public string Database { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ComplexAnalytics
    {
        public string Category { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public long Count { get; set; }
        public double Percentage { get; set; }
    }

    public class GeospatialStats
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public long TripCount { get; set; }
        public decimal AvgFare { get; set; }
        public double AvgDistance { get; set; }
    }

    public class TimeSeriesData
    {
        public DateTime TimeStamp { get; set; }
        public decimal Value { get; set; }
        public long Count { get; set; }
        public double MovingAverage { get; set; }
    }

    public class AdvancedMetrics
    {
        public decimal RevenuePerMile { get; set; }
        public double TripEfficiency { get; set; }
        public decimal PeakHourPremium { get; set; }
        public double WeekendBoost { get; set; }
        public long TotalRecords { get; set; }
    }
}