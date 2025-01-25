﻿using Bogus;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Redis_app.Models
{
    public class GenerateData
    {
        private static IDatabase redisDatabase = AppDbContext.redisDatabase;

        public static IServer server =AppDbContext.server;

        public int Count { get; set; }
  
        public void CleanDatabase()
        {
            server.FlushDatabase();
        }     
        public void GenerateAllData()
        {
            CleanDatabase();

            int seed = 12345;

            // Faker dla klasy Drone
            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.DroneId, f => f.IndexGlobal)
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            var drones = droneFaker.Generate(Count);

            // Faker dla klasy Insurance
            var insuranceFaker = new Faker<Insurance>()
                .RuleFor(i => i.InsuranceId, f => f.IndexGlobal)
                .RuleFor(i => i.InsuranceProvider, f => f.Company.CompanyName())
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.EndDate, f => f.Date.Future())
                .UseSeed(seed);

            var insurances = insuranceFaker.Generate(Count);

            // Faker dla klasy Pilot
            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.PilotId, f => f.IndexGlobal)
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            var pilots = pilotFaker.Generate(Count);

            // Faker dla klasy Mission
            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionId, f => f.IndexGlobal)
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            var missions = missionFaker.Generate(Count);

            // Faker dla klasy Location
            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.LocationId, f => f.IndexGlobal)
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            var locations = locationFaker.Generate(Count);

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

                    redisDatabase.HashSet(missionKey, new[]
                    {
                new HashEntry("MissionName", mission.MissionName),
                new HashEntry("StartTime", mission.StartTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new HashEntry("EndTime", mission.EndTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new HashEntry("Status", mission.Status),
                new HashEntry("DroneId", drone.DroneId.ToString())
                });
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

                    redisDatabase.HashSet(locationKey, new[]
                    {
                new HashEntry("Latitude", location.Latitude.ToString()),
                new HashEntry("Longitude", location.Longitude.ToString()),
                new HashEntry("Altitude", location.Altitude.ToString()),
                new HashEntry("Timestamp", location.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                new HashEntry("DroneId", drone.DroneId.ToString())
            });
                }
            }

            // Dodawanie pilota i przypisanie pilota do misji
            foreach (var pilot in pilots)
            {
                var pilotKey = $"Pilot:{pilot.PilotId}";

                redisDatabase.HashSet(pilotKey, new[]
                {
            new HashEntry("FirstName", pilot.FirstName),
            new HashEntry("LastName", pilot.LastName),
            new HashEntry("LicenseNumber", pilot.LicenseNumber),
            new HashEntry("InsuranceId", pilot.InsuranceId.ToString())
        });

                var randomMissionsForPilot = availableMissions.OrderBy(m => rand.Next()).Take(rand.Next(0, 3)).ToList();

                foreach (var mission in randomMissionsForPilot)
                {
                    var missionKey = $"Mission:{mission.MissionId}";

                    var pilotMissionKey = $"PilotMission:{pilot.PilotId}:{mission.MissionId}";
                    redisDatabase.HashSet(pilotMissionKey, new[]
                    {
                new HashEntry("PilotId", pilot.PilotId),
                new HashEntry("MissionId", mission.MissionId)
            });
                }
            }

            foreach (var insurance in insurances)
            {
                var insuranceKey = $"Insurance:{insurance.InsuranceId}";

                redisDatabase.HashSet(insuranceKey, new[]
                {
            new HashEntry("InsuranceProvider", insurance.InsuranceProvider),
            new HashEntry("PolicyNumber", insurance.PolicyNumber),
            new HashEntry("EndDate", insurance.EndDate.ToString("yyyy-MM-dd")),
            new HashEntry("PilotId", insurance.PilotId.ToString())
        });
            }

            foreach (var drone in drones)
            {
                var droneKey = $"Drone:{drone.DroneId}";

                redisDatabase.HashSet(droneKey, new[]
                {
            new HashEntry("Model", drone.Model),
            new HashEntry("Manufacturer", drone.Manufacturer),
            new HashEntry("YearOfManufacture", drone.YearOfManufacture.ToString()),
            new HashEntry("Specifications", drone.Specifications),
            new HashEntry("MissionIds", string.Join(",", drone.MissionIds)),
            new HashEntry("LocationIds", string.Join(",", drone.LocationIds))
        });
            }
        }

        public void GenerateDataForDelete()
        {
            CleanDatabase();

            int seed = 12345;

            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.DroneId, f => f.IndexGlobal)
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            var drones = droneFaker.Generate(Count);

            // Faker dla klasy Insurance
            var insuranceFaker = new Faker<Insurance>()
                .RuleFor(i => i.InsuranceId, f => f.IndexGlobal)
                .RuleFor(i => i.InsuranceProvider, f => f.Company.CompanyName())
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.EndDate, f => f.Date.Future())
                .UseSeed(seed);

            var insurances = insuranceFaker.Generate(Count);

            // Faker dla klasy Pilot
            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.PilotId, f => f.IndexGlobal)
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            var pilots = pilotFaker.Generate(Count);

            // Faker dla klasy Mission
            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionId, f => f.IndexGlobal)
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            var missions = missionFaker.Generate(Count);

            // Faker dla klasy Location
            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.LocationId, f => f.IndexGlobal)
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            var locations = locationFaker.Generate(Count);

            Random rand = new Random(seed);

            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);
 
            foreach (var drone in drones)
            {
                var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(rand.Next(0, 3)).ToList();
                drone.MissionIds = randomMissions.Select(m => m.MissionId).ToList();

                foreach (var mission in randomMissions)
                {
                    var missionKey = $"Mission:{mission.MissionId}";

                    redisDatabase.HashSet(missionKey, new[]
                    {
                new HashEntry("MissionName", mission.MissionName),
                new HashEntry("StartTime", mission.StartTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new HashEntry("EndTime", mission.EndTime.ToString("yyyy-MM-dd HH:mm:ss")),
                new HashEntry("Status", mission.Status),
                new HashEntry("DroneId", drone.DroneId.ToString())
                });
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

                    redisDatabase.HashSet(locationKey, new[]
                    {
                new HashEntry("Latitude", location.Latitude.ToString()),
                new HashEntry("Longitude", location.Longitude.ToString()),
                new HashEntry("Altitude", location.Altitude.ToString()),
                new HashEntry("Timestamp", location.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                new HashEntry("DroneId", drone.DroneId.ToString())
            });
                }
            }

            foreach (var pilot in pilots)
            {
                var pilotKey = $"Pilot:{pilot.PilotId}";

                redisDatabase.HashSet(pilotKey, new[]
                {
                new HashEntry("FirstName", pilot.FirstName),
                new HashEntry("LastName", pilot.LastName),
                new HashEntry("LicenseNumber", pilot.LicenseNumber),
                new HashEntry("InsuranceId", pilot.InsuranceId.ToString())
                });
            }

            foreach (var drone in drones)
            {
                var droneKey = $"Drone:{drone.DroneId}";

                redisDatabase.HashSet(droneKey, new[]
                {
                new HashEntry("Model", drone.Model),
                new HashEntry("Manufacturer", drone.Manufacturer),
                new HashEntry("YearOfManufacture", drone.YearOfManufacture.ToString()),
                new HashEntry("Specifications", drone.Specifications),
                new HashEntry("MissionIds", string.Join(",", drone.MissionIds)),
                new HashEntry("LocationIds", string.Join(",", drone.LocationIds))
            });
            }
        }
      
    }
}
