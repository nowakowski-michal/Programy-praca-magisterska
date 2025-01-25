using EFNpgsql_app;
using Microsoft.EntityFrameworkCore;
using System;

namespace EFNpgsql_app.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<Drone> Drones { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<Mission> Missions { get; set; }
        public DbSet<Pilot> Pilots { get; set; }
        public DbSet<Insurance> Insurances { get; set; }
        public DbSet<PilotMission> PilotMissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //konfiguracja zapisu dat
            modelBuilder.Entity<Location>()
               .Property(l => l.Timestamp)
               .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            modelBuilder.Entity<Mission>()
                .Property(m => m.StartTime)
                .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            modelBuilder.Entity<Mission>()
                .Property(m => m.EndTime)
                .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            modelBuilder.Entity<Insurance>()
                .Property(i => i.EndDate)
                .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            // Konfiguracja relacji 1:1
            modelBuilder.Entity<Pilot>()
                .HasOne(p => p.Insurance)
                .WithOne(i => i.Pilot)
                .HasForeignKey<Insurance>(i => i.PilotId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            // Konfiguracja relacji n:m przez tabelę pośredniczącą
            modelBuilder.Entity<PilotMission>()
                .HasKey(pm => new { pm.PilotId, pm.MissionId });

            modelBuilder.Entity<PilotMission>()
                .HasOne(pm => pm.Pilot)
                .WithMany(p => p.PilotMissions)
                .HasForeignKey(pm => pm.PilotId);

            modelBuilder.Entity<PilotMission>()
                .HasOne(pm => pm.Mission)
                .WithMany(m => m.PilotMissions)
                .HasForeignKey(pm => pm.MissionId);

            // Konfiguracja relacji 1:wielu dron-lokalizacje
            modelBuilder.Entity<Location>()
                .HasOne(l => l.Drone)
                .WithMany(d => d.Locations)
                .HasForeignKey(l => l.DroneId);

            // Konfiguracja relacji 1:wielu dron-misje
            modelBuilder.Entity<Mission>()
                .HasOne(m => m.Drone)
                .WithMany(d => d.Missions)
                .HasForeignKey(m => m.DroneId);

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Connection string do PostgreSQL
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=DronyEF1;Username=postgres;Password=admin1");
        }
    }
}
