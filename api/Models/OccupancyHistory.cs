using System.ComponentModel.DataAnnotations;

namespace SmartParking.API.Models
{
    public class OccupancyHistory
    {
        [Key]
        public int HistoryId { get; set; }
        
        [Required]
        public DateTime RecordedAt { get; set; }
        
        [Required]
        public int TotalSlots { get; set; }
        
        [Required]
        public int OccupiedSlots { get; set; }
        
        [Required]
        public int AvailableSlots { get; set; }
        
        public decimal OccupancyPercent { get; set; }
        
        public int Hour { get; set; }
        
        public string? DayOfWeek { get; set; }
    }
}
