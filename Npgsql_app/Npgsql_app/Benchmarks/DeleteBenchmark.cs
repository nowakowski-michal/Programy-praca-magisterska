using BenchmarkDotNet.Attributes;
using Npgsql;
using Npgsql_app.Models;
using System;

namespace Npgsql_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class DeleteBenchmark
    {
        private static string connectionString = AppDbContext.connectionString;

        [Params(100, 1000)]
        public int NumberOfRows;

        [IterationSetup]
        public void IterationSetup()
        {
            var generateData = new GenerateData
            {
                Count = 1000
            };
            generateData.GenerateForDelete();
        }
        [Benchmark]
        public void TestDelete_PilotWithoutInsurance()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string sqlQuery = "DELETE FROM pilots WHERE pilotid IN (SELECT pilotid FROM pilots LIMIT @NumberOfPilots)";

                using (var command = new NpgsqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@NumberOfPilots", NumberOfRows);
                    command.ExecuteNonQuery();
                }
            }
        }
        [Benchmark]
        public void TestDelete_DronesWithCascade()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                var deleteMissionsCmd = new NpgsqlCommand(
                    "DELETE FROM missions WHERE droneid IN (SELECT droneid FROM drones LIMIT @NumberOfRows)",
                    connection);
                deleteMissionsCmd.Parameters.AddWithValue("@NumberOfRows", NumberOfRows);
                deleteMissionsCmd.ExecuteNonQuery();

                var deleteLocationsCmd = new NpgsqlCommand(
                    "DELETE FROM locations WHERE droneid IN (SELECT droneid FROM drones LIMIT @NumberOfRows)",
                    connection);
                deleteLocationsCmd.Parameters.AddWithValue("@NumberOfRows", NumberOfRows);
                deleteLocationsCmd.ExecuteNonQuery();

                var deleteDronesCmd = new NpgsqlCommand(
                    "DELETE FROM drones WHERE droneid IN (SELECT droneid FROM drones LIMIT @NumberOfRows)",
                    connection);
                deleteDronesCmd.Parameters.AddWithValue("@NumberOfRows", NumberOfRows);
                deleteDronesCmd.ExecuteNonQuery();
            }
        }
    }
}

