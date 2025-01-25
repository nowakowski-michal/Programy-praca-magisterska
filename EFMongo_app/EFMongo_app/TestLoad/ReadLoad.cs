using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using EFMongo_app;
using EFMongo_app.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMongo_app.TestLoad
{
    //przy sprawdzaniu obciążenia zostanie wykonana tylko 3 iteracja testu oraz dwie rozgrzewające, 
    //aby łatwiej zmierzyć użycie zasobów przez aplikacje, dodatkowo zostana wyswietlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 3, warmupCount: 2)]
    public class Read_Load
    {
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
                .AsEnumerable() // Zatrzymujemy zapytanie, aby kolejne operacje były wykonywane w pamięci
                .Select(p => new
                {
                    p.PilotId,
                    p.FirstName,
                    p.LastName,
                    p.LicenseNumber,
                    // Pobierz ubezpieczenie ręcznie na podstawie Insurance.PilotId
                    InsuranceProvider = context.Insurances
                        .FirstOrDefault(i => i.PilotId == p.PilotId)?.InsuranceProvider ?? "Brak",
                    PolicyNumber = context.Insurances
                        .FirstOrDefault(i => i.PilotId == p.PilotId)?.PolicyNumber ?? "Brak",
                    EndDate = context.Insurances
                        .FirstOrDefault(i => i.PilotId == p.PilotId)?.EndDate
                })
                .ToList(); // Wykonanie zapytania i pobranie danych do pamięci
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
                .AsEnumerable() // Przekształcamy zapytanie, aby pozostałe operacje odbywały się w pamięci
                .Select(p => new
                {
                    p.PilotId,
                    p.FirstName,
                    p.LastName,
                    // Pobieramy misje związane z danym PilotId przez tabelę pośrednią PilotMission
                    Missions = context.PilotMissions
                        .Where(pm => pm.PilotId == p.PilotId)
                        .Select(pm => pm.MissionId)  // Pobieramy tylko ID misji
                        .Distinct()  // Zapewniamy, że misje są unikalne
                        .ToList()
                })
                .ToList();
        }
    }
}
