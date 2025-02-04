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
    public class AggregationBenchmark
    {
        [Params(10000)]
        public int Count { get; set; }
        public static ConnectionMultiplexer redisConnection;
        public static IDatabase redisDatabase;
        public static IServer server;
        [GlobalSetup]
        public void Setup()
        {
            redisConnection = AppDbContext.redisConnection;
            redisDatabase = AppDbContext.redisDatabase;
            server = AppDbContext.server;
        }
        [Benchmark]
        public void TestGroupByDrones()
        {
            try
            {
                // Pobranie wszystkich kluczy lokalizacji
                var locationKeys = server.Keys(pattern: "Location:*").ToList();
                var locationsByDate = new Dictionary<string, int>();

                // Iteracja przez wszystkie lokalizacje
                foreach (var locationKey in locationKeys)
                {
                    // Pobieranie danych lokalizacji z hash-a
                    var locationHash = redisDatabase.HashGetAll(locationKey);

                    // Odczytanie znacznika czasu (Timestamp) i wyodrębnienie samej daty
                    var timestamp = locationHash.FirstOrDefault(x => x.Name == "Timestamp").Value;
                    var date = Convert.ToDateTime(timestamp).ToString("yyyy-MM-dd");  

                    if (locationsByDate.ContainsKey(date))
                    {
                        locationsByDate[date]++;
                    }
                    else
                    {
                        locationsByDate[date] = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas grupowania lokalizacji po dacie: {ex.Message}");
            }
        }
        [Benchmark]
        public void TestGroupByDate()
        {
            try
            {
                // Pobranie wszystkich kluczy lokalizacji
                var locationKeys = server.Keys(pattern: "Location:*").ToList();
                var droneLocationCounts = new Dictionary<int, int>();

                foreach (var locationKey in locationKeys)
                {
                    // Pobieranie danych z hasha lokalizacji
                    var locationHash = redisDatabase.HashGetAll(locationKey);
                    var droneId = Convert.ToInt32(locationHash.FirstOrDefault(x => x.Name == "DroneId").Value);

                    if (droneLocationCounts.ContainsKey(droneId))
                    {
                        droneLocationCounts[droneId]++;
                    }
                    else
                    {
                        droneLocationCounts[droneId] = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas grupowania dronów: {ex.Message}");
            }
        }
    }
}
