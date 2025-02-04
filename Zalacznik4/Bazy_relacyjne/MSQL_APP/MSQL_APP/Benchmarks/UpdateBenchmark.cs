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
    public class UpdateBenchmark
    {
        private static string connectionString = AppDbContext.connectionString;
        [Params(100, 1000)]
        public int NumberOfRows;
        [Benchmark]
        public void TestUpdate_SingleTable()
        {
            Random random = new Random();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT TOP (@NumberOfRows) DroneId FROM Drones";
                using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
                {
                    selectCommand.Parameters.AddWithValue("@NumberOfRows", NumberOfRows);

                    using (SqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        List<int> droneIds = new List<int>();
                        while (reader.Read())
                        {
                            droneIds.Add(reader.GetInt32(0));
                        }
                        reader.Close();
                        string updateQuery = "UPDATE Drones SET Specifications = @Specifications WHERE DroneId = @DroneId";
                        foreach (var droneId in droneIds)
                        {
                            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
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

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string selectQuery = @"
            SELECT p.PilotId, i.InsuranceId
            FROM Pilots p
            INNER JOIN Insurance i ON p.PilotId = i.PilotId
            WHERE i.PolicyNumber IS NOT NULL
            AND p.PilotId IN (SELECT TOP (@NumberOfRows) PilotId FROM Pilots)";

                using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
                {
                    selectCommand.Parameters.AddWithValue("@NumberOfRows", NumberOfRows);
                    using (SqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        List<int> insuranceIds = new List<int>();

                        while (reader.Read())
                        {
                            insuranceIds.Add(reader.GetInt32(1)); 
                        }
                        reader.Close();
                        string updateQuery = "UPDATE Insurance SET PolicyNumber = @PolicyNumber WHERE InsuranceId = @InsuranceId";
                        foreach (var insuranceId in insuranceIds)
                        {
                            using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
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
