namespace SmartParking.API.DTOs
{
    public class ParkingSlotDto
    {
        public int SlotId { get; set; }
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public string SlotNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? CurrentVehicleNumber { get; set; }
        public DateTime? OccupiedSince { get; set; }
    }
}
