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
    public class DeleteBenchmark
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
        [IterationSetup]
        public void IterationSetup()
        {
            GenerateData generateData = new GenerateData();
            generateData.Count = 1000;
            generateData.GenerateDataForDelete();
        }
        [Benchmark]
        public void RemoveRandomPilots()
        {
            try
            {
                // Pobranie wszystkich kluczy pilotów z Redis
                var pilotKeys = server.Keys(pattern: "Pilot:*").ToList();
                var random = new Random();

                // Wybór losowej liczby kluczy
                var keysToRemove = pilotKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();
                foreach (var key in keysToRemove)
                {
                    redisDatabase.KeyDelete(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas usuwania kluczy pilotów: {ex.Message}");
            }
        }
       [Benchmark]
        public void TestDelete_DronesWithCascade()
        {
            try
            {
                // Pobieranie wszystkich kluczy dronów
                var droneKeys = server.Keys(pattern: "Drone:*").ToList();

                // Losowanie określonej liczby dronów
                var random = new Random();
                var selectedDroneKeys = droneKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();

                foreach (var droneKey in selectedDroneKeys)
                {
                    var droneHash = redisDatabase.HashGetAll(droneKey);

                    var missionIds = redisDatabase.HashGet(droneKey, "MissionIds");
                    var missionIdsList = missionIds.HasValue ? missionIds.ToString().Split(',').Select(int.Parse).ToList() : new List<int>();

                    var locationIds = redisDatabase.HashGet(droneKey, "LocationIds");
                    var locationIdsList = locationIds.HasValue ? locationIds.ToString().Split(',').Select(int.Parse).ToList() : new List<int>();

                    foreach (var missionId in missionIdsList)
                    {
                        var missionKey = $"Mission:{missionId}";
                        redisDatabase.KeyDelete(missionKey);
                    }

                    foreach (var locationId in locationIdsList)
                    {
                        var locationKey = $"Location:{locationId}";
                        redisDatabase.KeyDelete(locationKey);
                    }
                    redisDatabase.KeyDelete(droneKey);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas usuwania dronów: {ex.Message}");
            }
        }
    }
}
