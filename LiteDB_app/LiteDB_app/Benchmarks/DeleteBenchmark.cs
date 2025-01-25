using BenchmarkDotNet.Attributes;
using Bogus;
using LiteDB;
using LiteDB_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class DeleteBenchmark : IDisposable
    {
        private static LiteDatabase _database = new LiteDatabase(AppDbContext.connectionString);
        // Kolekcje LiteDB
        private ILiteCollection<Drone> _dronesCollection = _database.GetCollection<Drone>("Drones");
        private ILiteCollection<Pilot> _pilotsCollection = _database.GetCollection<Pilot>("Pilots");
        private ILiteCollection<Insurance> _insuranceCollection = _database.GetCollection<Insurance>("Insurance");
        private ILiteCollection<Mission> _missionsCollection = _database.GetCollection<Mission>("Missions");
        private ILiteCollection<Location> _locationsCollection = _database.GetCollection<Location>("Locations");
        private ILiteCollection<PilotMission> _pilotMissionsCollection = _database.GetCollection<PilotMission>("PilotMission");
        [Params(100, 1000)]
        public int NumberOfRows;
        private int Count = 1000;
        public void CleanDatabase()
        {
            _database.DropCollection("PilotMission");
            _database.DropCollection("Missions");
            _database.DropCollection("Insurance");
            _database.DropCollection("Pilots");
            _database.DropCollection("Locations");
            _database.DropCollection("Drones");
        }
        [IterationSetup]
        public void IterationSetup()
        {
            CleanDatabase();
            int seed = 12345;

            // Faker dla klasy Drone
            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            var drones = droneFaker.Generate(Count);


            // Faker dla klasy Pilot
            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            var pilots = pilotFaker.Generate(Count);

            // Faker dla klasy Mission
            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            var missions = missionFaker.Generate(Count);

            // Faker dla klasy Location
            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            var locations = locationFaker.Generate(Count);

            Random rand = new Random(seed);

            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);
            try
            {
                foreach (var pilot in pilots)
                {
                    _pilotsCollection.Insert(pilot);
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
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Wystąpił błąd podczas generowania danych: " + ex.Message);
                throw;
            }
        }

        [Benchmark]
        public void TestDelete_PilotWithoutInsurance()
        {
            var pilotsToDelete = _pilotsCollection
                .Find(p => p.Insurance == null) 
                .Take(NumberOfRows)
                .ToList();

            foreach (var pilot in pilotsToDelete)
            {
                _pilotsCollection.Delete(pilot.PilotId); 
            }
        }

        [Benchmark]
        public void TestDelete_DronesWithCascade()
        {
            var dronesToDelete = _dronesCollection.FindAll().Take(NumberOfRows).ToList();

            foreach (var drone in dronesToDelete)
            {
                _missionsCollection.Delete(drone.DroneId); 
                _locationsCollection.Delete(drone.DroneId); 
                _dronesCollection.Delete(drone.DroneId);
            }
        }
        public void Dispose()
        {
            _database.Dispose();
        }
    }
}
