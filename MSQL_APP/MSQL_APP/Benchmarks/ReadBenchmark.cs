using BenchmarkDotNet.Attributes;
using Microsoft.Data.SqlClient;
using Msql_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSQL_APP.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class ReadBenchmark
    {
        [Params(10000)]
        public int Count;
        private static string connectionString = AppDbContext.connectionString;
        [Benchmark]
        public void TestRead_Relacje1N()
        {
            var drones = new List<Drone>();

            string query = @"
        SELECT d.DroneId, d.Model, 
               m.MissionId, m.MissionName, 
               l.LocationId, l.Altitude
        FROM Drones d
        INNER JOIN Missions m ON d.DroneId = m.DroneId
        INNER JOIN Locations l ON d.DroneId = l.DroneId";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var dronesDict = new Dictionary<int, Drone>();

                        while (reader.Read())
                        {
                            var droneId = reader.GetInt32(reader.GetOrdinal("DroneId"));
                            if (!dronesDict.ContainsKey(droneId))
                            {
                                dronesDict[droneId] = new Drone
                                {
                                    DroneId = droneId,
                                    Model = reader["Model"].ToString(),
                                    Missions = new List<Mission>(),
                                    Locations = new List<Location>()
                                };
                            }


                            var mission = new Mission
                            {
                                MissionId = reader.GetInt32(reader.GetOrdinal("MissionId")),
                                MissionName = reader["MissionName"].ToString()
                            };

                            var location = new Location
                            {
                                LocationId = reader.GetInt32(reader.GetOrdinal("LocationId")),
                                Altitude = reader.GetDouble(reader.GetOrdinal("Altitude"))
                            };
                            var drone = dronesDict[droneId];
                            if (!drone.Missions.Any(m => m.MissionId == mission.MissionId))
                            {
                                drone.Missions.Add(mission);
                            }

                            if (!drone.Locations.Any(l => l.LocationId == location.LocationId))
                            {
                                drone.Locations.Add(location);
                            }
                        }
                        drones.AddRange(dronesDict.Values);
                    }
                }
            }
        }
        [Benchmark]
        public void TestRead_Relacja1_1()
        {
            List<Pilot> pilots = new List<Pilot>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"
                SELECT 
                    p.PilotId, 
                    p.FirstName, 
                    p.LastName, 
                    p.LicenseNumber, 
                    i.InsuranceId, 
                    i.InsuranceProvider, 
                    i.PolicyNumber, 
                    i.EndDate
                FROM Pilots p
                INNER JOIN Insurance i ON p.PilotId = i.PilotId";

                SqlCommand command = new SqlCommand(query, connection);

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var pilot = new Pilot
                        {
                            PilotId = (int)reader["PilotId"],
                            FirstName = reader["FirstName"].ToString(),
                            LastName = reader["LastName"].ToString(),
                            LicenseNumber = reader["LicenseNumber"].ToString(),
                            Insurance = new Insurance
                            {
                                InsuranceId = (int)reader["InsuranceId"],
                                InsuranceProvider = reader["InsuranceProvider"].ToString(),
                                PolicyNumber = reader["PolicyNumber"].ToString(),
                                EndDate = (DateTime)reader["EndDate"]
                            }
                        };

                        pilots.Add(pilot);
                    }
                }
            }
        }

        [Benchmark]
        public void TestRead_BezRelacji()
        {
            var pilots = new List<Pilot>();

            string query = "SELECT PilotId, FirstName, LastName, LicenseNumber FROM Pilots";

            using (var connection = new SqlConnection(connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var pilot = new Pilot
                            {
                                PilotId = reader.GetInt32(reader.GetOrdinal("PilotId")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                LastName = reader.GetString(reader.GetOrdinal("LastName")),
                                LicenseNumber = reader.GetString(reader.GetOrdinal("LicenseNumber"))
                            };

                            pilots.Add(pilot);
                        }
                    }
                }
            }
        }
        
        [Benchmark]
        public void TestRead_RelacjaNM()
        {
            List<Pilot> pilots = new List<Pilot>();

            // Zapytanie, które pobiera dane z tabeli pośredniczącej, pilota oraz misje
            string query = @"
        SELECT p.PilotId, p.FirstName, p.LastName, p.LicenseNumber, 
               m.MissionId, m.MissionName, m.StartTime, m.EndTime, m.Status
        FROM Pilots p
        INNER JOIN PilotMission pm ON p.PilotId = pm.PilotId
        INNER JOIN Missions m ON pm.MissionId = m.MissionId
        ORDER BY p.PilotId"; 

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        var pilotsDict = new Dictionary<int, Pilot>();

                        while (reader.Read())
                        {
                            int pilotId = reader.GetInt32(reader.GetOrdinal("PilotId"));
                            if (!pilotsDict.ContainsKey(pilotId))
                            {
                                pilotsDict[pilotId] = new Pilot
                                {
                                    PilotId = pilotId,
                                    FirstName = reader["FirstName"].ToString(),
                                    LastName = reader["LastName"].ToString(),
                                    LicenseNumber = reader["LicenseNumber"].ToString(),
                                    PilotMissions = new List<PilotMission>()
                                };
                            }
                            var mission = new Mission
                            {
                                MissionId = reader.GetInt32(reader.GetOrdinal("MissionId")),
                                MissionName = reader["MissionName"].ToString(),
                                StartTime = reader.GetDateTime(reader.GetOrdinal("StartTime")),
                                EndTime = reader.GetDateTime(reader.GetOrdinal("EndTime")),
                                Status = reader["Status"].ToString()
                            };
                            var pilot = pilotsDict[pilotId];
                            if (!pilot.PilotMissions.Any(pm => pm.MissionId == mission.MissionId))
                            {
                                pilot.PilotMissions.Add(new PilotMission
                                {
                                    PilotId = pilotId,
                                    MissionId = mission.MissionId,
                                    Pilot = pilot,
                                    Mission = mission
                                });
                            }
                        }
                        pilots.AddRange(pilotsDict.Values);
                    }
                }
            }
        }
        
    }
}
