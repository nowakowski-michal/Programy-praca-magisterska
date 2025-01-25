using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Bogus;

using RocksDb_app.Models;

using System;
using System.Data;
using Newtonsoft.Json;
using RocksDbApp.Models;
using RocksDbSharp;
namespace RocksDb_app.TestLoad
{
    //przy sprawdzaniu obciążenia zostanie wykonana tylko 1 iteracja testu, aby łatwiej zmierzyć
    // użycie zasobów przez aplikacje, dodatkowo zostana wyswietlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 1, warmupCount: 0)]
    public class Create_Load_10k
    {
        private RocksDb _db;
        private string _dbPath;
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
            _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var options = new DbOptions().SetCreateIfMissing(true);
            _db = RocksDb.Open(options, _dbPath);
            int seed = 12345;

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
            GenerateData.CleanDatabase(_db);
        }
        [Benchmark]
        public void GenerateAllData()
        {
            int seed = 12345;
            Random rand = new Random(seed);
            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].InsuranceId = insurances[i].InsuranceId;
                insurances[i].PilotId = pilots[i].PilotId;
            }
            foreach (var drone in drones)
            {
                var randomMissions = missions.OrderBy(m => rand.Next()).Take(rand.Next(0, 3)).ToList();
                drone.MissionIds = randomMissions.Select(m => m.MissionId).ToList();

                foreach (var mission in randomMissions)
                {
                    var missionKey = $"Mission:{mission.MissionId}";
                    mission.DroneId = drone.DroneId;
                    var missionJson = JsonConvert.SerializeObject(mission);
                    _db.Put(missionKey, missionJson);
                }
            }

            foreach (var drone in drones)
            {
                var randomLocations = locations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                drone.LocationIds = randomLocations.Select(l => l.LocationId).ToList();

                foreach (var location in randomLocations)
                {
                    var locationKey = $"Location:{location.LocationId}";
                    location.DroneId = drone.DroneId;
                    var locationJson = JsonConvert.SerializeObject(location);
                    _db.Put(locationKey, locationJson);
                }
            }
            foreach (var pilot in pilots)
            {
                var pilotKey = $"Pilot:{pilot.PilotId}";
                var pilotJson = JsonConvert.SerializeObject(pilot);
                _db.Put(pilotKey, pilotJson);
                var randomMissionsForPilot = missions.OrderBy(m => rand.Next()).Take(rand.Next(0, 3)).ToList();

                foreach (var mission in randomMissionsForPilot)
                {
                    var pilotMissionKey = $"PilotMission:{pilot.PilotId}:{mission.MissionId}";
                    var pilotMissionJson = JsonConvert.SerializeObject(new { PilotId = pilot.PilotId, MissionId = mission.MissionId });
                    _db.Put(pilotMissionKey, pilotMissionJson);
                }
            }

            foreach (var insurance in insurances)
            {
                var insuranceKey = $"Insurance:{insurance.InsuranceId}";
                var insuranceJson = JsonConvert.SerializeObject(insurance);
                _db.Put(insuranceKey, insuranceJson);
            }

            foreach (var drone in drones)
            {
                var droneKey = $"Drone:{drone.DroneId}";
                var droneJson = JsonConvert.SerializeObject(drone);
                _db.Put(droneKey, droneJson);
            }
        }
 

        [GlobalCleanup]
        public void Cleanup()
        {
            _db?.Dispose();
            if (Directory.Exists(_dbPath))
            {
                Directory.Delete(_dbPath, true);
            }
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
