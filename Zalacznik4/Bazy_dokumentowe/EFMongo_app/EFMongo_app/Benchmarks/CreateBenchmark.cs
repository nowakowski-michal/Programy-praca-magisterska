using BenchmarkDotNet.Attributes;
using Bogus;
using EFMongo_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMongo_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class CreateBenchmark
    {
        static AppDbContext context = new AppDbContext();

        [Params(100,1000,5000,10000)]
        public int Count { get; set; }

        // Metoda czyszcząca bazę danych przed każdą iteracją testu
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
            context.Drones.RemoveRange(context.Drones.ToList());
            context.Pilots.RemoveRange(context.Pilots.ToList());
            context.Missions.RemoveRange(context.Missions.ToList());
            context.Locations.RemoveRange(context.Locations.ToList());
            context.PilotMissions.RemoveRange(context.PilotMissions.ToList());
            context.Insurances.RemoveRange(context.Insurances.ToList());

        }

        // Benchmark dla generowania danych
        [Benchmark]
        public void GenerateAllData()
        {
            // Generowanie danych dla poszczególnych modeli
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
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            // Generowanie danych
            var drones = droneFaker.Generate(Count);
            var pilots = pilotFaker.Generate(Count);
            var missions = missionFaker.Generate(Count);
            var locations = locationFaker.Generate(Count);

            Random rand = new Random(seed);

            // Przypisanie ubezpieczeń do pilotów
            foreach (var pilot in pilots)
            {
                pilot.Insurance = insuranceFaker.Generate(1).FirstOrDefault();
            }

            // Generowanie relacji wielu do wielu dla misji i pilotów
            foreach (var mission in missions)
            {
                mission.PilotMissions = new List<PilotMission>();
                int pilotsCount = rand.Next(1, 4); 
                var availablePilots = new List<Pilot>(pilots);

                for (int i = 0; i < pilotsCount; i++)
                {
                    var randomPilot = availablePilots[rand.Next(availablePilots.Count)];
                    mission.PilotMissions.Add(new PilotMission { Pilot = randomPilot });
                    availablePilots.Remove(randomPilot);
                }
            }

            // Generowanie relacji dla dronów
            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);
            foreach (var drone in drones)
            {
                var randomLocations = availableLocations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                drone.Locations = randomLocations;
                availableLocations.RemoveAll(loc => randomLocations.Contains(loc));

                var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(3).ToList();
                drone.Missions = randomMissions;
                availableMissions.RemoveAll(m => randomMissions.Contains(m));
            }

            // Zapisanie danych do bazy
            context.Pilots.AddRange(pilots);
            context.Drones.AddRange(drones);
            context.SaveChanges();
        }
        // Benchmark dla generowania pilotów z ubezpieczeniem
        [Benchmark]
        public void GeneratePilotWithInsurance()
        {
            int seed = 12345;

            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            var pilots = pilotFaker.Generate(Count);

            if (true)
            {
                var insuranceFaker = new Faker<Insurance>()
                    .RuleFor(i => i.InsuranceProvider, f => f.Company.CompanyName())
                    .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                    .RuleFor(i => i.EndDate, f => f.Date.Future())
                    .UseSeed(seed);

                Random rand = new Random(seed);

                foreach (var pilot in pilots)
                {
                    pilot.Insurance = insuranceFaker.Generate(1).FirstOrDefault();
                }
            }

            context.Pilots.AddRange(pilots);
            context.SaveChanges();
        }

        // Benchmark dla generowania pilotów z ubezpieczeniem
        [Benchmark]
        public void GeneratePilot()
        {
            int seed = 21000;

            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            var pilots = pilotFaker.Generate(Count);

            context.Pilots.AddRange(pilots);
            context.SaveChanges();
        }
        // Benchmark dla generowania dronów z relacjami
        [Benchmark]
        public void GenerateDronesWithRelations()
        {
            int seed = 12345;

            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
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
            var missions = missionFaker.Generate(Count);
            var locations = locationFaker.Generate(Count);
            Random rand = new Random(seed);


            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);

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
            context.SaveChanges();
        }
        
    }
}
