using BenchmarkDotNet.Attributes;
using Npgsql;
using Npgsql_app.Models;
using System;
using System.Collections.Generic;

namespace Npgsql_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 rzeczywistych pomiarów oraz 3 rozgrzewające
    public class AggregationBenchmark
    {
        [Params(10000)]
        public int NumberOfRows;
        private static string connectionString = AppDbContext.connectionString;

        // Benchmark dla grupowania dronów i zliczania liczby lokalizacji
        [Benchmark]
        public void TestGroupByDrones()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // SQL do grupowania dronów i liczenia liczby lokalizacji przypisanych do każdego drona
                string sql = @"
                    SELECT d.droneid, d.model, COUNT(l.locationid) AS locationcount
                    FROM drones d
                    LEFT JOIN locations l ON d.droneid = l.droneid
                    GROUP BY d.droneid, d.model";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var droneLocationCounts = new List<dynamic>();

                        while (reader.Read())
                        {
                            droneLocationCounts.Add(new
                            {
                                DroneId = reader["droneid"],
                                DroneModel = reader["model"],
                                LocationCount = reader["locationcount"]
                            });
                        }
                    }
                }
            }
        }

        // Benchmark dla grupowania lokalizacji po dacie
        [Benchmark]
        public void TestGroupByDate()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                // SQL do grupowania lokalizacji po dacie (ignorowanie czasu)
                string sql = @"
                    SELECT CAST(l.timestamp AS DATE) AS date, COUNT(l.locationid) AS locationcount
                    FROM locations l
                    GROUP BY CAST(l.timestamp AS DATE)";

                using (var command = new NpgsqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var locationsByDate = new List<dynamic>();

                        while (reader.Read())
                        {
                            locationsByDate.Add(new
                            {
                                Date = reader["date"],
                                LocationCount = reader["locationcount"]
                            });
                        }
                    }
                }
            }
        }
    }
}
