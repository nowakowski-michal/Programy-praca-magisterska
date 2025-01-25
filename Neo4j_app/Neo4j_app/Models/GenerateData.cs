﻿using BenchmarkDotNet.Attributes;
using Bogus;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Neo4j_app.Models
{

    using Bogus;
    using Neo4j.Driver;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    public class GenerateData
    {
        private readonly IDriver _driver;
        public int Count { get; set; }

        public GenerateData()
        {
            _driver = AppDbContext._driver;
        }

        public async Task CleanDatabaseAsync()
        {
            using (var session = _driver.AsyncSession())
            {
                try
                {
                    
                    await session.RunAsync("MATCH (n) DETACH DELETE n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while cleaning the database: {ex.Message}");
                }
            }
        }
        public async Task GenerateAllData()
        {
            int seed = 12345;
            await CleanDatabaseAsync();

            
            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.DroneId, f => f.UniqueIndex)
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.PilotId, f => f.UniqueIndex)
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => "Name")
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            var insuranceFaker = new Faker<Insurance>()
                .RuleFor(i => i.InsuranceId, f => f.UniqueIndex)
                .RuleFor(i => i.InsuranceProvider, f => "Name")  
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.EndDate, f => f.Date.Future())
                .UseSeed(seed);

            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionId, f => f.UniqueIndex)
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.LocationId, f => f.UniqueIndex)
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            
            var drones = droneFaker.Generate(Count);
            var pilots = pilotFaker.Generate(Count);
            var missions = missionFaker.Generate(Count);
            var locations = locationFaker.Generate(Count);
            var insurances = insuranceFaker.Generate(Count);

            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].InsuranceId = insurances[i].InsuranceId;
                insurances[i].PilotId = pilots[i].PilotId;
            }

            
            var session = _driver.AsyncSession();

            try
            {
                
                foreach (var drone in drones)
                {
                    await session.RunAsync($"CREATE (:Drone {{DroneId: {drone.DroneId}, Model: '{drone.Model}', Manufacturer: '{drone.Manufacturer}', YearOfManufacture: {drone.YearOfManufacture}, Specifications: '{drone.Specifications}'}})");
                }

                foreach (var pilot in pilots)
                {
                    await session.RunAsync($"CREATE (:Pilot {{PilotId: {pilot.PilotId}, FirstName: '{pilot.FirstName}', LastName: '{pilot.LastName}', LicenseNumber: '{pilot.LicenseNumber}'}})");
                }

                foreach (var insurance in insurances)
                {
                    await session.RunAsync($"CREATE (:Insurance {{InsuranceId: {insurance.InsuranceId}, InsuranceProvider: '{insurance.InsuranceProvider.Replace("'", "''")}', PolicyNumber: '{insurance.PolicyNumber}', EndDate: date('{insurance.EndDate:yyyy-MM-dd}')}})");

                    
                }

                foreach (var mission in missions)
                {
                    await session.RunAsync($"CREATE (:Mission {{MissionId: {mission.MissionId}, MissionName: '{mission.MissionName}', StartTime: datetime('{mission.StartTime:yyyy-MM-ddTHH:mm:ss}'), EndTime: datetime('{mission.EndTime:yyyy-MM-ddTHH:mm:ss}'), Status: '{mission.Status}'}})");
                }

                foreach (var location in locations)
                {
                    
                    var lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
                    var lon = location.Longitude.ToString(CultureInfo.InvariantCulture);
                    var alt = location.Altitude.ToString(CultureInfo.InvariantCulture);
                    await session.RunAsync($"CREATE (l:Location {{LocationId: {location.LocationId}, Latitude: {lat}, Longitude: {lon}, Altitude: {alt}, Timestamp: '{location.Timestamp}'}})");
                }

                int insuranceIndex = 0;  
                foreach (var pilot in pilots)
                {
                    if (insuranceIndex < insurances.Count)
                    {
                        var insurance = insurances[insuranceIndex];  

                        
                        await session.RunAsync(
                            "MATCH (p:Pilot {PilotId: $pilotId}), (i:Insurance {InsuranceId: $insuranceId}) " +
                            "WHERE NOT (p)-[:HAS_INSURANCE]->(i) " +  
                            "CREATE (p)-[:HAS_INSURANCE]->(i)",
                            new { pilotId = pilot.PilotId, insuranceId = insurance.InsuranceId }
                        );

                        insuranceIndex++;  
                    }
                }

                foreach (var pilot in pilots)
                {
                    
                    var selectedMissions = missions.OrderBy(m => Guid.NewGuid()).Take(3).ToList(); 
                    foreach (var mission in selectedMissions)
                    {
                        
                        await session.RunAsync(
                            "MATCH (p:Pilot {PilotId: $pilotId}), (m:Mission {MissionId: $missionId}) " +
                            "MERGE (p)-[:ASSIGNED_TO]->(m)",  
                            new { pilotId = pilot.PilotId, missionId = mission.MissionId }
                        );
                    }
                }

                foreach (var drone in drones)
                {
                    
                    var availableMissions = new List<Mission>();

                    foreach (var mission in missions)
                    {
                        
                        var resultCursor = await session.RunAsync(
                            "MATCH (m:Mission {MissionId: $missionId})<-[:HAS_MISSION]-(d:Drone) RETURN d",
                            new { missionId = mission.MissionId }
                        );

                        
                        var results = await resultCursor.ToListAsync();

                        
                        if (results.Count == 0)
                        {
                            availableMissions.Add(mission);
                        }
                    }

                    
                    var selectedMissions = availableMissions.OrderBy(m => Guid.NewGuid()).Take(3).ToList();

                    foreach (var mission in selectedMissions)
                    {
                        
                        await session.RunAsync(
                            "MATCH (d:Drone {DroneId: $droneId}), (m:Mission {MissionId: $missionId}) " +
                            "CREATE (d)-[:HAS_MISSION]->(m)",
                            new { droneId = drone.DroneId, missionId = mission.MissionId }
                        );
                    }
                }

                foreach (var drone in drones)
                {
                    
                    var availableLocations = new List<Location>();

                    foreach (var location in locations)
                    {
                        
                        var resultCursor = await session.RunAsync(
                            "MATCH (l:Location {LocationId: $locationId})<-[:HAS_LOCATION]-(d:Drone) RETURN d",
                            new { locationId = location.LocationId }
                        );

                        
                        var results = await resultCursor.ToListAsync();

                        
                        if (results.Count == 0)
                        {
                            availableLocations.Add(location);
                        }
                    }

                    
                    var selectedLocations = availableLocations.OrderBy(l => Guid.NewGuid()).Take(3).ToList();

                    foreach (var location in selectedLocations)
                    {
                        
                        await session.RunAsync(
                            "MATCH (d:Drone {DroneId: $droneId}), (l:Location {LocationId: $locationId}) " +
                            "CREATE (d)-[:HAS_LOCATION]->(l)",
                            new { droneId = drone.DroneId, locationId = location.LocationId }
                        );
                    }
                }
            }
            finally
            {
                await session.CloseAsync();
            }

        }
        public async Task GenerateDataForDelete()
        {
            int seed = 12345;
            await CleanDatabaseAsync();

            
            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.DroneId, f => f.UniqueIndex)
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.PilotId, f => f.UniqueIndex)
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => "sss")
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);


            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionId, f => f.UniqueIndex)
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.LocationId, f => f.UniqueIndex)
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            
            var drones = droneFaker.Generate(Count);
            var pilots = pilotFaker.Generate(Count);
            var missions = missionFaker.Generate(Count);
            var locations = locationFaker.Generate(Count);

            
            var session = _driver.AsyncSession();

            try
            {
                
                foreach (var drone in drones)
                {
                    await session.RunAsync($"CREATE (:Drone {{DroneId: {drone.DroneId}, Model: '{drone.Model}', Manufacturer: '{drone.Manufacturer}', YearOfManufacture: {drone.YearOfManufacture}, Specifications: '{drone.Specifications}'}})");
                }

                foreach (var pilot in pilots)
                {
                    await session.RunAsync($"CREATE (:Pilot {{PilotId: {pilot.PilotId}, FirstName: '{pilot.FirstName}', LastName: '{pilot.LastName}', LicenseNumber: '{pilot.LicenseNumber}'}})");
                }


                foreach (var mission in missions)
                {
                    await session.RunAsync($"CREATE (:Mission {{MissionId: {mission.MissionId}, MissionName: '{mission.MissionName}', StartTime: datetime('{mission.StartTime:yyyy-MM-ddTHH:mm:ss}'), EndTime: datetime('{mission.EndTime:yyyy-MM-ddTHH:mm:ss}'), Status: '{mission.Status}'}})");
                }

                foreach (var location in locations)
                {
                    
                    var lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
                    var lon = location.Longitude.ToString(CultureInfo.InvariantCulture);
                    var alt = location.Altitude.ToString(CultureInfo.InvariantCulture);
                    await session.RunAsync($"CREATE (l:Location {{LocationId: {location.LocationId}, Latitude: {lat}, Longitude: {lon}, Altitude: {alt}, Timestamp: '{location.Timestamp}'}})");
                }

                foreach (var drone in drones)
                {
                    
                    var availableMissions = new List<Mission>();

                    foreach (var mission in missions)
                    {
                        
                        var resultCursor = await session.RunAsync(
                            "MATCH (m:Mission {MissionId: $missionId})<-[:HAS_MISSION]-(d:Drone) RETURN d",
                            new { missionId = mission.MissionId }
                        );

                        
                        var results = await resultCursor.ToListAsync();

                        
                        if (results.Count == 0)
                        {
                            availableMissions.Add(mission);
                        }
                    }

                    
                    var selectedMissions = availableMissions.OrderBy(m => Guid.NewGuid()).Take(3).ToList();

                    foreach (var mission in selectedMissions)
                    {
                        
                        await session.RunAsync(
                            "MATCH (d:Drone {DroneId: $droneId}), (m:Mission {MissionId: $missionId}) " +
                            "CREATE (d)-[:HAS_MISSION]->(m)",
                            new { droneId = drone.DroneId, missionId = mission.MissionId }
                        );
                    }
                }

                foreach (var drone in drones)
                {
                    
                    var availableLocations = new List<Location>();

                    foreach (var location in locations)
                    {
                        
                        var resultCursor = await session.RunAsync(
                            "MATCH (l:Location {LocationId: $locationId})<-[:HAS_LOCATION]-(d:Drone) RETURN d",
                            new { locationId = location.LocationId }
                        );

                        
                        var results = await resultCursor.ToListAsync();

                        
                        if (results.Count == 0)
                        {
                            availableLocations.Add(location);
                        }
                    }

                    
                    var selectedLocations = availableLocations.OrderBy(l => Guid.NewGuid()).Take(3).ToList();

                    foreach (var location in selectedLocations)
                    {
                        
                        await session.RunAsync(
                            "MATCH (d:Drone {DroneId: $droneId}), (l:Location {LocationId: $locationId}) " +
                            "CREATE (d)-[:HAS_LOCATION]->(l)",
                            new { droneId = drone.DroneId, locationId = location.LocationId }
                        );
                    }
                }
            }
            finally
            {
                await session.CloseAsync();
            }

        }
      




    }

}
