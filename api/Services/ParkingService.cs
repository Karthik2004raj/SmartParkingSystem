using Microsoft.EntityFrameworkCore;
using SmartParking.API.Data;
using SmartParking.API.DTOs;
using SmartParking.API.Models;

namespace SmartParking.API.Services
{
    public interface IParkingService
    {
        Task<DashboardStatsDto> GetDashboardStats();
        Task<List<ParkingSlotDto>> GetAllSlots();
        Task<bool> VehicleEntry(VehicleEntryDto entry);
        Task<bool> VehicleExit(int slotId);
        Task<int> GetPredictedOccupancy();
        Task<bool> AddParkingSlots();
    }
    
    public class ParkingService : IParkingService
    {
        private readonly ApplicationDbContext _context;
        
        public ParkingService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<bool> AddParkingSlots()
        {
            //if (await _context.ParkingSlots.AnyAsync())
               // return false;
            
           var zones = await _context.ParkingZones.ToListAsync();
           var slots = new List<ParkingSlot>();
            
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
            
            if (slots.Count > 0)
            {
                slots[0].Status = "Occupied";
                slots[0].CurrentVehicleNumber = "KA01AB1234";
                slots[0].OccupiedSince = DateTime.UtcNow;
                
                slots[1].Status = "Occupied";
                slots[1].CurrentVehicleNumber = "KA02CD5678";
                slots[1].OccupiedSince = DateTime.UtcNow;
            }
            
            await _context.ParkingSlots.AddRangeAsync(slots);
            await _context.SaveChangesAsync();
            return true;
        }
        
        public async Task<DashboardStatsDto> GetDashboardStats()
        {
            var totalSlots = await _context.ParkingSlots.CountAsync();
            var occupiedSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Occupied");
            var reservedSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Reserved");
            var maintenanceSlots = await _context.ParkingSlots.CountAsync(s => s.Status == "Maintenance");
            var availableSlots = totalSlots - occupiedSlots - reservedSlots - maintenanceSlots;
            
            var utilizationPercentage = totalSlots > 0 ? (decimal)occupiedSlots / totalSlots * 100 : 0;
            
            var congestionLevel = utilizationPercentage switch
            {
                >= 90 => "Critical",
                >= 80 => "High",
                >= 60 => "Medium",
                >= 30 => "Low",
                _ => "Very Low"
            };
            
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
        
        public async Task<List<ParkingSlotDto>> GetAllSlots()
        {
            var slots = await _context.ParkingSlots
                .Include(s => s.Zone)
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
                
            return slots;
        }
        
        public async Task<bool> VehicleEntry(VehicleEntryDto entry)
        {
            var slot = await _context.ParkingSlots.FindAsync(entry.SlotId);
            if (slot == null || slot.Status != "Available")
                return false;
                
            slot.Status = "Occupied";
            slot.CurrentVehicleNumber = entry.VehicleNumber;
            slot.OccupiedSince = DateTime.UtcNow;
            
            var transaction = new ParkingTransaction
            {
                SlotId = entry.SlotId,
                VehicleNumber = entry.VehicleNumber,
                EntryTime = DateTime.UtcNow
            };
            
            _context.ParkingTransactions.Add(transaction);
            await _context.SaveChangesAsync();
            await RecordOccupancyHistory();
            return true;
        }
        
        public async Task<bool> VehicleExit(int slotId)
        {
            var slot = await _context.ParkingSlots.FindAsync(slotId);
            if (slot == null || slot.Status != "Occupied")
                return false;
                
            var transaction = await _context.ParkingTransactions
                .Where(t => t.SlotId == slotId && t.ExitTime == null)
                .FirstOrDefaultAsync();
                
            if (transaction != null)
            {
                transaction.ExitTime = DateTime.UtcNow;
                transaction.DurationMinutes = (int)(DateTime.UtcNow - transaction.EntryTime).TotalMinutes;
            }
            
            slot.Status = "Available";
            slot.CurrentVehicleNumber = null;
            slot.OccupiedSince = null;
            
            await _context.SaveChangesAsync();
            await RecordOccupancyHistory();
            return true;
        }
        
        public async Task<int> GetPredictedOccupancy()
        {
            var now = DateTime.UtcNow;
            var currentHour = now.Hour;
            var currentDayOfWeek = now.DayOfWeek.ToString();
            var currentOccupancy = await _context.ParkingSlots.CountAsync(s => s.Status == "Occupied");
            var totalSlots = await _context.ParkingSlots.CountAsync();
            
            if (totalSlots == 0) return 0;
            
            var last3HourAvg = await _context.OccupancyHistories
                .Where(h => h.RecordedAt >= now.AddHours(-3))
                .AverageAsync(h => (double)h.OccupiedSlots);
                
            var sameHourAvg = await _context.OccupancyHistories
                .Where(h => h.Hour == currentHour && h.DayOfWeek == currentDayOfWeek)
                .AverageAsync(h => (double)h.OccupiedSlots);
                
            var predicted = (0.5 * currentOccupancy) + 
                           (0.3 * (last3HourAvg > 0 ? last3HourAvg : currentOccupancy)) + 
                           (0.2 * (sameHourAvg > 0 ? sameHourAvg : currentOccupancy));
                           
            return Math.Min(totalSlots, Math.Max(0, (int)predicted));
        }
        
        private async Task RecordOccupancyHistory()
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
        }
    }
}
