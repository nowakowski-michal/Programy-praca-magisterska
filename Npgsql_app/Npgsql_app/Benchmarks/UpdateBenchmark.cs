using BenchmarkDotNet.Attributes;
using Npgsql;
using Npgsql_app.Models;
using System;
using System.Collections.Generic;

namespace Npgsql_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class UpdateBenchmark
    {
        private static string connectionString = AppDbContext.connectionString;

        [Params(100, 1000)]
        public int NumberOfRows;

        [Benchmark]
        public void TestUpdate_SingleTable()
        {
            Random random = new Random();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT droneid FROM drones LIMIT @NumberOfRows";
                using (var selectCommand = new NpgsqlCommand(selectQuery, connection))
                {
                    selectCommand.Parameters.AddWithValue("@NumberOfRows", NumberOfRows);

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        List<int> droneIds = new List<int>();
                        while (reader.Read())
                        {
                            droneIds.Add(reader.GetInt32(0));
                        }
                        reader.Close();

                        string updateQuery = "UPDATE drones SET specifications = @Specifications WHERE droneid = @DroneId";
                        foreach (var droneId in droneIds)
                        {
                            using (var updateCommand = new NpgsqlCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@Specifications", "Updated Specification " + random.Next(0, 10));
                                updateCommand.Parameters.AddWithValue("@DroneId", droneId);
                                updateCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        [Benchmark]
        public void TestUpdate_WithRelationship()
        {
            Random random = new Random();

            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                string selectQuery = @"
                    SELECT p.pilotid, i.insuranceid
                    FROM pilots p
                    INNER JOIN insurance i ON p.pilotid = i.pilotid
                    WHERE i.policynumber IS NOT NULL
                    LIMIT @NumberOfRows";

                using (var selectCommand = new NpgsqlCommand(selectQuery, connection))
                {
                    selectCommand.Parameters.AddWithValue("@NumberOfRows", NumberOfRows);

                    using (var reader = selectCommand.ExecuteReader())
                    {
                        List<int> insuranceIds = new List<int>();

                        while (reader.Read())
                        {
                            insuranceIds.Add(reader.GetInt32(1)); 
                        }

                        reader.Close();

                        string updateQuery = "UPDATE insurance SET policynumber = @PolicyNumber WHERE insuranceid = @InsuranceId";
                        foreach (var insuranceId in insuranceIds)
                        {
                            using (var updateCommand = new NpgsqlCommand(updateQuery, connection))
                            {
                                updateCommand.Parameters.AddWithValue("@PolicyNumber", "NEW-POLICY-" + random.Next(0, 10));
                                updateCommand.Parameters.AddWithValue("@InsuranceId", insuranceId);
                                updateCommand.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }
    }
}
