using BenchmarkDotNet.Attributes;
using Msql_app.Models;
using MSQL_APP.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;

namespace MSQL_APP.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class DeleteBenchmark
    {
        // Dane połączeniowe ADO.NET
        private static string connectionString = AppDbContext.connectionString;
        [Params(100, 1000)]
        public int NumberOfRows;

        [IterationSetup]
        public void IterationSetup()
        {

            GenerateData generateData = new GenerateData();
            generateData.Count = 1000;
            generateData.GenerateForDelete();
        }
        [Benchmark]
        public void TestDelete_PilotWithoutInsurance()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string sqlQuery = "DELETE TOP (@NumberOfPilots) FROM Pilots";

                using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                {
                    command.Parameters.AddWithValue("@NumberOfPilots", NumberOfRows);

                    command.ExecuteNonQuery();
   
                }
            }
        }
        [Benchmark]
        public void TestDelete_DronesWithCascade()
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var deleteMissionsCmd = new SqlCommand(
                    "DELETE FROM Missions WHERE DroneId IN (SELECT TOP (@NumberOfRows) DroneId FROM Drones ORDER BY DroneId)",
                    connection);
                deleteMissionsCmd.Parameters.Add(new SqlParameter("@NumberOfRows", SqlDbType.Int) { Value = NumberOfRows });
                deleteMissionsCmd.ExecuteNonQuery();

                var deleteLocationsCmd = new SqlCommand(
                    "DELETE FROM Locations WHERE DroneId IN (SELECT TOP (@NumberOfRows) DroneId FROM Drones ORDER BY DroneId)",
                    connection);
                deleteLocationsCmd.Parameters.Add(new SqlParameter("@NumberOfRows", SqlDbType.Int) { Value = NumberOfRows });
                deleteLocationsCmd.ExecuteNonQuery();
                var deleteDronesCmd = new SqlCommand(
                    "DELETE FROM Drones WHERE DroneId IN (SELECT TOP (@NumberOfRows) DroneId FROM Drones ORDER BY DroneId)",
                    connection);
                deleteDronesCmd.Parameters.Add(new SqlParameter("@NumberOfRows", SqlDbType.Int) { Value = NumberOfRows });
                deleteDronesCmd.ExecuteNonQuery();
            }
        }
    }
}
