using BenchmarkDotNet.Attributes;
using Enyim.Caching;
using Memcached_app.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memcached_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class DeleteBenchmark
    {
        private IMemcachedClient _memcachedClient;
        [Params(100, 1000)]
        public int NumberOfRows;
        [GlobalSetup]
        public void Setup()
        {
            _memcachedClient = AppDbContext.MemcachedClient;
        }
        [IterationSetup]
        public void IterationSetup()
        {
            GenerateData generateData = new GenerateData();
            generateData.Count = 100;
            generateData.GenerateDataForDelete();
        }
        [Benchmark]
        public void RemoveRandomPilots()
        {
            // Pobranie wszystkich kluczy pilotów z Memcached
            var pilotKeys1 = AppDbContext.GetKeysByCategory("Pilot");
            var pilotKeys = pilotKeys1.Where(key => key.StartsWith("Pilot:")).ToList();
            var random = new Random(12345);
            var keysToRemove = pilotKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();
            // Usuwanie wybranych kluczy pilotów
            foreach (var key in keysToRemove)
            {
                _memcachedClient.Remove(key);
            }
        }
        [Benchmark]
        public void TestDelete_DronesWithCascade()
        {
            // Pobieranie wszystkich kluczy dronów z Memcached
            var droneKeys1 = AppDbContext.GetKeysByCategory("Drone");
            var droneKeys = droneKeys1.Where(key => key.StartsWith("Drone:")).ToList();
            // Losowanie określonej liczby dronów
            var random = new Random();
            var selectedDroneKeys = droneKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();
            foreach (var droneKey in selectedDroneKeys)
            {
                var droneJson = _memcachedClient.Get<string>(droneKey);
                if (droneJson != null)
                {
                    var drone = JsonConvert.DeserializeObject<Drone>(droneJson);
                    var missionIdsList = drone.MissionIds;
                    var locationIdsList = drone.LocationIds;
                    foreach (var missionId in missionIdsList)
                    {
                        var missionKey = $"Mission:{missionId}";
                        _memcachedClient.Remove(missionKey);
                    }
                    foreach (var locationId in locationIdsList)
                    {
                        var locationKey = $"Location:{locationId}";
                        _memcachedClient.Remove(locationKey);
                    }
                    _memcachedClient.Remove(droneKey);
                }
            }
        }

    }
}
