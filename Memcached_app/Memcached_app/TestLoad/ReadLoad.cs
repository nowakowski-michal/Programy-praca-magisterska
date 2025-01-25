using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Enyim.Caching;
using Memcached_app.Models;
using Newtonsoft.Json;


namespace Memcached_app.TestLoad
{
    //przy sprawdzaniu obciążenia zostanie wykonana tylko 3 iteracja testu oraz dwie rozgrzewające, 
    //aby łatwiej zmierzyć użycie zasobów przez aplikacje, dodatkowo zostana wyswietlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 3, warmupCount: 2)]
    public class Read_Load
    {
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
            var pilots = new List<Pilot>();

            // Pobranie kluczy pilotów z Memcached
            var pilotKeys1 = AppDbContext.GetKeysByCategory("Pilot");
            var pilotKeys = pilotKeys1.Where(key => key.StartsWith("Pilot:")).ToList();

            foreach (var pilotKey in pilotKeys)
            {
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

            foreach (var pilot in pilots)
            {
                var insuranceKey = $"Insurance:{pilot.InsuranceId}";
                var insuranceJson = _memcachedClient.Get<string>(insuranceKey);
                if (insuranceJson != null)
                {
                    var insurance = JsonConvert.DeserializeObject<Insurance>(insuranceJson);
                }
            }
        }
        [Benchmark]
        public void TestRead_BezRelacji()
        {
            var pilots = new List<Pilot>();
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
