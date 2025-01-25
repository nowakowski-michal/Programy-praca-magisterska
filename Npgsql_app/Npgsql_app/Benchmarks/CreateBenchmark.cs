﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Bogus;
using Npgsql;
using Npgsql_app.Models;
using System;
using System.Collections.Generic;
using System.Data;

namespace Npgsql_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class CreateBenchmark
    {
        private static string connectionString = AppDbContext.connectionString;
        private List<Drone> drones = new List<Drone>();
        private List<Insurance> insurances = new List<Insurance>();
        private List<Pilot> pilots = new List<Pilot>();
        private List<Mission> missions = new List<Mission>();
        private List<Location> locations = new List<Location>();

        [Params(100,1000,5000,10000)]
        public int Count { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            int seed = 12345;

            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            drones = droneFaker.Generate(Count);

            var insuranceFaker = new Faker<Insurance>()
                .RuleFor(i => i.InsuranceProvider, f => f.Company.CompanyName())
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.EndDate, f => f.Date.Future())
                .UseSeed(seed);

            insurances = insuranceFaker.Generate(Count);

            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            pilots = pilotFaker.Generate(Count);

            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            missions = missionFaker.Generate(Count);

            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            locations = locationFaker.Generate(Count);

            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].Insurance = insurances[i];
                pilots[i].Insurance.PilotId = pilots[i].PilotId;
            }
        }

        [IterationSetup]
        public void CleanDatabase()
        {
            string[] tables = new[] { "PilotMission", "Missions", "Insurance", "Pilots", "Locations", "Drones" };

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var table in tables)
                    {
                        using (var command = new NpgsqlCommand($"DELETE FROM {table}", connection, transaction))
                        {
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
            }
        }

        [Benchmark]
        public void GenerateAllData()
        {
            int seed = 12345;
            Random rand = new Random(seed);

            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var pilot in pilots)
                    {
                        string sql = @"
                    INSERT INTO Pilots (FirstName, LastName, LicenseNumber) 
                    VALUES (@FirstName, @LastName, @LicenseNumber)
                    RETURNING PilotId;";

                        using (var command = new NpgsqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@FirstName", pilot.FirstName);
                            command.Parameters.AddWithValue("@LastName", pilot.LastName);
                            command.Parameters.AddWithValue("@LicenseNumber", pilot.LicenseNumber);

                            pilot.PilotId = (int)command.ExecuteScalar();
                        }

                        string insuranceSql = @"
                    INSERT INTO Insurance (InsuranceProvider, PolicyNumber, EndDate, PilotId) 
                    VALUES (@InsuranceProvider, @PolicyNumber, @EndDate, @PilotId);";

                        using (var command = new NpgsqlCommand(insuranceSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@InsuranceProvider", pilot.Insurance.InsuranceProvider);
                            command.Parameters.AddWithValue("@PolicyNumber", pilot.Insurance.PolicyNumber);
                            command.Parameters.AddWithValue("@EndDate", pilot.Insurance.EndDate);
                            command.Parameters.AddWithValue("@PilotId", pilot.PilotId);

                            command.ExecuteNonQuery(); 
                        }
                    }
                    foreach (var drone in drones)
                    {
                        string droneSql = @"
                    INSERT INTO Drones (Model, Manufacturer, YearOfManufacture, Specifications)
                    VALUES (@Model, @Manufacturer, @YearOfManufacture, @Specifications)
                    RETURNING DroneId;";

                        using (var command = new NpgsqlCommand(droneSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Model", drone.Model);
                            command.Parameters.AddWithValue("@Manufacturer", drone.Manufacturer);
                            command.Parameters.AddWithValue("@YearOfManufacture", drone.YearOfManufacture);
                            command.Parameters.AddWithValue("@Specifications", drone.Specifications);

                            drone.DroneId = (int)command.ExecuteScalar();
                        }

                        var randomLocations = availableLocations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                        drone.Locations = randomLocations;
                        availableLocations.RemoveAll(loc => randomLocations.Contains(loc));

                        foreach (var location in randomLocations)
                        {
                            string locationSql = @"
                        INSERT INTO Locations (Latitude, Longitude, Altitude, Timestamp, DroneId)
                        VALUES (@Latitude, @Longitude, @Altitude, @Timestamp, @DroneId);";

                            using (var command = new NpgsqlCommand(locationSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Latitude", location.Latitude);
                                command.Parameters.AddWithValue("@Longitude", location.Longitude);
                                command.Parameters.AddWithValue("@Altitude", location.Altitude);
                                command.Parameters.AddWithValue("@Timestamp", location.Timestamp);
                                command.Parameters.AddWithValue("@DroneId", drone.DroneId);

                                command.ExecuteNonQuery(); 
                            }
                        }

                        var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(3).ToList();
                        drone.Missions = randomMissions;
                        availableMissions.RemoveAll(m => randomMissions.Contains(m));

                        foreach (var mission in randomMissions)
                        {
                            string missionSql = @"
                        INSERT INTO Missions (MissionName, StartTime, EndTime, Status, DroneId)
                        VALUES (@MissionName, @StartTime, @EndTime, @Status, @DroneId)
                        RETURNING MissionId;";

                            using (var command = new NpgsqlCommand(missionSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@MissionName", mission.MissionName);
                                command.Parameters.AddWithValue("@StartTime", mission.StartTime);
                                command.Parameters.AddWithValue("@EndTime", mission.EndTime);
                                command.Parameters.AddWithValue("@Status", mission.Status);
                                command.Parameters.AddWithValue("@DroneId", drone.DroneId);

                                mission.MissionId = (int)command.ExecuteScalar();
                            }

                            var randomPilot = pilots[rand.Next(pilots.Count)];
                            string pilotMissionSql = @"
                        INSERT INTO PilotMission (PilotId, MissionId)
                        VALUES (@PilotId, @MissionId);";

                            using (var command = new NpgsqlCommand(pilotMissionSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@PilotId", randomPilot.PilotId); 
                                command.Parameters.AddWithValue("@MissionId", mission.MissionId);

                                command.ExecuteNonQuery(); 
                            }
                        }
                    }

                    transaction.Commit();
                } 
            }
        }
        [Benchmark]
        public void GenerateDronesWithRelations()
        {
            int seed = 12345;
            Random rand = new Random(seed);

            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                { 

                    foreach (var drone in drones)
                    {
                        string droneSql = @"
                    INSERT INTO Drones (Model, Manufacturer, YearOfManufacture, Specifications)
                    VALUES (@Model, @Manufacturer, @YearOfManufacture, @Specifications)
                    RETURNING DroneId;";

                        using (var command = new NpgsqlCommand(droneSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Model", drone.Model);
                            command.Parameters.AddWithValue("@Manufacturer", drone.Manufacturer);
                            command.Parameters.AddWithValue("@YearOfManufacture", drone.YearOfManufacture);
                            command.Parameters.AddWithValue("@Specifications", drone.Specifications);

                            drone.DroneId = (int)command.ExecuteScalar();
                        }

                        var randomLocations = availableLocations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                        drone.Locations = randomLocations;
                        availableLocations.RemoveAll(loc => randomLocations.Contains(loc));

                        foreach (var location in randomLocations)
                        {
                            string locationSql = @"
                        INSERT INTO Locations (Latitude, Longitude, Altitude, Timestamp, DroneId)
                        VALUES (@Latitude, @Longitude, @Altitude, @Timestamp, @DroneId);";

                            using (var command = new NpgsqlCommand(locationSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Latitude", location.Latitude);
                                command.Parameters.AddWithValue("@Longitude", location.Longitude);
                                command.Parameters.AddWithValue("@Altitude", location.Altitude);
                                command.Parameters.AddWithValue("@Timestamp", location.Timestamp);
                                command.Parameters.AddWithValue("@DroneId", drone.DroneId);

                                command.ExecuteNonQuery(); 
                            }
                        }

                        var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(3).ToList();
                        drone.Missions = randomMissions;
                        availableMissions.RemoveAll(m => randomMissions.Contains(m));

                        foreach (var mission in randomMissions)
                        {
                            string missionSql = @"
                        INSERT INTO Missions (MissionName, StartTime, EndTime, Status, DroneId)
                        VALUES (@MissionName, @StartTime, @EndTime, @Status, @DroneId)
                        RETURNING MissionId;";

                            using (var command = new NpgsqlCommand(missionSql, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@MissionName", mission.MissionName);
                                command.Parameters.AddWithValue("@StartTime", mission.StartTime);
                                command.Parameters.AddWithValue("@EndTime", mission.EndTime);
                                command.Parameters.AddWithValue("@Status", mission.Status);
                                command.Parameters.AddWithValue("@DroneId", drone.DroneId);

                                mission.MissionId = (int)command.ExecuteScalar();
                            }
                        }
                    }
                    transaction.Commit();
                } 
            } 
        }
        [Benchmark]
        public void GeneratePilotsWithInsurance()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var pilot in pilots)
                    {
                        string sql = @"
                    INSERT INTO Pilots (FirstName, LastName, LicenseNumber) 
                    VALUES (@FirstName, @LastName, @LicenseNumber)
                    RETURNING PilotId;";

                        using (var command = new NpgsqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@FirstName", pilot.FirstName);
                            command.Parameters.AddWithValue("@LastName", pilot.LastName);
                            command.Parameters.AddWithValue("@LicenseNumber", pilot.LicenseNumber);

                            pilot.PilotId = (int)command.ExecuteScalar(); 
                        }

                        string insuranceSql = @"
                    INSERT INTO Insurance (InsuranceProvider, PolicyNumber, EndDate, PilotId) 
                    VALUES (@InsuranceProvider, @PolicyNumber, @EndDate, @PilotId);";

                        using (var command = new NpgsqlCommand(insuranceSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@InsuranceProvider", pilot.Insurance.InsuranceProvider);
                            command.Parameters.AddWithValue("@PolicyNumber", pilot.Insurance.PolicyNumber);
                            command.Parameters.AddWithValue("@EndDate", pilot.Insurance.EndDate);
                            command.Parameters.AddWithValue("@PilotId", pilot.PilotId);

                            command.ExecuteNonQuery(); 
                        }
                    }
                    transaction.Commit();
                } 
            } 
        }
        [Benchmark]
        public void GeneratePilot()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var pilot in pilots)
                    {
                        string sql = @"
                    INSERT INTO Pilots (FirstName, LastName, LicenseNumber) 
                    VALUES (@FirstName, @LastName, @LicenseNumber)
                    RETURNING PilotId;";

                        using (var command = new NpgsqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@FirstName", pilot.FirstName);
                            command.Parameters.AddWithValue("@LastName", pilot.LastName);
                            command.Parameters.AddWithValue("@LicenseNumber", pilot.LicenseNumber);

                            pilot.PilotId = (int)command.ExecuteScalar(); 
                        }         
                    }
                    transaction.Commit();
                } 
            } 
        }
    }
}
