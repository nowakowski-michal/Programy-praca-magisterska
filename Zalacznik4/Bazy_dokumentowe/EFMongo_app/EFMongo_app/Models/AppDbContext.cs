using EFMongo_app;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMongo_app.Models
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
            // Definiowanie relacji 1:1 
            modelBuilder.Entity<Pilot>()
                .HasOne(p => p.Insurance)
                .WithOne(i => i.Pilot)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade); 

            // Relacje n:m przez tabele posreniczącą
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

            // Relacja 1:N - dron i lokalizacje
            modelBuilder.Entity<Location>()
                .HasOne(l => l.Drone)
                .WithMany(d => d.Locations)
                .HasForeignKey(l => l.DroneId);

            // Relacja 1:N - dron i misje
            modelBuilder.Entity<Mission>()
                .HasOne(m => m.Drone)
                .WithMany(d => d.Missions)
                .HasForeignKey(m => m.DroneId);

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // MongoDB nie wymaga connection string w tej samej formie co SQL.
            optionsBuilder.UseMongoDB("mongodb://localhost:27017", "DBEF1");
            this.Database.AutoTransactionBehavior = AutoTransactionBehavior.Never;
        }
    }  
}
