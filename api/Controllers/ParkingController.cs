using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging; // Added for ILogger
using SmartParking.API.DTOs;
using SmartParking.API.Services;

namespace SmartParking.API.Controllers
{
    /// <summary>
    /// Manages parking-related operations: dashboard stats, slots, vehicle entry/exit, and slot initialization.
    /// All endpoints except init-slots require JWT authentication ([Authorize]).
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ParkingController : ControllerBase
    {
        // Service that encapsulates parking business logic
        private readonly IParkingService _parkingService;

        // Logger for capturing runtime information and errors
        private readonly ILogger<ParkingController> _logger;

        /// <summary>
        /// Constructor: Injects dependencies via Dependency Injection (DI).
        /// - IParkingService: Handles all parking operations (dashboard, slots, transactions).
        /// - ILogger<ParkingController>: Enables structured logging for this controller.
        /// DI decouples the controller from concrete implementations, improving testability.
        /// </summary>
        /// <param name="parkingService">Service for parking logic.</param>
        /// <param name="logger">Logger for this controller.</param>
        public ParkingController(IParkingService parkingService, ILogger<ParkingController> logger)
        {
            _parkingService = parkingService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves real-time dashboard statistics (total, occupied, available, prediction, zone stats).
        /// Requires authentication.
        /// </summary>
        /// <returns>DashboardStatsDto with current parking data.</returns>
        [HttpGet("dashboard")]
        [Authorize]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                _logger.LogInformation("Fetching dashboard statistics.");
                var stats = await _parkingService.GetDashboardStats();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching dashboard stats.");
                return StatusCode(500, new { message = "Unable to retrieve dashboard statistics at this time." });
            }
        }

        /// <summary>
        /// Retrieves the list of all parking slots with their current status.
        /// Requires authentication.
        /// </summary>
        /// <returns>List of ParkingSlotDto objects.</returns>
        [HttpGet("slots")]
        [Authorize]
        public async Task<IActionResult> GetAllSlots()
        {
            try
            {
                _logger.LogInformation("Fetching all parking slots.");
                var slots = await _parkingService.GetAllSlots();
                return Ok(slots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching parking slots.");
                return StatusCode(500, new { message = "Unable to retrieve parking slots." });
            }
        }

        /// <summary>
        /// Registers a vehicle entry into a specific parking slot.
        /// Requires authentication.
        /// </summary>
        /// <param name="entry">Contains SlotId and VehicleNumber.</param>
        /// <returns>Success or error message.</returns>
        [HttpPost("entry")]
        [Authorize]
        public async Task<IActionResult> VehicleEntry([FromBody] VehicleEntryDto entry)
        {
            try
            {
                _logger.LogInformation("Vehicle entry requested for slot {SlotId}, vehicle {VehicleNumber}",
                    entry.SlotId, entry.VehicleNumber);

                var result = await _parkingService.VehicleEntry(entry);
                if (!result)
                {
                    _logger.LogWarning("Vehicle entry failed - slot {SlotId} not available or invalid.", entry.SlotId);
                    return BadRequest(new { message = "Slot not available or invalid" });
                }

                _logger.LogInformation("Vehicle {VehicleNumber} entered slot {SlotId} successfully.",
                    entry.VehicleNumber, entry.SlotId);
                return Ok(new { message = "Vehicle entry recorded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during vehicle entry for slot {SlotId}.", entry.SlotId);
                return StatusCode(500, new { message = "An error occurred while recording vehicle entry." });
            }
        }

        /// <summary>
        /// Processes vehicle exit from a given slot, calculates duration.
        /// Requires authentication.
        /// </summary>
        /// <param name="slotId">ID of the slot to exit.</param>
        /// <returns>Success or error message.</returns>
        [HttpPost("exit/{slotId}")]
        [Authorize]
        public async Task<IActionResult> VehicleExit(int slotId)
        {
            try
            {
                _logger.LogInformation("Vehicle exit requested for slot {SlotId}.", slotId);

                var result = await _parkingService.VehicleExit(slotId);
                if (!result)
                {
                    _logger.LogWarning("Vehicle exit failed - slot {SlotId} not occupied or invalid.", slotId);
                    return BadRequest(new { message = "Slot not occupied or invalid" });
                }

                _logger.LogInformation("Vehicle exited slot {SlotId} successfully.", slotId);
                return Ok(new { message = "Vehicle exit recorded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during vehicle exit for slot {SlotId}.", slotId);
                return StatusCode(500, new { message = "An error occurred while recording vehicle exit." });
            }
        }

        /// <summary>
        /// Initializes the parking slots (creates 30 slots across 3 zones).
        /// This endpoint is public (no [Authorize]) for easy setup during deployment.
        /// If slots already exist, it returns a bad request.
        /// </summary>
        /// <returns>Success or conflict message.</returns>
        [HttpPost("init-slots")]
        // No [Authorize] - This endpoint is public for easy setup
        public async Task<IActionResult> InitializeSlots()
        {
            try
            {
                _logger.LogInformation("Attempting to initialize parking slots.");
                var result = await _parkingService.AddParkingSlots();
                if (!result)
                {
                    _logger.LogWarning("Slot initialization failed - slots already exist.");
                    return BadRequest(new { message = "Slots already initialized" });
                }

                _logger.LogInformation("Parking slots initialized successfully (30 slots created).");
                return Ok(new { message = "Parking slots initialized successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while initializing parking slots.");
                return StatusCode(500, new { message = "An error occurred while initializing slots." });
            }
        }
    }
}