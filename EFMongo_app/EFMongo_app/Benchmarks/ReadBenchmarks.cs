using BenchmarkDotNet.Attributes;
using EFMongo_app.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMongo_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class ReadBenchmark
    {
        [Params(10000)]
        public int Count { get; set; }
        static AppDbContext context = new AppDbContext();

        // Test dla relacji 1:N (Dron - Misje, Dron - Lokalizacje)
        [Benchmark]
        public void TestRead_Relacje1N()
        {
            var dronesWithMissionsAndLocations = context.Drones
                .AsEnumerable() 
                .Select(d => new
                {
                    d.DroneId,
                    d.Model,
                    // Pobieranie powiązanych misji na podstawie DroneId
                    Missions = context.Missions
                        .Where(m => m.DroneId == d.DroneId)
                        .Select(m => new
                        {
                            m.MissionId,
                            m.MissionName
                        }).ToList(),
                    // Pobieranie powiązanych lokalizacji na podstawie DroneId
                    Locations = context.Locations
                        .Where(l => l.DroneId == d.DroneId)
                        .Select(l => new
                        {
                            l.LocationId,
                            l.Altitude
                        }).ToList()
                })
                .ToList();
        }
        [Benchmark]
        public void TestRead_Relacja1_1()
        {
            // Pobieramy listę pilotów
            var pilotsWithInsurance = context.Pilots
                .AsEnumerable()
                .Select(p => new
                {
                    p.PilotId,
                    p.FirstName,
                    p.LastName,
                    p.LicenseNumber,
                    InsuranceProvider = context.Insurances
                        .FirstOrDefault(i => i.PilotId == p.PilotId)?.InsuranceProvider ?? "Brak",
                    PolicyNumber = context.Insurances
                        .FirstOrDefault(i => i.PilotId == p.PilotId)?.PolicyNumber ?? "Brak",
                    EndDate = context.Insurances
                        .FirstOrDefault(i => i.PilotId == p.PilotId)?.EndDate
                })
                .ToList();
        }
        [Benchmark]
        public void TestRead_BezRelacji()
        {
            var pilots = context.Pilots
                .Select(p => new
                {
                    p.PilotId,
                    p.FirstName,
                    p.LastName,
                    p.LicenseNumber
                })
                .ToList();
        }
        [Benchmark]
        public void TestRead_RelacjaNM()
        {
            var pilotsWithMissions = context.Pilots
                .AsEnumerable() 
                .Select(p => new
                {
                    p.PilotId,
                    p.FirstName,
                    p.LastName,
                    Missions = context.PilotMissions
                        .Where(pm => pm.PilotId == p.PilotId)
                        .Select(pm => pm.MissionId)  
                        .Distinct() 
                        .ToList()
                })
                .ToList();
        }
    }
}
