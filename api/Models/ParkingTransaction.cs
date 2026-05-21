using System.ComponentModel.DataAnnotations;

namespace SmartParking.API.Models
{
    public class ParkingTransaction
    {
        [Key]
        public int TransactionId { get; set; }
        
        [Required]
        public int SlotId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string VehicleNumber { get; set; } = string.Empty;
        
        [Required]
        public DateTime EntryTime { get; set; }
        
        public DateTime? ExitTime { get; set; }
        
        public int? DurationMinutes { get; set; }
        
        public decimal? Amount { get; set; }
    }
}
