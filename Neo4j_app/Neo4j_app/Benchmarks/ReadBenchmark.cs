using BenchmarkDotNet.Attributes;
using Bogus;
using Neo4j.Driver;
using Neo4j_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] 
    public class ReadBenchmark
    {
        [Params(500)]
        public int Count { get; set; }
        private static IDriver _driver;
        [GlobalSetup]
        public void Setup()
        {
            _driver = AppDbContext._driver;
        }
        [Benchmark]
        public async Task TestRead_Relacja1_1()
        {
            var session = _driver.AsyncSession();
            var result = await session.RunAsync(
                "MATCH (p:Pilot)-[:HAS_INSURANCE]->(i:Insurance) " +
                "RETURN p.PilotId AS PilotId, p.FirstName AS PilotFirstName, p.LastName AS PilotLastName, " +
                "i.InsuranceId AS InsuranceId, i.InsuranceProvider AS InsuranceProvider, " +
                "i.PolicyNumber AS PolicyNumber, i.EndDate AS InsuranceEndDate"
            );

            var records = await result.ToListAsync();

            var pilots = new List<Pilot>();
            var insurances = new List<Insurance>();

            foreach (var record in records)
            {
                
                var pilot = new Pilot
                {
                    PilotId = record["PilotId"].As<int>(),
                    FirstName = record["PilotFirstName"].As<string>(),
                    LastName = record["PilotLastName"].As<string>(),
                    InsuranceId = record["InsuranceId"].As<int>()
                };

                
                pilots.Add(pilot);

                
                var insurance = new Insurance
                {
                    InsuranceId = record["InsuranceId"].As<int>(),
                    InsuranceProvider = record["InsuranceProvider"].As<string>(),
                    PolicyNumber = record["PolicyNumber"].As<string>(),
                    EndDate = record["InsuranceEndDate"].As<DateTime>(),
                    PilotId = record["PilotId"].As<int>()
                };

                
                insurances.Add(insurance);
            }

        }
         [Benchmark]
        public async Task TestRead_BezRelacji()
        {
            var session = _driver.AsyncSession();
            try
            {
                var result = await session.RunAsync(
                    "MATCH (p:Pilot) " +
                    "RETURN p"
                );

                var records = await result.ToListAsync();
                var pilots = new List<Pilot>(); 

                foreach (var record in records)
                {
                    var pilotNode = record["p"].As<INode>();

                    
                    var properties = pilotNode.Properties;

                    
                    var pilot = new Pilot
                    {
                        PilotId = properties.ContainsKey("PilotId") ? Convert.ToInt32(properties["PilotId"]) : 0,
                        FirstName = properties.ContainsKey("FirstName") ? properties["FirstName"].ToString() : string.Empty,
                        LastName = properties.ContainsKey("LastName") ? properties["LastName"].ToString() : string.Empty,
                        LicenseNumber = properties.ContainsKey("LicenseNumber") ? properties["LicenseNumber"].ToString() : string.Empty
                    };

                    pilots.Add(pilot); 
                }

            }
            finally
            {
                await session.CloseAsync();
            }
        }
        [Benchmark]

        public async Task TestRead_Relacje1N()
        {
            var session = _driver.AsyncSession();

            try
            {
                
                var result = await session.RunAsync(
                    "MATCH (d:Drone)-[:HAS_MISSION]->(m:Mission), (d)-[:HAS_LOCATION]->(l:Location) " +
                    "RETURN d.DroneId AS DroneId, d.Model AS DroneModel, d.Manufacturer AS DroneManufacturer, " +
                    "m.MissionId AS MissionId, m.MissionName AS MissionName, " +
                    "l.LocationId AS LocationId, l.Latitude AS LocationLatitude, " +
                    "l.Longitude AS LocationLongitude, l.Altitude AS LocationAltitude, l.Timestamp AS LocationTimestamp"
                );

                var records = await result.ToListAsync();

                
                var droneList = new List<Drone>();  
                var missionList = new List<Mission>(); 
                var locationList = new List<Location>(); 

                
                var droneMissionMap = new Dictionary<int, List<int>>();  
                var droneLocationMap = new Dictionary<int, List<int>>();  

                foreach (var record in records)
                {
                    var droneId = record["DroneId"].As<int>();
                    var droneModel = record["DroneModel"].As<string>();
                    var droneManufacturer = record["DroneManufacturer"].As<string>();
                    var missionId = record["MissionId"].As<int>();
                    var missionName = record["MissionName"].As<string>();

                    var locationId = record["LocationId"].As<int>();
                    var locationLatitude = record["LocationLatitude"].As<double>();
                    var locationLongitude = record["LocationLongitude"].As<double>();

                    
                    var mission = new Mission
                    {
                        MissionId = missionId,
                        MissionName = missionName,
                    };

                    
                    missionList.Add(mission);

                    
                    var location = new Location
                    {
                        LocationId = locationId,
                        Latitude = locationLatitude,
                        Longitude = locationLongitude,
                    };

                    
                    locationList.Add(location);

                    
                    if (!droneMissionMap.ContainsKey(droneId))
                    {
                        droneMissionMap[droneId] = new List<int>();
                    }
                    if (!droneLocationMap.ContainsKey(droneId))
                    {
                        droneLocationMap[droneId] = new List<int>();
                    }

                    
                    droneMissionMap[droneId].Add(missionId);
                    droneLocationMap[droneId].Add(locationId);
                }

                
                foreach (var record in records)
                {
                    var droneId = record["DroneId"].As<int>();
                    var droneModel = record["DroneModel"].As<string>();
                    var droneManufacturer = record["DroneManufacturer"].As<string>();

                    var drone = new Drone
                    {
                        DroneId = droneId,
                        Model = droneModel,
                        Manufacturer = droneManufacturer,
                        MissionIds = droneMissionMap.ContainsKey(droneId) ? droneMissionMap[droneId] : new List<int>(),
                        LocationIds = droneLocationMap.ContainsKey(droneId) ? droneLocationMap[droneId] : new List<int>()
                    };

                    
                    droneList.Add(drone);
                }

                foreach (var drone in droneList)
                {
                    foreach (var missionId in drone.MissionIds)
                    {
                        var mission = missionList.FirstOrDefault(m => m.MissionId == missionId);
    
                    }
                    foreach (var locationId in drone.LocationIds)
                    {
                        var location = locationList.FirstOrDefault(l => l.LocationId == locationId);

                    }
                    Console.WriteLine();
                }
            }
            finally
            {
                await session.CloseAsync();
            }
        }
        [Benchmark]
        public async Task TestRead_RelacjaNM()
        {
            var session = _driver.AsyncSession();

            
            var pilots = new List<Pilot>();
            var missions = new List<Mission>();

            try
            {
                
                var result = await session.RunAsync(
                    "MATCH (p:Pilot)-[:ASSIGNED_TO]->(m:Mission) " +
                    "RETURN p.PilotId AS PilotId, p.FirstName AS FirstName, p.LastName AS LastName, p.LicenseNumber AS LicenseNumber, " +
                    "p.InsuranceId AS InsuranceId, m.MissionId AS MissionId, m.MissionName AS MissionName, " +
                    "m.StartTime AS StartTime, m.EndTime AS EndTime, m.Status AS Status, m.DroneId AS DroneId"
                );

                
                var records = await result.ToListAsync();

                
                foreach (var record in records)
                {
                    
                    var pilotId = record["PilotId"].As<int>();  
                    var firstName = record["FirstName"].As<string>();
                    var lastName = record["LastName"].As<string>();
                    var licenseNumber = record["LicenseNumber"].As<string>();

                    
                    var insuranceId = record["InsuranceId"].As<int?>();

                    var missionId = record["MissionId"].As<int>();  
                    var missionName = record["MissionName"].As<string>();
                    var startTime = record["StartTime"].As<ZonedDateTime>();  
                    var endTime = record["EndTime"].As<ZonedDateTime>();      
                    var status = record["Status"].As<string>();

                    
                    DateTime startDateTime = startTime.ToDateTimeOffset().DateTime;
                    DateTime endDateTime = endTime.ToDateTimeOffset().DateTime;

                    
                    var pilot = new Pilot
                    {
                        PilotId = pilotId,
                        FirstName = firstName,
                        LastName = lastName,
                        LicenseNumber = licenseNumber,
                        InsuranceId = insuranceId  
                    };

                    var mission = new Mission
                    {
                        MissionId = missionId,
                        MissionName = missionName,
                        StartTime = startDateTime,  
                        EndTime = endDateTime,      
                        Status = status,
                    };

                    
                    pilots.Add(pilot);
                    missions.Add(mission);
                }

            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }


}
