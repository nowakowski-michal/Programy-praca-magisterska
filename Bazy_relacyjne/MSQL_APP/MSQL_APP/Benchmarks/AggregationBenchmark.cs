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
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 rzeczywistych pomiarów oraz 3 rozgrzewające
    public class AggregationBenchmark
    {
        [Params(10000)]
        public int Count;
        private static string connectionString = AppDbContext.connectionString;
        // Benchmark dla grupowania dronów i zliczania liczby lokalizacji
        [Benchmark]
        public void TestGroupByDrones()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // SQL do grupowania dronów i liczenia liczby lokalizacji przypisanych do każdego drona
                string sql = @"
                    SELECT d.DroneId, d.Model, COUNT(l.LocationId) AS LocationCount
                    FROM Drones d
                    LEFT JOIN Locations l ON d.DroneId = l.DroneId
                    GROUP BY d.DroneId, d.Model";

                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var droneLocationCounts = new List<dynamic>();

                        while (reader.Read())
                        {
                            droneLocationCounts.Add(new
                            {
                                DroneId = reader["DroneId"],
                                DroneModel = reader["Model"],
                                LocationCount = reader["LocationCount"]
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
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // SQL do grupowania lokalizacji po dacie (ignorowanie czasu)
                string sql = @"
                    SELECT CAST(l.Timestamp AS DATE) AS Date, COUNT(l.LocationId) AS LocationCount
                    FROM Locations l
                    GROUP BY CAST(l.Timestamp AS DATE)";

                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var locationsByDate = new List<dynamic>();

                        while (reader.Read())
                        {
                            locationsByDate.Add(new
                            {
                                Date = reader["Date"],
                                LocationCount = reader["LocationCount"]
                            });
                        }
                    }
                }
            }
        }
       
    }

}
