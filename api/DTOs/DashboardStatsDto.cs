namespace SmartParking.API.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalSlots { get; set; }
        public int OccupiedSlots { get; set; }
        public int AvailableSlots { get; set; }
        public int ReservedSlots { get; set; }
        public int MaintenanceSlots { get; set; }
        public decimal UtilizationPercentage { get; set; }
        public string CongestionLevel { get; set; } = string.Empty;
        public int PredictedNextHourOccupancy { get; set; }
        public List<ZoneStatDto> ZoneStats { get; set; } = new();
    }
    
    public class ZoneStatDto
    {
        public string ZoneName { get; set; } = string.Empty;
        public int TotalSlots { get; set; }
        public int OccupiedSlots { get; set; }
        public int AvailableSlots { get; set; }
        public decimal OccupancyRate { get; set; }
    }
}
