using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Bogus;
using Couchbase;
using Couchbase_app.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Couchbase_app.TestLoad
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 1, warmupCount: 0)]
    public class Create_Load_10k
    {
        private List<Drone> drones = new List<Drone>();
        private List<Insurance> insurances = new List<Insurance>();
        private List<Pilot> pilots = new List<Pilot>();
        private List<Mission> missions = new List<Mission>();
        private List<Location> locations = new List<Location>();
        private readonly Random _rand = new Random(12345);
        private IBucket _bucket;
        private readonly string _scopeName = AppDbContext.ScopeName;
        private Cluster _cluster;

        [Params(10000)]
        public int Count { get; set; }

        [GlobalSetup]
        public async Task Setup()
        {
            try
            {
                var options = new ClusterOptions
                {
                    UserName = "minx1",
                    Password = "minx111"
                };
                _cluster = (Cluster)await Cluster.ConnectAsync("couchbase://localhost", options);
                _bucket = await _cluster.BucketAsync("DronesBucket");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to Couchbase: {ex.Message}");
                throw;
            }

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
        public async void CleanDatabaseAsync()
        {
            try
            {
                var collectionManager = _bucket.Collections;

                var scopes = await collectionManager.GetAllScopesAsync();

                foreach (var scope in scopes)
                {
                    if (scope.Name == _scopeName)
                    {
                        foreach (var collection in scope.Collections)
                        {
                            var query = $"DELETE FROM `{_bucket.Name}`.`{scope.Name}`.`{collection.Name}` WHERE TRUE";
                            await _cluster.QueryAsync<dynamic>(query);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cleaning the database: {ex.Message}");
                throw;
            }
        }


        [Benchmark]
        public async Task GenerateAllData()
        {
            try
            {
                // Relacja 1:1 dla pilota i jego ubezpieczenia
                for (int i = 0; i < pilots.Count; i++)
                {
                    var pilot = pilots[i];
                    var insurance = insurances[i];
                    insurance.PilotId = pilot.PilotId;
                    pilot.Insurance = insurance;
                }

                // Dodawanie danych do kolekcji w Couchbase
                foreach (var pilot in pilots)
                {
                    var pilotCollection = _bucket.Scope(_scopeName).Collection("Pilots");
                    await pilotCollection.UpsertAsync($"pilot::{pilot.PilotId}", pilot);

                    var insuranceCollection = _bucket.Scope(_scopeName).Collection("Insurances");
                    await insuranceCollection.UpsertAsync($"insurance::{pilot.PilotId}", pilot.Insurance);
                }

                foreach (var drone in drones)
                {
                    var droneCollection = _bucket.Scope(_scopeName).Collection("Drones");
                    await droneCollection.UpsertAsync($"drone::{drone.DroneId}", drone);

                    var randomLocations = locations.OrderBy(l => _rand.Next()).Take(_rand.Next(0, 8)).ToList();
                    drone.Locations = randomLocations;

                    foreach (var location in randomLocations)
                    {
                        location.DroneId = drone.DroneId;
                        var locationCollection = _bucket.Scope(_scopeName).Collection("Locations");
                        await locationCollection.UpsertAsync($"location::{location.LocationId}", location);
                    }

                    var randomMissions = missions.OrderBy(m => _rand.Next()).Take(3).ToList();
                    drone.Missions = randomMissions;

                    foreach (var mission in randomMissions)
                    {
                        mission.DroneId = drone.DroneId;
                        var missionCollection = _bucket.Scope(_scopeName).Collection("Missions");
                        await missionCollection.UpsertAsync($"mission::{mission.MissionId}", mission);

                        var randomPilot = pilots[_rand.Next(pilots.Count)];
                        var pilotMission = new PilotMission
                        {
                            PilotId = randomPilot.PilotId,
                            MissionId = mission.MissionId,
                            Pilot = randomPilot,
                            Mission = mission
                        };

                        var pilotMissionCollection = _bucket.Scope(_scopeName).Collection("PilotMissions");
                        await pilotMissionCollection.UpsertAsync($"pilotMission::{pilotMission.PilotId}::{pilotMission.MissionId}", pilotMission);
                    }

                    await droneCollection.UpsertAsync($"drone::{drone.DroneId}", drone);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during data generation: {ex.Message}");
                throw;
            }

        }
    }
}