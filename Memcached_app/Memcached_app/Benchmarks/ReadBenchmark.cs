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
    public class ReadBenchmark
    {
        [Params(1000)]
        public int Count { get; set; }
        private IMemcachedClient _memcachedClient;
        [GlobalSetup]
        public void Setup()
        {
            _memcachedClient = AppDbContext.MemcachedClient;
        }
        [Benchmark]
        public void TestRead_Relacje1N()
        {
            // Pobieranie wszystkich kluczy z AppDbContext
            var droneKeys = AppDbContext.GetKeysByCategory("Drone");
            var drones = new List<Drone>();
            // Pobieramy dane każdego drona na podstawie kluczy
            foreach (var droneKey in droneKeys)
            {
                var droneJson = _memcachedClient.Get<string>(droneKey); 
                if (droneJson != null)
                {
                    var drone = JsonConvert.DeserializeObject<Drone>(droneJson);
                    if (drone != null)
                    {
                        drones.Add(drone);
                    }
                }
            }
            foreach (var drone in drones)
            {
                // Pobieranie szczegółów misji na podstawie MissionIds
                foreach (var missionId in drone.MissionIds)
                {
                    var missionKey = $"Mission:{missionId}";
                    var missionJson = _memcachedClient.Get<string>(missionKey); 
                    if (missionJson != null)
                    {
                        var mission = JsonConvert.DeserializeObject<Mission>(missionJson);
                    }
                }

                // Pobieranie szczegółów lokalizacji na podstawie LocationIds
                foreach (var locationId in drone.LocationIds)
                {
                    var locationKey = $"Location:{locationId}";
                    var locationJson = _memcachedClient.Get<string>(locationKey); 
                    if (locationJson != null)
                    {
                        var location = JsonConvert.DeserializeObject<Location>(locationJson);
                    }
                }
            }

        }
        [Benchmark]
        public void TestRead_Relacja1_1()
        {
            // Lista pilotów
            var pilots = new List<Pilot>();

            // Pobranie kluczy pilotów z Memcached
            var pilotKeys1 = AppDbContext.GetKeysByCategory("Pilot");
            var pilotKeys = pilotKeys1.Where(key => key.StartsWith("Pilot:")).ToList();

            foreach (var pilotKey in pilotKeys)
            {
                // Pobieramy dane pilota jako JSON
                var pilotJson = _memcachedClient.Get<string>(pilotKey);
                if (pilotJson != null)
                {
                    // Deserializacja danych pilota
                    var pilot = JsonConvert.DeserializeObject<Pilot>(pilotJson);
                    if (pilot != null)
                    {
                        pilots.Add(pilot);
                    }
                }
            }

            // Iteracja przez listę pilotów
            foreach (var pilot in pilots)
            {

                // Pobieranie szczegółów ubezpieczenia na podstawie InsuranceId
                var insuranceKey = $"Insurance:{pilot.InsuranceId}";
                var insuranceJson = _memcachedClient.Get<string>(insuranceKey);
                if (insuranceJson != null)
                {
                    // Deserializacja danych ubezpieczenia
                    var insurance = JsonConvert.DeserializeObject<Insurance>(insuranceJson);
                }
            }
        }
        [Benchmark]
        public void TestRead_BezRelacji()
        {
            var pilots = new List<Pilot>();

            // Pobranie kluczy pilotów z Memcached (np. wszystkie klucze, które zaczynają się od "Pilot:")
            var pilotKeys = AppDbContext.GetKeysByCategory("Pilot");

            foreach (var pilotKey in pilotKeys)
            {
                var pilotJson = _memcachedClient.Get<string>(pilotKey);
                if (pilotJson != null)
                {
                    var pilot = JsonConvert.DeserializeObject<Pilot>(pilotJson);
                    if (pilot != null)
                    {
                        pilots.Add(pilot);
                    }
                }
            }

        }
        [Benchmark]
        public void TestRead_RelacjaNM()
        {
            // Pobieranie wszystkich kluczy, które zaczynają się od "PilotMission:"
            var pilotMissionKeys = AppDbContext.GetKeysByCategory("PilotMission");

            foreach (var pilotMissionKey in pilotMissionKeys)
            {
                var parts = pilotMissionKey.Split(':');
                if (parts.Length == 3)
                {
                    var pilotId = Convert.ToInt32(parts[1]);
                    var missionId = Convert.ToInt32(parts[2]);
                    var pilotMission = new PilotMission
                    {
                        PilotId = pilotId,
                        MissionId = missionId
                    };

                    var pilotKeyForDetails = $"Pilot:{pilotMission.PilotId}";
                    var pilotJson = _memcachedClient.Get<string>(pilotKeyForDetails);
                    if (pilotJson != null)
                    {
                        var pilot = JsonConvert.DeserializeObject<Pilot>(pilotJson);

                    }
                    var missionKeyForDetails = $"Mission:{pilotMission.MissionId}";
                    var missionJson = _memcachedClient.Get<string>(missionKeyForDetails);
                    if (missionJson != null)
                    {
                        var mission = JsonConvert.DeserializeObject<Mission>(missionJson);
                    }
                }
            }
        }
    }
}
