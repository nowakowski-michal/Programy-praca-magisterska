using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Redis_app.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redis_app.TestLoad
{
    //przy sprawdzaniu obciążenia zostanie wykonana tylko 3 iteracja testu oraz dwie rozgrzewające, 
    //aby łatwiej zmierzyć użycie zasobów przez aplikacje, dodatkowo zostana wyswietlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 3, warmupCount: 2)]
    public class Read_Load
    {
        public static ConnectionMultiplexer redisConnection;
        public static IDatabase redisDatabase;
        public static IServer server;
        [GlobalSetup]
        public void Setup()
        {
            redisConnection = AppDbContext.redisConnection;
            redisDatabase = AppDbContext.redisDatabase;
            server = AppDbContext.server;
        }
        [Benchmark]
        public void TestRead_Relacje1N()
        {
            var drones = new List<Drone>();
            var droneKeys = server.Keys(pattern: "Drone:*").ToList(); 

            foreach (var droneKey in droneKeys)
            {
                var droneHash = redisDatabase.HashGetAll(droneKey);
                var drone = new Drone
                {
                    DroneId = Convert.ToInt32(droneKey.ToString().Split(':')[1]),
                    Model = droneHash.FirstOrDefault(x => x.Name == "Model").Value,
                    Manufacturer = droneHash.FirstOrDefault(x => x.Name == "Manufacturer").Value,
                    YearOfManufacture = Convert.ToInt32(droneHash.FirstOrDefault(x => x.Name == "YearOfManufacture").Value),
                    Specifications = droneHash.FirstOrDefault(x => x.Name == "Specifications").Value,

                    MissionIds = droneHash.FirstOrDefault(x => x.Name == "MissionIds").Value
                                    .ToString()
                                    .Split(',')
                                    .Where(id => !string.IsNullOrWhiteSpace(id)) 
                                    .Select(id => int.TryParse(id, out var result) ? result : (int?)null)
                                    .Where(id => id.HasValue) 
                                    .Select(id => id.Value) 
                                    .ToList(),

                    LocationIds = droneHash.FirstOrDefault(x => x.Name == "LocationIds").Value
                                    .ToString()
                                    .Split(',')
                                    .Where(id => !string.IsNullOrWhiteSpace(id)) 
                                    .Select(id => int.TryParse(id, out var result) ? result : (int?)null) 
                                    .Where(id => id.HasValue) 
                                    .Select(id => id.Value) 
                                    .ToList()
                };
                drones.Add(drone);
            }

            foreach (var drone in drones)
            {

                foreach (var missionId in drone.MissionIds)
                {
                    var missionKey = $"Mission:{missionId}";
                    var missionHash = redisDatabase.HashGetAll(missionKey);
                    var missionName = missionHash.FirstOrDefault(x => x.Name == "MissionName").Value;
                    var startTime = missionHash.FirstOrDefault(x => x.Name == "StartTime").Value;
                    var endTime = missionHash.FirstOrDefault(x => x.Name == "EndTime").Value;
                    var status = missionHash.FirstOrDefault(x => x.Name == "Status").Value;
                }
                foreach (var locationId in drone.LocationIds)
                {
                    var locationKey = $"Location:{locationId}";
                    var locationHash = redisDatabase.HashGetAll(locationKey);
                    var latitude = locationHash.FirstOrDefault(x => x.Name == "Latitude").Value;
                    var longitude = locationHash.FirstOrDefault(x => x.Name == "Longitude").Value;
                    var altitude = locationHash.FirstOrDefault(x => x.Name == "Altitude").Value;
                    var timestamp = locationHash.FirstOrDefault(x => x.Name == "Timestamp").Value;
                }
            }
        }
        [Benchmark]
        public void TestRead_Relacja1_1()
        {
            var pilots = new List<Pilot>();
            var pilotKeys = server.Keys(pattern: "Pilot:*").ToList();

            foreach (var pilotKey in pilotKeys)
            {
                var pilotHash = redisDatabase.HashGetAll(pilotKey);
                var pilot = new Pilot
                {
                    PilotId = Convert.ToInt32(pilotKey.ToString().Split(':')[1]),
                    FirstName = pilotHash.FirstOrDefault(x => x.Name == "FirstName").Value,
                    LastName = pilotHash.FirstOrDefault(x => x.Name == "LastName").Value,
                    LicenseNumber = pilotHash.FirstOrDefault(x => x.Name == "LicenseNumber").Value,
                    InsuranceId = Convert.ToInt32(pilotHash.FirstOrDefault(x => x.Name == "InsuranceId").Value)
                };
                pilots.Add(pilot);
            }

            foreach (var pilot in pilots)
            {

                var insuranceKey = $"Insurance:{pilot.InsuranceId}";
                var insuranceHash = redisDatabase.HashGetAll(insuranceKey);
                var insuranceProvider = insuranceHash.FirstOrDefault(x => x.Name == "InsuranceProvider").Value;
                var policyNumber = insuranceHash.FirstOrDefault(x => x.Name == "PolicyNumber").Value;
                var endDate = insuranceHash.FirstOrDefault(x => x.Name == "EndDate").Value;
            }
        }
        [Benchmark]
        public void TestRead_BezRelacji()
        {

            var pilots = new List<Pilot>();
            var pilotKeys = server.Keys(pattern: "Pilot:*").ToList(); 

            foreach (var pilotKey in pilotKeys)
            {
                var pilotHash = redisDatabase.HashGetAll(pilotKey);
                var pilot = new Pilot
                {
                    PilotId = Convert.ToInt32(pilotKey.ToString().Split(':')[1]),
                    FirstName = pilotHash.FirstOrDefault(x => x.Name == "FirstName").Value,
                    LastName = pilotHash.FirstOrDefault(x => x.Name == "LastName").Value,
                    LicenseNumber = pilotHash.FirstOrDefault(x => x.Name == "LicenseNumber").Value
                };
                pilots.Add(pilot);
            }
        }
        [Benchmark]
        public void TestRead_RelacjaNM()
        {
            var pilotKeys = server.Keys(pattern: "PilotMission:*").ToList(); 
            foreach (var pilotKey in pilotKeys)
            {
                var pilotMissionHash = redisDatabase.HashGetAll(pilotKey);
                var pilotMission = new PilotMission
                {
                    PilotId = Convert.ToInt32(pilotKey.ToString().Split(':')[1]),
                    MissionId = Convert.ToInt32(pilotKey.ToString().Split(':')[2])
                };
                var pilotKeyForDetails = $"Pilot:{pilotMission.PilotId}";
                if (redisDatabase.KeyType(pilotKeyForDetails) == RedisType.Hash)
                {
                    var pilotHash = redisDatabase.HashGetAll(pilotKeyForDetails);

                    var pilot = new Pilot
                    {
                        PilotId = pilotMission.PilotId,
                        FirstName = pilotHash.FirstOrDefault(x => x.Name == "FirstName").Value,
                        LastName = pilotHash.FirstOrDefault(x => x.Name == "LastName").Value,
                        LicenseNumber = pilotHash.FirstOrDefault(x => x.Name == "LicenseNumber").Value

                    };
                }
                var missionKeyForDetails = $"Mission:{pilotMission.MissionId}";
                var missionKeyType = redisDatabase.KeyType(missionKeyForDetails);
                if (missionKeyType == RedisType.Hash)
                {
                    var missionHash = redisDatabase.HashGetAll(missionKeyForDetails);
                    var mission = new Mission
                    {
                        MissionId = pilotMission.MissionId,
                        MissionName = missionHash.FirstOrDefault(x => x.Name == "MissionName").Value,
                        StartTime = DateTime.Parse(missionHash.FirstOrDefault(x => x.Name == "StartTime").Value),
                        EndTime = DateTime.Parse(missionHash.FirstOrDefault(x => x.Name == "EndTime").Value),
                        Status = missionHash.FirstOrDefault(x => x.Name == "Status").Value,
                        DroneId = Convert.ToInt32(missionHash.FirstOrDefault(x => x.Name == "DroneId").Value)
                    };
                }
            }
        }
    }
}
