using Microsoft.EntityFrameworkCore;
using SmartParking.API.Models;

namespace SmartParking.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
        public DbSet<ParkingZone> ParkingZones { get; set; }
        public DbSet<ParkingSlot> ParkingSlots { get; set; }
        public DbSet<ParkingTransaction> ParkingTransactions { get; set; }
        public DbSet<OccupancyHistory> OccupancyHistories { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            string adminHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            
            modelBuilder.Entity<User>().HasData(
                new User { UserId = 1, Username = "admin", PasswordHash = adminHash, Role = "Admin", IsActive = true },
                new User { UserId = 2, Username = "operator", PasswordHash = adminHash, Role = "Operator", IsActive = true },
                new User { UserId = 3, Username = "viewer", PasswordHash = adminHash, Role = "Viewer", IsActive = true }
            );
            
            modelBuilder.Entity<ParkingZone>().HasData(
                new ParkingZone { ZoneId = 1, ZoneName = "A", Description = "Zone A - Main Building" },
                new ParkingZone { ZoneId = 2, ZoneName = "B", Description = "Zone B - East Wing" },
                new ParkingZone { ZoneId = 3, ZoneName = "C", Description = "Zone C - West Wing" }
            );
        }
    }
}
