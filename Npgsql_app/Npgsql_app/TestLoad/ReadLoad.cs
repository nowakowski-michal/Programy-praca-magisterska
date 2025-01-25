using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Npgsql;
using Npgsql_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Npgsql_app.TestLoad
{
    //przy sprawdzaniu obciążenia zostanie wykonana tylko 3 iteracja testu oraz dwie rozgrzewające, 
    //aby łatwiej zmierzyć użycie zasobów przez aplikacje, dodatkowo zostana wyswietlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 3, warmupCount: 2)]
    public class Read_Load
    {
        private static string connectionString = AppDbContext.connectionString;

        [Benchmark]
        public void TestRead_Relacje1N()
        {
            var dronesList = new List<Drone>();

            string query = @"
                SELECT d.DroneId, d.Model, 
                       m.MissionId, m.MissionName, 
                       l.LocationId, l.Altitude
                FROM Drones d
                LEFT JOIN Missions m ON d.DroneId = m.DroneId
                LEFT JOIN Locations l ON d.DroneId = l.DroneId";

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                using (var command = new NpgsqlCommand(query, connection))
                {
                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        var drones = new Dictionary<int, Drone>();

                        while (reader.Read())
                        {
                            int droneId = reader.GetInt32(reader.GetOrdinal("DroneId"));
                            string model = reader.GetString(reader.GetOrdinal("Model"));
                            int missionId = reader.IsDBNull(reader.GetOrdinal("MissionId")) ? -1 : reader.GetInt32(reader.GetOrdinal("MissionId"));
                            string missionName = reader.IsDBNull(reader.GetOrdinal("MissionName")) ? null : reader.GetString(reader.GetOrdinal("MissionName"));
                            int locationId = reader.IsDBNull(reader.GetOrdinal("LocationId")) ? -1 : reader.GetInt32(reader.GetOrdinal("LocationId"));
                            double altitude = reader.IsDBNull(reader.GetOrdinal("Altitude")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("Altitude"));

                            if (!drones.ContainsKey(droneId))
                            {
                                drones[droneId] = new Drone
                                {
                                    DroneId = droneId,
                                    Model = model,
                                    Missions = new List<Mission>(),
                                    Locations = new List<Location>()
                                };
                            }

                            var drone = drones[droneId];
                            if (missionId != -1)
                            {
                                drone.Missions.Add(new Mission
                                {
                                    MissionId = missionId,
                                    MissionName = missionName
                                });
                            }
                            if (locationId != -1)
                            {
                                drone.Locations.Add(new Location
                                {
                                    LocationId = locationId,
                                    Altitude = altitude
                                });
                            }
                        }
                        dronesList.AddRange(drones.Values);
                    }
                }
            }
        }

        [Benchmark]
        public void TestRead_Relacja1_1()
        {
            List<Pilot> pilots = new List<Pilot>();

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
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

                NpgsqlCommand command = new NpgsqlCommand(query, connection);

                connection.Open();

                using (NpgsqlDataReader reader = command.ExecuteReader())
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

            using (var connection = new NpgsqlConnection(connectionString))
            {
                using (var command = new NpgsqlCommand(query, connection))
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

            string query = @"
                SELECT p.PilotId, p.FirstName, p.LastName, p.LicenseNumber, 
                       m.MissionId, m.MissionName, m.StartTime, m.EndTime, m.Status
                FROM Pilots p
                INNER JOIN PilotMission pm ON p.PilotId = pm.PilotId
                INNER JOIN Missions m ON pm.MissionId = m.MissionId
                ORDER BY p.PilotId";

            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
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
