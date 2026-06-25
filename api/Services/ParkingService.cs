using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // Added for ILogger
using SmartParking.API.Data;
using SmartParking.API.DTOs;
using SmartParking.API.Models;

namespace SmartParking.API.Services
{
    /// <summary>
    /// Defines parking-related business operations: dashboard stats, slot management, vehicle entry/exit, predictions.
    /// </summary>
    public interface IParkingService
    {
        Task<DashboardStatsDto> GetDashboardStats();
        Task<List<ParkingSlotDto>> GetAllSlots();
        Task<bool> VehicleEntry(VehicleEntryDto entry);
        Task<bool> VehicleExit(int slotId);
        Task<int> GetPredictedOccupancy();
        Task<bool> AddParkingSlots();
    }

    /// <summary>
    /// Implementation of IParkingService using Entity Framework Core and SQLite/MySQL.
    /// Handles all parking business logic with comprehensive error handling.
    /// </summary>
    public class ParkingService : IParkingService
    {
        // Database context for all database operations
        private readonly ApplicationDbContext _context;

        // Logger for capturing runtime information and errors
        private readonly ILogger<ParkingService> _logger;

        /// <summary>
        /// Constructor: Injects dependencies via Dependency Injection (DI).
        /// - ApplicationDbContext: Provides database access (DbContext).
        /// - ILogger<ParkingService>: Enables structured logging for this service.
        /// DI decouples the service from concrete implementations, improving testability.
        /// </summary>
        /// <param name="context">Database context for all queries and updates.</param>
        /// <param name="logger">Logger for this service.</param>
        public ParkingService(ApplicationDbContext context, ILogger<ParkingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Initializes parking slots: creates 30 slots (10 per zone, across 3 zones).
        /// Two slots are pre-occupied for demo purposes (KA01AB1234, KA02CD5678).
        /// </summary>
        /// <returns>True if slots were created successfully; false if slots already exist.</returns>
        public async Task<bool> AddParkingSlots()
        {
            try
            {
                _logger.LogInformation("Attempting to initialize parking slots.");

                // Check if slots already exist to prevent duplication
                if (await _context.ParkingSlots.AnyAsync())
                {
                    _logger.LogWarning("Slot initialization aborted: slots already exist.");
                    return false; // Return false to indicate they already exist
                }

                // Fetch existing zones from database (A, B, C)
                var zones = await _context.ParkingZones.ToListAsync();
                if (zones == null || zones.Count == 0)
                {
                    _logger.LogError("No parking zones found in the database. Please seed zones first.");
                    throw new InvalidOperationException("Parking zones are not configured. Please seed the database.");
                }

                var slots = new List<ParkingSlot>();

                // Create 10 slots per zone (e.g., A1–A10, B1–B10, C1–C10)
                foreach (var zone in zones)
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        slots.Add(new ParkingSlot
                        {
                            ZoneId = zone.ZoneId,
                            SlotNumber = $"{zone.ZoneName}{i}",
                            Status = "Available"
                        });
                    }
                }

                // Mark first two slots as occupied for demo purposes
                if (slots.Count > 0)
                {
                    slots[0].Status = "Occupied";
                    slots[0].CurrentVehicleNumber = "KA01AB1234";
                    slots[0].OccupiedSince = DateTime.UtcNow;

                    slots[1].Status = "Occupied";
                    slots[1].CurrentVehicleNumber = "KA02CD5678";
                    slots[1].OccupiedSince = DateTime.UtcNow;
                }

                // Add all slots to the database in one batch
                await _context.ParkingSlots.AddRangeAsync(slots);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully initialized {Count} parking slots.", slots.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while initializing parking slots.");
                // Rethrow with a clear message for the controller to handle
                throw new Exception("Failed to initialize parking slots. Please check the database configuration.", ex);
            }
        }

        /// <summary>
        /// Retrieves real-time dashboard statistics.
        /// </summary>
        /// <returns>DashboardStatsDto with total, occupied, available, prediction, and zone stats.</returns>
        public async Task<DashboardStatsDto> GetDashboardStats()
        {
            try
            {
                _logger.LogInformation("Fetching dashboard statistics.");

                // Get all slot counts in parallel for better performance
                var totalSlots = await _context.ParkingSlots.CountAsync();
                var occupiedSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Occupied");
                var reservedSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Reserved");
                var maintenanceSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Maintenance");

                // Calculate derived values
                var availableSlots = totalSlots - occupiedSlots - reservedSlots - maintenanceSlots;
                var utilizationPercentage = totalSlots > 0 ? (decimal)occupiedSlots / totalSlots * 100 : 0;

                // Determine congestion level based on utilization
                var congestionLevel = utilizationPercentage switch
                {
                    >= 90 => "Critical",
                    >= 80 => "High",
                    >= 60 => "Medium",
                    >= 30 => "Low",
                    _ => "Very Low"
                };

                // Get per-zone statistics with a single query (includes navigation property)
                var zoneStats = await _context.ParkingZones
                    .Include(z => z.ParkingSlots)
                    .Select(z => new ZoneStatDto
                    {
                        ZoneName = z.ZoneName,
                        TotalSlots = z.ParkingSlots.Count,
                        OccupiedSlots = z.ParkingSlots.Count(s => s.Status == "Occupied"),
                        AvailableSlots = z.ParkingSlots.Count(s => s.Status == "Available"),
                        OccupancyRate = z.ParkingSlots.Count > 0 ?
                            (decimal)z.ParkingSlots.Count(s => s.Status == "Occupied") / z.ParkingSlots.Count * 100 : 0
                    })
                    .ToListAsync();

                // Get AI prediction for next hour
                var predictedOccupancy = await GetPredictedOccupancy();

                return new DashboardStatsDto
                {
                    TotalSlots = totalSlots,
                    OccupiedSlots = occupiedSlots,
                    AvailableSlots = availableSlots,
                    ReservedSlots = reservedSlots,
                    MaintenanceSlots = maintenanceSlots,
                    UtilizationPercentage = utilizationPercentage,
                    CongestionLevel = congestionLevel,
                    PredictedNextHourOccupancy = predictedOccupancy,
                    ZoneStats = zoneStats
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching dashboard statistics.");
                // Rethrow for the controller to handle and return a user-friendly message
                throw new Exception("Unable to retrieve dashboard statistics. Please check the database connection.", ex);
            }
        }

        /// <summary>
        /// Retrieves all parking slots with their current status and associated zone info.
        /// </summary>
        /// <returns>List of ParkingSlotDto objects.</returns>
        public async Task<List<ParkingSlotDto>> GetAllSlots()
        {
            try
            {
                _logger.LogInformation("Fetching all parking slots.");

                var slots = await _context.ParkingSlots
                    .Include(s => s.Zone) // Eager load zone data to avoid N+1 queries
                    .Select(s => new ParkingSlotDto
                    {
                        SlotId = s.SlotId,
                        ZoneId = s.ZoneId,
                        ZoneName = s.Zone != null ? s.Zone.ZoneName : "",
                        SlotNumber = s.SlotNumber,
                        Status = s.Status,
                        CurrentVehicleNumber = s.CurrentVehicleNumber,
                        OccupiedSince = s.OccupiedSince
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} parking slots.", slots.Count);
                return slots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching parking slots.");
                throw new Exception("Unable to retrieve parking slots. Please try again later.", ex);
            }
        }

        /// <summary>
        /// Registers a vehicle entry into a specific parking slot.
        /// </summary>
        /// <param name="entry">Contains SlotId and VehicleNumber.</param>
        /// <returns>True if entry was successful; false if slot is unavailable or invalid.</returns>
        public async Task<bool> VehicleEntry(VehicleEntryDto entry)
        {
            try
            {
                _logger.LogInformation("Processing vehicle entry for slot {SlotId}, vehicle {VehicleNumber}.",
                    entry.SlotId, entry.VehicleNumber);

                // Find the slot; if not found or not available, return false
                var slot = await _context.ParkingSlots.FindAsync(entry.SlotId);
                if (slot == null || slot.Status != "Available")
                {
                    _logger.LogWarning("Vehicle entry failed: slot {SlotId} not available or invalid.", entry.SlotId);
                    return false;
                }

                // Update slot status to occupied
                slot.Status = "Occupied";
                slot.CurrentVehicleNumber = entry.VehicleNumber;
                slot.OccupiedSince = DateTime.UtcNow;

                // Create a new transaction record
                var transaction = new ParkingTransaction
                {
                    SlotId = entry.SlotId,
                    VehicleNumber = entry.VehicleNumber,
                    EntryTime = DateTime.UtcNow
                };

                _context.ParkingTransactions.Add(transaction);

                // Save changes and record occupancy history
                await _context.SaveChangesAsync();
                await RecordOccupancyHistory();

                _logger.LogInformation("Vehicle {VehicleNumber} entered slot {SlotId} successfully.",
                    entry.VehicleNumber, entry.SlotId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during vehicle entry for slot {SlotId}.", entry.SlotId);
                // Rethrow so the controller can handle it
                throw new Exception("An error occurred while recording vehicle entry.", ex);
            }
        }

        /// <summary>
        /// Processes vehicle exit from a given slot and calculates the parking duration.
        /// </summary>
        /// <param name="slotId">ID of the slot to exit.</param>
        /// <returns>True if exit was successful; false if slot is not occupied or invalid.</returns>
        public async Task<bool> VehicleExit(int slotId)
        {
            try
            {
                _logger.LogInformation("Processing vehicle exit for slot {SlotId}.", slotId);

                // Find the slot; if not found or not occupied, return false
                var slot = await _context.ParkingSlots.FindAsync(slotId);
                if (slot == null || slot.Status != "Occupied")
                {
                    _logger.LogWarning("Vehicle exit failed: slot {SlotId} not occupied or invalid.", slotId);
                    return false;
                }

                // Find the active transaction (without exit time)
                var transaction = await _context.ParkingTransactions
                    .Where(t => t.SlotId == slotId && t.ExitTime == null)
                    .FirstOrDefaultAsync();

                if (transaction != null)
                {
                    // Calculate duration and mark exit time
                    transaction.ExitTime = DateTime.UtcNow;
                    transaction.DurationMinutes = (int)(DateTime.UtcNow - transaction.EntryTime).TotalMinutes;
                }
                else
                {
                    _logger.LogWarning("No active transaction found for slot {SlotId}.", slotId);
                }

                // Free the slot
                slot.Status = "Available";
                slot.CurrentVehicleNumber = null;
                slot.OccupiedSince = null;

                await _context.SaveChangesAsync();
                await RecordOccupancyHistory();

                _logger.LogInformation("Vehicle exited slot {SlotId} successfully.", slotId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during vehicle exit for slot {SlotId}.", slotId);
                throw new Exception("An error occurred while recording vehicle exit.", ex);
            }
        }

        /// <summary>
        /// Predicts next-hour occupancy using a weighted linear regression formula:
        /// 0.5 × CurrentOccupancy + 0.3 × Last3HourAvg + 0.2 × SameHourHistoricalAvg
        /// </summary>
        /// <returns>Predicted number of occupied slots for the next hour.</returns>
        public async Task<int> GetPredictedOccupancy()
        {
            try
            {
                var now = DateTime.UtcNow;
                var currentHour = now.Hour;
                var currentDayOfWeek = now.DayOfWeek.ToString();

                // Get current occupancy
                var currentOccupancy = await _context.ParkingSlots.CountAsync(s => s.Status == "Occupied");
                var totalSlots = await _context.ParkingSlots.CountAsync();

                if (totalSlots == 0) return 0;

                // Calculate last 3 hours average (fallback to current occupancy if no data)
                var last3HourAvg = await _context.OccupancyHistories
                    .Where(h => h.RecordedAt >= now.AddHours(-3))
                    .AverageAsync(h => (double?)h.OccupiedSlots) ?? currentOccupancy;

                // Calculate same hour historical average (fallback to current occupancy)
                var sameHourAvg = await _context.OccupancyHistories
                    .Where(h => h.Hour == currentHour && h.DayOfWeek == currentDayOfWeek)
                    .AverageAsync(h => (double?)h.OccupiedSlots) ?? currentOccupancy;

                // Weighted formula: 50% current, 30% recent trend, 20% historical pattern
                var predicted = (0.5 * currentOccupancy) +
                               (0.3 * (last3HourAvg > 0 ? last3HourAvg : currentOccupancy)) +
                               (0.2 * (sameHourAvg > 0 ? sameHourAvg : currentOccupancy));

                // Clamp the prediction between 0 and total slots
                return Math.Min(totalSlots, Math.Max(0, (int)predicted));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Prediction failed, falling back to current occupancy.");
                // Fallback: return current occupancy if prediction fails
                return await _context.ParkingSlots.CountAsync(s => s.Status == "Occupied");
            }
        }

        /// <summary>
        /// Records a snapshot of current occupancy for historical analysis and AI predictions.
        /// Called after every vehicle entry/exit.
        /// </summary>
        private async Task RecordOccupancyHistory()
        {
            try
            {
                var totalSlots = await _context.ParkingSlots.CountAsync();
                if (totalSlots == 0) return;

                var occupiedSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Occupied");
                var now = DateTime.UtcNow;

                var history = new OccupancyHistory
                {
                    RecordedAt = now,
                    TotalSlots = totalSlots,
                    OccupiedSlots = occupiedSlots,
                    AvailableSlots = totalSlots - occupiedSlots,
                    OccupancyPercent = (decimal)occupiedSlots / totalSlots * 100,
                    Hour = now.Hour,
                    DayOfWeek = now.DayOfWeek.ToString()
                };

                _context.OccupancyHistories.Add(history);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Occupancy history recorded: {Occupied}/{Total} at {Time}.",
                    occupiedSlots, totalSlots, now.ToLocalTime());
            }
            catch (Exception ex)
            {
                // Non-critical operation: log but don't throw (history recording shouldn't break the main flow)
                _logger.LogError(ex, "Failed to record occupancy history. This does not affect core operations.");
            }
        }
    }
}