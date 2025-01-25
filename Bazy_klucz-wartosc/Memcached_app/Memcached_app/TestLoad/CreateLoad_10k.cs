using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Bogus;
using Enyim.Caching.Memcached;
using Enyim.Caching;
using Memcached_app.Models;

using System;
using System.Data;
using Newtonsoft.Json;
namespace Memcached_app.TestLoad
{
    //przy sprawdzaniu obciążenia zostanie wykonana tylko 1 iteracja testu, aby łatwiej zmierzyć
    // użycie zasobów przez aplikacje, dodatkowo zostana wyswietlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 1, warmupCount: 0)]
    public class Create_Load_10k
    {
        private IMemcachedClient _memcachedClient;
        private List<Drone> drones = new List<Drone>();
        private List<Insurance> insurances = new List<Insurance>();
        private List<Pilot> pilots = new List<Pilot>();
        private List<Mission> missions = new List<Mission>();
        private List<Location> locations = new List<Location>();

        [Params(10000)]
        public int Count { get; set; }
        [GlobalSetup]
        public void Setup()
        {
            _memcachedClient = AppDbContext.MemcachedClient;
            int seed = 12345;

            // Faker dla klasy Drone
            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.DroneId, f => f.IndexGlobal)
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            drones = droneFaker.Generate(Count);

            // Faker dla klasy Insurance
            var insuranceFaker = new Faker<Insurance>()
                .RuleFor(i => i.InsuranceId, f => f.IndexGlobal)
                .RuleFor(i => i.InsuranceProvider, f => f.Company.CompanyName())
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.EndDate, f => f.Date.Future())
                .UseSeed(seed);

            insurances = insuranceFaker.Generate(Count);

            // Faker dla klasy Pilot
            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.PilotId, f => f.IndexGlobal)
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            pilots = pilotFaker.Generate(Count);

            // Faker dla klasy Mission
            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionId, f => f.IndexGlobal)
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            missions = missionFaker.Generate(Count);

            // Faker dla klasy Location
            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.LocationId, f => f.IndexGlobal)
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            locations = locationFaker.Generate(Count);

        }
        [IterationSetup]
        public void CleanDatabase()
        {
            _memcachedClient.FlushAll();
        }
        [Benchmark]
        public void GenerateAllData()
        {
            CleanDatabase();

            int seed = 12345;

            Random rand = new Random(seed);

            // Przypisywanie misji i lokalizacji do dronów
            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);
            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].InsuranceId = insurances[i].InsuranceId;
                insurances[i].PilotId = pilots[i].PilotId;
            }

            // Przypisywanie misji do dronów
            foreach (var drone in drones)
            {
                var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(rand.Next(0, 3)).ToList();
                drone.MissionIds = randomMissions.Select(m => m.MissionId).ToList();

                foreach (var mission in randomMissions)
                {
                    var missionKey = $"Mission:{mission.MissionId}";
                    var missionJson = JsonConvert.SerializeObject(mission);
                    _memcachedClient.Store(StoreMode.Set, missionKey, missionJson);
                    AppDbContext.AddKeyToCategory("Mission", missionKey);

                }
            }
            // Przypisywanie lokalizacji do dronów
            foreach (var drone in drones)
            {
                var randomLocations = availableLocations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                drone.LocationIds = randomLocations.Select(l => l.LocationId).ToList();
                availableLocations.RemoveAll(l => randomLocations.Contains(l));
                foreach (var location in randomLocations)
                {
                    var locationKey = $"Location:{location.LocationId}";
                    var locationJson = JsonConvert.SerializeObject(location);
                    _memcachedClient.Store(StoreMode.Set, locationKey, locationJson);
                    AppDbContext.AddKeyToCategory("Location", locationKey);

                }
            }

            // Dodawanie pilota i przypisanie pilota do misji
            foreach (var pilot in pilots)
            {
                var pilotKey = $"Pilot:{pilot.PilotId}";
                var pilotJson = JsonConvert.SerializeObject(pilot);
                _memcachedClient.Store(StoreMode.Set, pilotKey, pilotJson);
                AppDbContext.AddKeyToCategory("Pilot", pilotKey);
                var randomMissionsForPilot = availableMissions.OrderBy(m => rand.Next()).Take(rand.Next(0, 3)).ToList();

                foreach (var mission in randomMissionsForPilot)
                {
                    var pilotMissionKey = $"PilotMission:{pilot.PilotId}:{mission.MissionId}";
                    var pilotMission = new { PilotId = pilot.PilotId, MissionId = mission.MissionId };
                    var pilotMissionJson = JsonConvert.SerializeObject(pilotMission);
                    _memcachedClient.Store(StoreMode.Set, pilotMissionKey, pilotMissionJson);
                    AppDbContext.AddKeyToCategory("PilotMission", pilotMissionKey);
                }
            }

            // Dodawanie ubezpieczenia
            foreach (var insurance in insurances)
            {
                var insuranceKey = $"Insurance:{insurance.InsuranceId}";
                var insuranceJson = JsonConvert.SerializeObject(insurance);
                _memcachedClient.Store(StoreMode.Set, insuranceKey, insuranceJson);
                AppDbContext.AddKeyToCategory("Insurance", insuranceKey);
            }

            // Dodawanie dronów do Memcached
            foreach (var drone in drones)
            {
                var droneKey = $"Drone:{drone.DroneId}";
                var droneJson = JsonConvert.SerializeObject(drone);
                _memcachedClient.Store(StoreMode.Set, droneKey, droneJson);
                AppDbContext.AddKeyToCategory("Drone", droneKey);
            }
        }
        [Benchmark]
        public void GeneratePilotsWithInsurance()
        {
            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].InsuranceId = insurances[i].InsuranceId;
                insurances[i].PilotId = pilots[i].PilotId;
            }
            foreach (var pilot in pilots)
            {
                var pilotKey = $"Pilot:{pilot.PilotId}";
                var pilotJson = JsonConvert.SerializeObject(pilot);
                _memcachedClient.Store(StoreMode.Set, pilotKey, pilotJson);
                AppDbContext.AddKeyToCategory("Pilot", pilotKey);

            }
            // Dodawanie ubezpieczenia
            foreach (var insurance in insurances)
            {
                var insuranceKey = $"Insurance:{insurance.InsuranceId}";
                var insuranceJson = JsonConvert.SerializeObject(insurance);
                _memcachedClient.Store(StoreMode.Set, insuranceKey, insuranceJson);
                AppDbContext.AddKeyToCategory("Insurance", insuranceKey);
            }

        }
        [Benchmark]
        public void GeneratePilots()
        {
            foreach (var pilot in pilots)
            {
                var pilotKey = $"Pilot:{pilot.PilotId}";
                var pilotJson = JsonConvert.SerializeObject(pilot);
                _memcachedClient.Store(StoreMode.Set, pilotKey, pilotJson);
                AppDbContext.AddKeyToCategory("Pilot", pilotKey);

            }
        }
        [Benchmark]
        public void GenerateDronesWithRelations()
        {
            int seed = 12345;

            Random rand = new Random(seed);

            // Przypisywanie misji i lokalizacji do dronów
            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);
            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].InsuranceId = insurances[i].InsuranceId;
                insurances[i].PilotId = pilots[i].PilotId;
            }

            // Przypisywanie misji do dronów
            foreach (var drone in drones)
            {
                var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(rand.Next(0, 3)).ToList();
                drone.MissionIds = randomMissions.Select(m => m.MissionId).ToList();

                foreach (var mission in randomMissions)
                {
                    var missionKey = $"Mission:{mission.MissionId}";
                    var missionJson = JsonConvert.SerializeObject(mission);
                    _memcachedClient.Store(StoreMode.Set, missionKey, missionJson);
                    AppDbContext.AddKeyToCategory("Mission", missionKey);

                }
            }

            // Przypisywanie lokalizacji do dronów
            foreach (var drone in drones)
            {
                var randomLocations = availableLocations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                drone.LocationIds = randomLocations.Select(l => l.LocationId).ToList();
                availableLocations.RemoveAll(l => randomLocations.Contains(l));
                foreach (var location in randomLocations)
                {
                    var locationKey = $"Location:{location.LocationId}";
                    var locationJson = JsonConvert.SerializeObject(location);
                    _memcachedClient.Store(StoreMode.Set, locationKey, locationJson);
                    AppDbContext.AddKeyToCategory("Location", locationKey);

                }
            }
            foreach (var drone in drones)
            {
                var droneKey = $"Drone:{drone.DroneId}";
                var droneJson = JsonConvert.SerializeObject(drone);
                _memcachedClient.Store(StoreMode.Set, droneKey, droneJson);
                AppDbContext.AddKeyToCategory("Drone", droneKey);
            }
        }
    }
}
