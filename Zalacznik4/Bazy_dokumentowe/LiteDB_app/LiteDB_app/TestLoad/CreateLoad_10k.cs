using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Bogus;
using LiteDB;
using LiteDB_app.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace LiteDB_app.TestLoad
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 1, warmupCount: 0)]
    public class Create_Load_10k : IDisposable
    {
        private List<Drone> drones = new List<Drone>();
        private List<Insurance> insurances = new List<Insurance>();
        private List<Pilot> pilots = new List<Pilot>();
        private List<Mission> missions = new List<Mission>();
        private List<Location> locations = new List<Location>();

        [Params(10000)]
        public int Count { get; set; }

        private static LiteDatabase _database = new LiteDatabase(AppDbContext.connectionString);
        // Kolekcje LiteDB
        private ILiteCollection<Drone> _dronesCollection = _database.GetCollection<Drone>("Drones");
        private ILiteCollection<Pilot> _pilotsCollection = _database.GetCollection<Pilot>("Pilots");
        private ILiteCollection<Insurance> _insuranceCollection = _database.GetCollection<Insurance>("Insurance");
        private ILiteCollection<Mission> _missionsCollection = _database.GetCollection<Mission>("Missions");
        private ILiteCollection<Location> _locationsCollection = _database.GetCollection<Location>("Locations");
        private ILiteCollection<PilotMission> _pilotMissionsCollection = _database.GetCollection<PilotMission>("PilotMission");
        [GlobalSetup]
        public void Setup()
        {
            int seed = 12345;

            // Faker dla klasy Drone
            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            drones = droneFaker.Generate(Count);

            // Faker dla klasy Insurance
            var insuranceFaker = new Faker<Insurance>()
                .RuleFor(i => i.InsuranceProvider, f => f.Company.CompanyName())
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.EndDate, f => f.Date.Future())
                .UseSeed(seed);

            insurances = insuranceFaker.Generate(Count);

            // Faker dla klasy Pilot
            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            pilots = pilotFaker.Generate(Count);

            // Faker dla klasy Mission
            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            missions = missionFaker.Generate(Count);

            // Faker dla klasy Location
            var locationFaker = new Faker<Location>()
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
            _database.DropCollection("PilotMission");
            _database.DropCollection("Missions");
            _database.DropCollection("Insurance");
            _database.DropCollection("Pilots");
            _database.DropCollection("Locations");
            _database.DropCollection("Drones");
        }
        [Benchmark]
        public void GenerateAllData()
        {
            int seed = 12345;
            // Relacja 1:1 dla pilota i jego ubezpieczenia
            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].Insurance = insurances[i];
            }
            Random rand = new Random(seed);

            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);

            // Dodawanie danych do LiteDB bez transakcji
            try
            {
                foreach (var pilot in pilots)
                {
                    var insertedPilot = _pilotsCollection.Insert(pilot);
                    pilot.Insurance.PilotId = pilot.PilotId;
                    _insuranceCollection.Insert(pilot.Insurance);
                }

                // Dodawanie dronów
                foreach (var drone in drones)
                {
                    _dronesCollection.Insert(drone);

                    // Dodawanie lokalizacji dla dronów
                    var randomLocations = availableLocations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                    drone.Locations = randomLocations;
                    availableLocations.RemoveAll(loc => randomLocations.Contains(loc));

                    foreach (var location in randomLocations)
                    {
                        location.DroneId = drone.DroneId;
                        _locationsCollection.Insert(location);
                    }

                    // Dodawanie misji dla dronów
                    var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(3).ToList();
                    drone.Missions = randomMissions;
                    availableMissions.RemoveAll(m => randomMissions.Contains(m));

                    foreach (var mission in randomMissions)
                    {
                        mission.DroneId = drone.DroneId;
                        _missionsCollection.Insert(mission);

                        // Dodawanie pilotów do misji
                        var randomPilot = pilots[rand.Next(pilots.Count)];
                        var pilotMission = new PilotMission
                        {
                            PilotId = randomPilot.PilotId,
                            MissionId = mission.MissionId
                        };

                        _pilotMissionsCollection.Insert(pilotMission);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Wystąpił błąd podczas generowania danych: " + ex.Message);
                throw;
            }
        }
        public void Dispose()
        {
            _database.Dispose();
        }
    }
}