using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Bogus;
using EFNpgsql_app;  
using EFNpgsql_app.Models;  
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EFNpgsql_app.TestLoad 
{
    // Przy sprawdzaniu obciążenia zostanie wykonana tylko 1 iteracja testu, aby łatwiej zmierzyć
    // użycie zasobów przez aplikacje, dodatkowo zostaną wyświetlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 1, warmupCount: 0)]
    public class Create_Load_100k
    {
        static AppDbContext context = new AppDbContext();
        [Params(100000)]
        public int Count { get; set; }

        // Metoda czyszcząca bazę danych
        [GlobalSetup]
        public void ClearDatabase()
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            context.SaveChanges();
        }

        [IterationSetup]
        public void ClearTable()
        {
            context.Database.ExecuteSqlRaw("DELETE FROM \"Drones\"");
            context.Database.ExecuteSqlRaw("DELETE FROM \"Pilots\"");
            context.Database.ExecuteSqlRaw("DELETE FROM \"Missions\"");
            context.Database.ExecuteSqlRaw("DELETE FROM \"Locations\"");
            context.Database.ExecuteSqlRaw("DELETE FROM \"PilotMissions\"");
        }

        // Benchmark dla generowania danych
        [Benchmark]
        public void GenerateAllData()
        {
            // Ustawienie stałego seedu, aby dane były powtarzalne
            // Dane są generowane za pomocą klasy Faker z biblioteki Bogus
            int seed = 12345;
            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            var insuranceFaker = new Faker<Insurance>()
                .RuleFor(i => i.InsuranceProvider, f => f.Company.CompanyName())
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.EndDate, f => f.Date.Future())
                .UseSeed(seed);

            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past().ToUniversalTime()) 
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime).ToUniversalTime()) 
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            // Generowanie danych na podstawie przekazanych parametrów
            var drones = droneFaker.Generate(Count);
            var pilots = pilotFaker.Generate(Count);
            var missions = missionFaker.Generate(Count);
            var locations = locationFaker.Generate(Count);

            // Generowanie danych do tabel połączonych relacjami
            // Stały rand, aby odwzorowanie zawsze było takie samo
            Random rand = new Random(seed);

            // Relacja 1:1 - Przypisanie instancji dla pilota
            foreach (var pilot in pilots)
            {
                pilot.Insurance = insuranceFaker.Generate(1).FirstOrDefault();
            }

            // Relacja wiele do wielu - Przydzielanie misji do pilotów i pilotów do misji
            foreach (var mission in missions)
            {
                mission.PilotMissions = new List<PilotMission>();

                // Losowanie liczby pilotów od 1 do 3 do misji
                int pilotsCount = rand.Next(1, 4);
                var availablePilots = new List<Pilot>(pilots);

                for (int i = 0; i < pilotsCount; i++)
                {
                    var randomPilot = availablePilots[rand.Next(availablePilots.Count)];
                    mission.PilotMissions.Add(new PilotMission { Pilot = randomPilot });
                    availablePilots.Remove(randomPilot);
                }
            }

            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);

            // Relacja 1:N - W tabeli Drones przypisanie misji i lokalizacji
            foreach (var drone in drones)
            {
                var randomLocations = availableLocations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                drone.Locations = randomLocations;
                availableLocations.RemoveAll(loc => randomLocations.Contains(loc));

                var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(3).ToList();
                drone.Missions = randomMissions;
                availableMissions.RemoveAll(m => randomMissions.Contains(m));
            }

            context.Drones.AddRange(drones);
            context.Pilots.AddRange(pilots);
            context.SaveChanges();
        }
    }
}
