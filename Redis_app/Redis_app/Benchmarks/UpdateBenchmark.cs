using BenchmarkDotNet.Attributes;
using Redis_app.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Redis_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class UpdateBenchmark
    {
        public static ConnectionMultiplexer redisConnection;
        public static IDatabase redisDatabase;
        public static IServer server;
        [Params(100, 1000)]
        public int NumberOfRows;
        [GlobalSetup]
        public void Setup()
        {
            redisConnection = AppDbContext.redisConnection;
            redisDatabase = AppDbContext.redisDatabase;
            server = AppDbContext.server;
        }
        [Benchmark]
        public void TestUpdate_SingleTable()
        {
            try
            {
                // Pobieranie wszystkich kluczy dronów
                var droneKeys = server.Keys(pattern: "Drone:*").ToList();

                // Wybieranie 5 losowych kluczy dronów
                var random = new Random(12345);
                var selectedDroneKeys = droneKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();

                foreach (var droneKey in selectedDroneKeys)
                {
                    // Generowanie nowej specyfikacji
                    var newSpecification = "Updated Specification " + random.Next(0, 10);

                    // Aktualizacja Specifications w Redis
                    redisDatabase.HashSet(droneKey, new HashEntry[]
                    {
                        new HashEntry("Specifications", newSpecification)
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while updating drone specifications: {ex.Message}");
            }
        }
        [Benchmark]
        public void TestUpdate_WithRelationship()
        {
            try
            {
                // Pobieranie wszystkich kluczy pilotów
                var pilotKeys = server.Keys(pattern: "Pilot:*").ToList();
                var random = new Random(12345);
                var selectedPilotKeys = pilotKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();

                foreach (var pilotKey in selectedPilotKeys)
                {
                    var pilotHash = redisDatabase.HashGetAll(pilotKey);

                    var insuranceIdField = pilotHash.FirstOrDefault(x => x.Name == "InsuranceId").Value;
                    if (insuranceIdField.IsNullOrEmpty)
                    {
                        continue;
                    }
                    var insuranceKey = $"Insurance:{insuranceIdField}";
                    if (!redisDatabase.KeyExists(insuranceKey))
                    {

                        continue;
                    }

                    // Generowanie nowego numeru polisy
                    var newPolicyNumber = "NEW-POLICY-" + random.Next(0, 10);

                    // Aktualizacja PolicyNumber w Redis
                    redisDatabase.HashSet(insuranceKey, new HashEntry[]
                    {
                     new HashEntry("PolicyNumber", newPolicyNumber)
                    });

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while updating insurance policy numbers: {ex.Message}");
            }
        }
    }
}
