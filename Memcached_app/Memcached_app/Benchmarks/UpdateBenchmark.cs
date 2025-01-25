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
    public class UpdateBenchmark
    {
        private IMemcachedClient _memcachedClient;
        [Params(100, 1000)]
        public int NumberOfRows;
        [GlobalSetup]
        public void Setup()
        {
            _memcachedClient = AppDbContext.MemcachedClient;
        }
        [Benchmark]
        public void TestUpdate_WithRelationship()
        {
            // Pobranie kluczy pilotów z Memcached
            var pilotKeys1 = AppDbContext.GetKeysByCategory("Pilot");
            var pilotKeys = pilotKeys1.Where(key => key.StartsWith("Pilot:")).ToList();
            var random = new Random(12345);
            var selectedPilotKeys = pilotKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();

            foreach (var pilotKey in selectedPilotKeys)
            {
                var pilotJson = _memcachedClient.Get<string>(pilotKey);
                if (pilotJson == null)
                {
                    continue; 
                }
                var pilot = JsonConvert.DeserializeObject<Pilot>(pilotJson);
                if (pilot == null)
                {
                    continue; 
                }
                if (pilot.InsuranceId == null)
                {
                    continue; 
                }

                var insuranceKey = $"Insurance:{pilot.InsuranceId}";
                var insuranceJson = _memcachedClient.Get<string>(insuranceKey);
                if (insuranceJson == null)
                {
                    continue;
                }
                var insurance = JsonConvert.DeserializeObject<Insurance>(insuranceJson);
                if (insurance == null)
                {
                    continue; 
                }

                var newPolicyNumber = $"NEW-POLICY-{random.Next(0, 10000)}";
                insurance.PolicyNumber = newPolicyNumber;
                var updatedInsuranceJson = JsonConvert.SerializeObject(insurance);
                _memcachedClient.Store(Enyim.Caching.Memcached.StoreMode.Set, insuranceKey, updatedInsuranceJson);
              
            }
        }
        [Benchmark]
        public void TestUpdate_SingleTable()
        {
            var droneKeys1 = AppDbContext.GetKeysByCategory("Drone");
            var droneKeys = droneKeys1.Where(key => key.StartsWith("Drone:")).ToList();
            var random = new Random(12345);
            var selectedDroneKeys = droneKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();

            foreach (var droneKey in selectedDroneKeys)
            {
                var droneJson = _memcachedClient.Get<string>(droneKey);
                if (droneJson == null)
                {
                    continue;
                }

                // Deserializacja danych drona
                var drone = JsonConvert.DeserializeObject<Drone>(droneJson);
                if (drone == null)
                {
                    continue;
                }
                var newSpecification = "Updated Specification " + random.Next(0, 10);
                drone.Specifications = newSpecification;
                var updatedDroneJson = JsonConvert.SerializeObject(drone);
                _memcachedClient.Store(Enyim.Caching.Memcached.StoreMode.Set, droneKey, updatedDroneJson);
            }
        }


    }
}
