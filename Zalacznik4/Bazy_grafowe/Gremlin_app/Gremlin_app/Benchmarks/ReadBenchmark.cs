using BenchmarkDotNet.Attributes;
using Gremlin.Net.Driver;
using Gremlin_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremlin_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class ReadBenchmark
    {
        [Params(100)]
        public int Count { get; set; }
        private static GremlinClient _client;
        [GlobalSetup]
        public void Setup()
        {
            _client = AppDbContext.client;
        }
        [Benchmark]

        public async Task TestRead_Relacje1N()
        {
            try
            {
                // Zapytanie do pobrania dronów, misji i lokalizacji
                var query = "g.V().hasLabel('drone').as('d').both('HAS_MISSION', 'HAS_LOCATION').as('m', 'l').select('d', 'm', 'l').by(valueMap()).by(valueMap()).by(valueMap()).dedup()";
                var results = await _client.SubmitAsync<dynamic>(query);

                var drones = new List<Drone>();
                var missions = new List<Mission>();
                var locations = new List<Location>();

                foreach (var result in results)
                {
                    // Dron
                    var drone = result["d"];
                    int droneId = 0;
                    if (drone.ContainsKey("DroneId"))
                    {
                        int.TryParse(string.Join(", ", drone["DroneId"]), out droneId); 
                    }

                    var model = drone.ContainsKey("Model") ? string.Join(", ", drone["Model"]) : "N/A";
                    var manufacturer = drone.ContainsKey("Manufacturer") ? string.Join(", ", drone["Manufacturer"]) : "N/A";
                    var yearOfManufacture = drone.ContainsKey("YearOfManufacture") ? int.Parse(string.Join(", ", drone["YearOfManufacture"])) : 0;
                    var specifications = drone.ContainsKey("Specifications") ? string.Join(", ", drone["Specifications"]) : "N/A";

                    drones.Add(new Drone
                    {
                        DroneId = droneId,
                        Model = model,
                        Manufacturer = manufacturer,
                        YearOfManufacture = yearOfManufacture,
                        Specifications = specifications,
                        MissionIds = new List<int>(), 
                        LocationIds = new List<int>()
                    });

                    if (result["m"] != null)
                    {
                        var mission = result["m"];
                        int missionId = 0;
                        if (mission.ContainsKey("MissionId"))
                        {
                            int.TryParse(string.Join(", ", mission["MissionId"]), out missionId); 
                        }
                        var missionName = mission.ContainsKey("MissionName") ? string.Join(", ", mission["MissionName"]) : "N/A";
                        var missionStatus = mission.ContainsKey("Status") ? string.Join(", ", mission["Status"]) : "N/A";
                        var missionStartTime = mission.ContainsKey("StartTime") ? DateTime.Parse(string.Join(", ", mission["StartTime"])) : DateTime.MinValue;
                        var missionEndTime = mission.ContainsKey("EndTime") ? DateTime.Parse(string.Join(", ", mission["EndTime"])) : DateTime.MinValue;

                        missions.Add(new Mission
                        {
                            MissionId = missionId,
                            MissionName = missionName,
                            Status = missionStatus,
                            StartTime = missionStartTime,
                            EndTime = missionEndTime,
                            DroneId = droneId 
                        });  
                    }

                    if (result["l"] != null)
                    {
                        var location = result["l"];
                        int locationId = 0;
                        if (location.ContainsKey("LocationId"))
                        {
                            int.TryParse(string.Join(", ", location["LocationId"]), out locationId); 
                        }

                        var latitude = location.ContainsKey("Latitude") ? double.Parse(string.Join(", ", location["Latitude"])) : 0;
                        var longitude = location.ContainsKey("Longitude") ? double.Parse(string.Join(", ", location["Longitude"])) : 0;
                        var altitude = location.ContainsKey("Altitude") ? double.Parse(string.Join(", ", location["Altitude"])) : 0;
                        var timestamp = location.ContainsKey("Timestamp") ? DateTime.Parse(string.Join(", ", location["Timestamp"])) : DateTime.MinValue;

                        locations.Add(new Location
                        {
                            LocationId = locationId,
                            Latitude = latitude,
                            Longitude = longitude,
                            Altitude = altitude,
                            Timestamp = timestamp,
                            DroneId = droneId 
                        });             
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while retrieving data: {ex.Message}");
            }
        }
        [Benchmark]
        public async Task TestRead_BezRelacji()
        {
            var pilots = new List<Pilot>();
                var query = "g.V().hasLabel('pilot').valueMap()";
                var results = await _client.SubmitAsync<dynamic>(query);
                foreach (var result in results)
                {
                    var pilot = new Pilot
                    {
                        PilotId = result.ContainsKey("PilotId") ? int.Parse(result["PilotId"][0].ToString()) : 0,
                        FirstName = result.ContainsKey("FirstName") ? result["FirstName"][0].ToString() : "N/A",
                        LastName = result.ContainsKey("LastName") ? result["LastName"][0].ToString() : "N/A",
                        LicenseNumber = result.ContainsKey("LicenseNumber") ? result["LicenseNumber"][0].ToString() : "N/A"
                    };
                    pilots.Add(pilot);        
                }
        }
        [Benchmark]
        public async Task TestRead_Relacja1_1()
        {

            var query = "g.V().hasLabel('pilot').as('p').out('HAS_INSURANCE').as('i').select('p', 'i').by(valueMap())";
            var result = await _client.SubmitAsync<dynamic>(query);
            var pilots = new List<Pilot>(); 
            var insurances = new List<Insurance>(); 

            foreach (var record in result)
            {
                var pilot = record["p"];
                var pilotId = Convert.ToInt32(pilot["PilotId"][0]);
                var firstName = pilot["FirstName"][0].ToString();
                var lastName = pilot["LastName"][0].ToString();
                var licenseNumber = pilot["LicenseNumber"][0].ToString();
                var newPilot = new Pilot
                {
                    PilotId = pilotId,
                    FirstName = firstName,
                    LastName = lastName,
                    LicenseNumber = licenseNumber
                };
                pilots.Add(newPilot);

                var insurance = record["i"];
                var insuranceId = Convert.ToInt32(insurance["InsuranceId"][0]);
                var policyNumber = insurance["PolicyNumber"][0].ToString();
                var insuranceProvider = insurance["InsuranceProvider"][0].ToString();
                var endDate = Convert.ToDateTime(insurance["EndDate"][0]);

                var newInsurance = new Insurance
                {
                    InsuranceId = insuranceId,
                    InsuranceProvider = insuranceProvider,
                    PolicyNumber = policyNumber,
                    EndDate = endDate,
                    PilotId = pilotId 

                };
                insurances.Add(newInsurance);
            }
        }
        [Benchmark]
        public async Task TestRead_RelacjaNM()
        {
            try
            {
                // Zapytanie Gremlina dla wszystkich pilotów i ich misji
                var query = "g.V().hasLabel('pilot').as('p').out('ASSIGNED_TO').as('m').select('p', 'm').by(valueMap()).by(valueMap())";
                var results = await _client.SubmitAsync<dynamic>(query);

                foreach (var result in results)
                {
                    // Pilot
                    var pilot = result["p"];
                    var pilotId = pilot.ContainsKey("PilotId") ? string.Join(", ", pilot["PilotId"]) : "N/A";
                    var firstName = pilot.ContainsKey("FirstName") ? string.Join(", ", pilot["FirstName"]) : "N/A";
                    var lastName = pilot.ContainsKey("LastName") ? string.Join(", ", pilot["LastName"]) : "N/A";

                    // Misja
                    if (result["m"] != null)
                    {
                        var mission = result["m"];
                        var missionId = mission.ContainsKey("MissionId") ? string.Join(", ", mission["MissionId"]) : "N/A";
                        var missionName = mission.ContainsKey("MissionName") ? string.Join(", ", mission["MissionName"]) : "N/A";
                        var missionStatus = mission.ContainsKey("Status") ? string.Join(", ", mission["Status"]) : "N/A";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while retrieving data: {ex.Message}");
            }
        }
    }
}
