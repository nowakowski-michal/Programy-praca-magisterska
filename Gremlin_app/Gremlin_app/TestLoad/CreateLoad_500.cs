using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Bogus;
using System;
using System.Data;
using Newtonsoft.Json;
using Gremlin.Net.Driver;
using Gremlin_app.Models;

namespace Gremlin_app.TestLoad
{
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 1, warmupCount: 0)]
    public class Create_Load_100k
    {
        private static GremlinClient _client;
        private List<Drone> drones = new List<Drone>();
        private List<Insurance> insurances = new List<Insurance>();
        private List<Pilot> pilots = new List<Pilot>();
        private List<Mission> missions = new List<Mission>();
        private List<Location> locations = new List<Location>();
        private static readonly object lockObject = new object();

        [Params(500)]
        public int Count { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _client = AppDbContext.client;
            int seed = 12345;

            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.DroneId, f => f.IndexGlobal)
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            drones = droneFaker.Generate(Count);

            var insuranceFaker = new Faker<Insurance>()
                .RuleFor(i => i.InsuranceId, f => f.IndexGlobal)
                .RuleFor(i => i.InsuranceProvider, f => "Provider Name")
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.EndDate, f => f.Date.Future())
                .UseSeed(seed);

            insurances = insuranceFaker.Generate(Count);

            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.PilotId, f => f.IndexGlobal)
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => "Name")
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            pilots = pilotFaker.Generate(Count);

            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionId, f => f.IndexGlobal)
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            missions = missionFaker.Generate(Count);

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
                var deleteEdgesQuery = "g.E().drop()";
                await _client.SubmitAsync<dynamic>(deleteEdgesQuery);

                var deleteVerticesQuery = "g.V().drop()";
                await _client.SubmitAsync<dynamic>(deleteVerticesQuery);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while cleaning the database: {ex.Message}");
            }
        }

        [Benchmark]
        public async Task GenerateAllData()
        {
            int seed = 12345;

            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].InsuranceId = insurances[i].InsuranceId;
                insurances[i].PilotId = pilots[i].PilotId;
            }

            try
            {
                foreach (var drone in drones)
                {
                    var query = $"g.addV('drone').property('DroneId', {drone.DroneId}).property('Model', '{drone.Model}').property('Manufacturer', '{drone.Manufacturer}').property('YearOfManufacture', {drone.YearOfManufacture}).property('Specifications', '{drone.Specifications}')";
                    await _client.SubmitAsync<dynamic>(query);
                }

                foreach (var pilot in pilots)
                {
                    var query = $"g.addV('pilot').property('PilotId', {pilot.PilotId}).property('FirstName', '{pilot.FirstName}').property('LastName', '{pilot.LastName}').property('LicenseNumber', '{pilot.LicenseNumber}')";
                    await _client.SubmitAsync<dynamic>(query);
                }

                foreach (var mission in missions)
                {
                    var query = $"g.addV('mission').property('MissionId', {mission.MissionId}).property('MissionName', '{mission.MissionName}').property('StartTime', '{mission.StartTime:yyyy-MM-ddTHH:mm:ss}').property('EndTime', '{mission.EndTime:yyyy-MM-ddTHH:mm:ss}').property('Status', '{mission.Status}')";
                    await _client.SubmitAsync<dynamic>(query);
                }

                foreach (var location in locations)
                {
                    var lat = (int)(location.Latitude * 1000000);
                    var lon = (int)(location.Longitude * 1000000);
                    var alt = (int)(location.Altitude);

                    if (lat == 0 || lon == 0 || alt == 0)
                    {
                        continue;
                    }

                    var query = $"g.addV('location').property('LocationId', {location.LocationId}).property('Latitude', {lat}).property('Longitude', {lon}).property('Altitude', {alt}).property('Timestamp', '{location.Timestamp:yyyy-MM-ddTHH:mm:ss}')";
                    await _client.SubmitAsync<dynamic>(query);
                }

                foreach (var insurance in insurances)
                {
                    var query = $"g.addV('insurance').property('InsuranceId', {insurance.InsuranceId}).property('InsuranceProvider', '{insurance.InsuranceProvider.Replace("'", "''")}').property('PolicyNumber', '{insurance.PolicyNumber}').property('EndDate', '{insurance.EndDate:yyyy-MM-dd}')";
                    await _client.SubmitAsync<dynamic>(query);
                }

                foreach (var pilot in pilots)
                {
                    var query = $@"
                        g.V().has('pilot', 'PilotId', {pilot.PilotId}).as('p')
                          .V().has('insurance', 'InsuranceId', {pilot.InsuranceId}).as('i')
                          .addE('HAS_INSURANCE').from('p').to('i')";
                    await _client.SubmitAsync<dynamic>(query);
                }

                foreach (var pilot in pilots)
                {
                    var selectedMissions = missions.OrderBy(m => Guid.NewGuid()).Take(3).ToList();
                    foreach (var mission in selectedMissions)
                    {
                        var query = $"g.V().has('pilot', 'PilotId', {pilot.PilotId}).as('p').V().has('mission', 'MissionId', {mission.MissionId}).addE('ASSIGNED_TO').from('p')";
                        await _client.SubmitAsync<dynamic>(query);
                    }
                }

                foreach (var drone in drones)
                {
                    var selectedMissions = missions.OrderBy(m => Guid.NewGuid()).Take(3).ToList();
                    foreach (var mission in selectedMissions)
                    {
                        var query = $"g.V().has('drone', 'DroneId', {drone.DroneId}).as('d').V().has('mission', 'MissionId', {mission.MissionId}).addE('HAS_MISSION').from('d')";
                        await _client.SubmitAsync<dynamic>(query);
                    }
                }

                foreach (var drone in drones)
                {
                    var selectedLocations = locations.OrderBy(l => Guid.NewGuid()).Take(3).ToList();
                    foreach (var location in selectedLocations)
                    {
                        var query = $"g.V().has('drone', 'DroneId', {drone.DroneId}).as('d').V().has('location', 'LocationId', {location.LocationId}).addE('HAS_LOCATION').from('d')";
                        await _client.SubmitAsync<dynamic>(query);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while generating data: {ex.Message}");
            }
        }
    }
}
