using System.ComponentModel.DataAnnotations;

namespace SmartParking.API.Models
{
    public class ParkingZone
    {
        [Key]
        public int ZoneId { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string ZoneName { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<ParkingSlot> ParkingSlots { get; set; } = new List<ParkingSlot>();
    }
}
