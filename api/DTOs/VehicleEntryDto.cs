using System.ComponentModel.DataAnnotations;

namespace SmartParking.API.DTOs
{
    public class VehicleEntryDto
    {
        [Required]
        public int SlotId { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string VehicleNumber { get; set; } = string.Empty;
    }
}
