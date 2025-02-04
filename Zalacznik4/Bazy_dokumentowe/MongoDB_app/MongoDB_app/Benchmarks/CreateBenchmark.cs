using BenchmarkDotNet.Attributes;
using Bogus;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class CreateBenchmark
    {
        private List<Drone> drones = new List<Drone>();
        private List<Insurance> insurances = new List<Insurance>();
        private List<Pilot> pilots = new List<Pilot>();
        private List<Mission> missions = new List<Mission>();
        private List<Location> locations = new List<Location>();

        [Params(100,1000,5000,10000)]
        public int Count { get; set; }
        private static IMongoDatabase database = new MongoClient(AppDbContext.clientString).GetDatabase(AppDbContext.databaseString);
        private IMongoCollection<Drone> dronesCollection => database.GetCollection<Drone>("Drones");
        private IMongoCollection<Mission> missionsCollection => database.GetCollection<Mission>("Missions");
        private IMongoCollection<Location> locationsCollection => database.GetCollection<Location>("Locations");
        private IMongoCollection<Pilot> pilotsCollection => database.GetCollection<Pilot>("Pilots");
        private IMongoCollection<PilotMission> pilotMissionsCollection => database.GetCollection<PilotMission>("PilotMission");
        private IMongoCollection<Insurance> insuranceCollection => database.GetCollection<Insurance>("Insurance");

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
            database.DropCollection("PilotMission");
            database.DropCollection("Missions");
            database.DropCollection("Insurance");
            database.DropCollection("Pilots");
            database.DropCollection("Locations");
            database.DropCollection("Drones");
        }

        [Benchmark]
        public void GenerateAllData()
        {
            // Relacja 1:1 dla pilota i jego ubezpieczenia
            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].Insurance = insurances[i];
            }
            int seed = 12345;
            Random rand = new Random(seed);

            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);

            // Dodawanie danych do MongoDB bez transakcji
            try
            {
                foreach (var pilot in pilots)
                {
                    if (pilot.PilotId == ObjectId.Empty)
                    {
                        pilot.PilotId = ObjectId.GenerateNewId();
                        pilot.Insurance.PilotId = pilot.PilotId;
                    }
                    var insurance = pilot.Insurance;
                    if (insurance.PilotId == ObjectId.Empty)
                    {
                        insurance.PilotId = pilot.PilotId;
                    }

                    insuranceCollection.InsertOne(insurance);
                    pilotsCollection.InsertOne(pilot);
                }
                foreach (var drone in drones)
                {
                    dronesCollection.InsertOne(drone);

                    // Dodawanie lokalizacji
                    var randomLocations = availableLocations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                    foreach (var location in randomLocations)
                    {
                        location.DroneId = drone.DroneId;
                        location.LocationId = ObjectId.GenerateNewId(); 
                        locationsCollection.InsertOne(location);
                    }

                    // Dodawanie misji
                    var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(3).ToList();
                    foreach (var mission in randomMissions)
                    {
                        mission.DroneId = drone.DroneId;
                        mission.MissionId = ObjectId.GenerateNewId(); 
                        missionsCollection.InsertOne(mission);

                        var randomPilot = pilots[rand.Next(pilots.Count)];
                        var pilotMission = new PilotMission
                        {
                            PilotMissionId = ObjectId.GenerateNewId(),
                            PilotId = randomPilot.PilotId,
                            MissionId = mission.MissionId,
                            Pilot = randomPilot,
                            Mission = mission    
                        };
                        pilotMissionsCollection.InsertOne(pilotMission);
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
        public void GeneratePilotsWithInsurance()
        {
            // Relacja 1:1 dla pilota i jego ubezpieczenia
            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].Insurance = insurances[i];
            }
            foreach (var pilot in pilots)
            {
                if (pilot.PilotId == ObjectId.Empty)
                {
                    pilot.PilotId = ObjectId.GenerateNewId();
                    pilot.Insurance.PilotId = pilot.PilotId;
                }
                var insurance = pilot.Insurance;
                if (insurance.PilotId == ObjectId.Empty)
                {
                    insurance.PilotId = pilot.PilotId;
                }

                insuranceCollection.InsertOne(insurance);
                pilotsCollection.InsertOne(pilot);
            }
        }
        [Benchmark]
        public void GeneratePilots()
        {
            foreach (var pilot in pilots)
            {
                pilotsCollection.InsertOne(pilot);
            }
        }
        [Benchmark]
        public void GenerateDronesWithRelations()
        {

            int seed = 12345;
            Random rand = new Random(seed);

            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);

            try
            {
                // Dodawanie dronów
                foreach (var drone in drones)
                {
                    dronesCollection.InsertOne(drone);

                    // Dodawanie lokalizacji dla dronów
                    var randomLocations = availableLocations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                    drone.Locations = randomLocations;
                    availableLocations.RemoveAll(loc => randomLocations.Contains(loc));

                    foreach (var location in randomLocations)
                    {
                        location.DroneId = drone.DroneId;
                        locationsCollection.InsertOne(location);
                    }

                    // Dodawanie misji dla dronów
                    var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(3).ToList();
                    drone.Missions = randomMissions;
                    availableMissions.RemoveAll(m => randomMissions.Contains(m));

                    foreach (var mission in randomMissions)
                    {
                        mission.DroneId = drone.DroneId;
                        missionsCollection.InsertOne(mission);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Wystąpił błąd podczas generowania danych: " + ex.Message);
                throw;
            }
        }
    }
}

