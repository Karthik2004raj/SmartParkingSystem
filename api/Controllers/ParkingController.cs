using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.API.DTOs;
using SmartParking.API.Services;

namespace SmartParking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParkingController : ControllerBase
    {
        private readonly IParkingService _parkingService;
        
        public ParkingController(IParkingService parkingService)
        {
            _parkingService = parkingService;
        }
        
        [HttpGet("dashboard")]
        [Authorize]
        public async Task<IActionResult> GetDashboardStats()
        {
            var stats = await _parkingService.GetDashboardStats();
            return Ok(stats);
        }
        
        [HttpGet("slots")]
        [Authorize]
        public async Task<IActionResult> GetAllSlots()
        {
            var slots = await _parkingService.GetAllSlots();
            return Ok(slots);
        }
        
        [HttpPost("entry")]
        [Authorize]
        public async Task<IActionResult> VehicleEntry([FromBody] VehicleEntryDto entry)
        {
            var result = await _parkingService.VehicleEntry(entry);
            if (!result)
                return BadRequest(new { message = "Slot not available or invalid" });
                
            return Ok(new { message = "Vehicle entry recorded successfully" });
        }
        
        [HttpPost("exit/{slotId}")]
        [Authorize]
        public async Task<IActionResult> VehicleExit(int slotId)
        {
            var result = await _parkingService.VehicleExit(slotId);
            if (!result)
                return BadRequest(new { message = "Slot not occupied or invalid" });
                
            return Ok(new { message = "Vehicle exit recorded successfully" });
        }
        
        [HttpPost("init-slots")]
        // No [Authorize] - This endpoint is public for easy setup
        public async Task<IActionResult> InitializeSlots()
        {
            var result = await _parkingService.AddParkingSlots();
            if (!result)
                return BadRequest(new { message = "Slots already initialized" });
                
            return Ok(new { message = "Parking slots initialized successfully" });
        }
    }
}
