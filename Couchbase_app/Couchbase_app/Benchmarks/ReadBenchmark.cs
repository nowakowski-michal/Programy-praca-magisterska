using BenchmarkDotNet.Attributes;
using Couchbase;
using Couchbase_app.Models;
using Newtonsoft.Json.Linq;


namespace Couchbase_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class ReadBenchmark
    {
        [Params(1000)]
        public int Count { get; set; }
        private Cluster _cluster;
        [GlobalSetup]
        public void Setup()
        {
            var options = new ClusterOptions
            {
                UserName = "minx1",
                Password = "minx111"
            };
            _cluster = (Cluster?)Cluster.ConnectAsync("couchbase://localhost", options).Result;
        }
        [Benchmark]
        public async Task TestRead_Relacje1N()
        {
            var query = "SELECT * FROM `DronesBucket`.`DronesScope`.`Drones`";
            var result = await _cluster.QueryAsync<dynamic>(query);

            List<Drone> drones = new List<Drone>();
            await foreach (var row in result)
            {
                var drone = new Drone
                {
                    DroneId = row.Drones.droneId,
                    Model = row.Drones.model,
                    Manufacturer = row.Drones.manufacturer,
                    YearOfManufacture = row.Drones.yearOfManufacture,
                    Specifications = row.Drones.specifications,
                    Locations = new List<Location>(),
                    Missions = new List<Mission>()
                };

                // Mapowanie lokalizacji
                if (row.Drones.locations != null)
                {
                    foreach (var location in row.Drones.locations)
                    {
                        var locationObj = new Location
                        {
                            LocationId = location.locationId,
                            Latitude = location.latitude,
                            Longitude = location.longitude,
                            Altitude = location.altitude,
                            // Sprawdzanie i konwertowanie timestamp na DateTime
                            Timestamp = ConvertToDateTime(location.timestamp),
                            DroneId = location.droneId
                        };
                        drone.Locations.Add(locationObj);
                    }
                }

                // Mapowanie misji
                if (row.Drones.missions != null)
                {
                    foreach (var mission in row.Drones.missions)
                    {
                        var missionObj = new Mission
                        {
                            MissionId = mission.missionId,
                            MissionName = mission.missionName,
                            StartTime = ConvertToDateTime(mission.startTime),
                            EndTime = ConvertToDateTime(mission.endTime),
                            Status = mission.status,
                            DroneId = mission.droneId
                        };
                        drone.Missions.Add(missionObj);
                    }
                }
                drones.Add(drone);
            }
        }
        [Benchmark]
        public async Task TestRead_Relacja1_1()
        {
            var query = "SELECT * FROM `DronesBucket`.`DronesScope`.`Pilots`";
            var result = await _cluster.QueryAsync<dynamic>(query);

            List<Pilot> pilots = new List<Pilot>();

            await foreach (var row in result)
            {
                var pilot = new Pilot
                {
                    PilotId = row.Pilots.pilotId,
                    FirstName = row.Pilots.firstName,
                    LastName = row.Pilots.lastName,
                    LicenseNumber = row.Pilots.licenseNumber,
                    Insurance = null
                };

                // Mapowanie ubezpieczenia
                if (row.Pilots.insurance != null)
                {
                    pilot.Insurance = new Insurance
                    {
                        InsuranceId = row.Pilots.insurance.insuranceId,
                        InsuranceProvider = row.Pilots.insurance.insuranceProvider,
                        PolicyNumber = row.Pilots.insurance.policyNumber,
                        EndDate = ConvertToDateTime(row.Pilots.insurance.endDate),
                        PilotId = row.Pilots.insurance.pilotId
                    };
                }
                pilots.Add(pilot);
            }
        }
        [Benchmark]
        public async Task TestRead_BezRelacji()
        {
            var query = "SELECT * FROM `DronesBucket`.`DronesScope`.`Pilots`";
            var result = await _cluster.QueryAsync<dynamic>(query);

            List<Pilot> pilots = new List<Pilot>();
            await foreach (var row in result)
            {
                var pilot = new Pilot
                {
                    PilotId = row.Pilots.pilotId,
                    FirstName = row.Pilots.firstName,
                    LastName = row.Pilots.lastName,
                    LicenseNumber = row.Pilots.licenseNumber
                };
                pilots.Add(pilot);
            }
        }
        [Benchmark]
        public async Task TestRead_RelacjaNM()
        {
            //pobranie id misji i pilotów
            var query = "SELECT * FROM `DronesBucket`.`DronesScope`.`PilotMissions`";
            var result = await _cluster.QueryAsync<dynamic>(query);

            List<PilotMission> pilotMissions = new List<PilotMission>();

            await foreach (var row in result)
            {
                // Mapowanie danych pilota
                var pilot = new Pilot
                {
                    PilotId = row.PilotMissions.pilot.pilotId,
                    FirstName = row.PilotMissions.pilot.firstName,
                    LastName = row.PilotMissions.pilot.lastName,
                    LicenseNumber = row.PilotMissions.pilot.licenseNumber,
                    Insurance = row.PilotMissions.pilot.insurance != null ? new Insurance
                    {
                        InsuranceId = row.PilotMissions.pilot.insurance.insuranceId,
                        InsuranceProvider = row.PilotMissions.pilot.insurance.insuranceProvider,
                        PolicyNumber = row.PilotMissions.pilot.insurance.policyNumber,
                        EndDate = Convert.ToDateTime(row.PilotMissions.pilot.insurance.endDate)  
                    } : null
                };

                // Mapowanie danych misji
                var mission = new Mission
                {
                    MissionId = row.PilotMissions.mission.missionId,
                    MissionName = row.PilotMissions.mission.missionName,
                    StartTime = Convert.ToDateTime(row.PilotMissions.mission.startTime),  
                    EndTime = Convert.ToDateTime(row.PilotMissions.mission.endTime), 
                    Status = row.PilotMissions.mission.status,
                    DroneId = row.PilotMissions.mission.droneId
                };

                var pilotMission = new PilotMission
                {
                    PilotId = row.PilotMissions.pilotId,
                    Pilot = pilot,
                    MissionId = row.PilotMissions.missionId,
                    Mission = mission
                };

                pilotMissions.Add(pilotMission);
            }
        }

        //Konwersja timestamp odebrnaego z bazy na datattime
        private DateTime ConvertToDateTime(dynamic value)
        {
            if (value is JValue jValue && jValue.Type == JTokenType.Date)
            {
                return jValue.ToObject<DateTime>();
            }
            return DateTime.MinValue; 
        }
    }
}
