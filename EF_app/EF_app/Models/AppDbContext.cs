using Ef_app;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ef_app.Models
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
            //Konfiguracja relacji 1:1
            modelBuilder.Entity<Pilot>()
                .HasOne(p => p.Insurance)
                .WithOne(i => i.Pilot)
                .HasForeignKey<Insurance>(i => i.PilotId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            //Konfiguracja relacji n:m przez tabele posreniczącą
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

            // konfiguracja relacji 1:wielu dron-loklaizacje
            modelBuilder.Entity<Location>()
                .HasOne(l => l.Drone)
                .WithMany(d => d.Locations)
                .HasForeignKey(l => l.DroneId);

            //konfiguracja relacji 1-wielu dron-misje
            modelBuilder.Entity<Mission>()
                .HasOne(m => m.Drone)
                .WithMany(d => d.Missions)
                .HasForeignKey(m => m.DroneId);


            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //connection string do połaczenia z bazą danych
            optionsBuilder.UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Baza_ef_test;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False");
        }
    }
}
