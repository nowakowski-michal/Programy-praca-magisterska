using Bogus;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB_app.Models
{
    public class GenerateData : IDisposable
    {
        private static LiteDatabase _database = new LiteDatabase(AppDbContext.connectionString);
        public int Count { get; set; }

        // Kolekcje LiteDB
        private ILiteCollection<Drone> _dronesCollection;
        private ILiteCollection<Pilot> _pilotsCollection;
        private ILiteCollection<Insurance> _insuranceCollection;
        private ILiteCollection<Mission> _missionsCollection;
        private ILiteCollection<Location> _locationsCollection;
        private ILiteCollection<PilotMission> _pilotMissionsCollection;

        public GenerateData()
        {
            
            _dronesCollection = _database.GetCollection<Drone>("Drones");
            _pilotsCollection = _database.GetCollection<Pilot>("Pilots");
            _insuranceCollection = _database.GetCollection<Insurance>("Insurance");
            _missionsCollection = _database.GetCollection<Mission>("Missions");
            _locationsCollection = _database.GetCollection<Location>("Locations");
            _pilotMissionsCollection = _database.GetCollection<PilotMission>("PilotMission");
        }
        public void CleanDatabase()
        {
            _database.DropCollection("PilotMission");
            _database.DropCollection("Missions");
            _database.DropCollection("Insurance");
            _database.DropCollection("Pilots");
            _database.DropCollection("Locations");
            _database.DropCollection("Drones");
        }
        // Generowanie danych
        public void GenerateAllData()
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

            // Faker dla klasy Insurance
            var insuranceFaker = new Faker<Insurance>()
                .RuleFor(i => i.InsuranceProvider, f => f.Company.CompanyName())
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.EndDate, f => f.Date.Future())
                .UseSeed(seed);

            var insurances = insuranceFaker.Generate(Count);

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

            // Relacja 1:1 dla pilota i jego ubezpieczenia
            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].Insurance = insurances[i];
            }
            Random rand = new Random(seed);

            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);

            try
            {
                foreach (var pilot in pilots)
                {
                    var insertedPilot = _pilotsCollection.Insert(pilot);
                    pilot.Insurance.PilotId = pilot.PilotId;
                    _insuranceCollection.Insert(pilot.Insurance);
                }

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
            _database?.Dispose();
        }
    }
}
