using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using RocksDb_app.Models;
using Newtonsoft.Json;
using RocksDbApp.Models;
using RocksDbSharp;


namespace RocksDb_app.TestLoad
{
    //przy sprawdzaniu obciążenia zostanie wykonana tylko 3 iteracja testu oraz dwie rozgrzewające, 
    //aby łatwiej zmierzyć użycie zasobów przez aplikacje, dodatkowo zostana wyswietlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 3, warmupCount: 2)]
    public class Read_load : IDisposable
    {
        private RocksDb _db;
        private string _dbPath;
        private int DbSize = 10000;
        [GlobalSetup]
        public void Setup()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var options = new DbOptions().SetCreateIfMissing(true);
            _db = RocksDb.Open(options, _dbPath);
            GenerateData.CleanDatabase(_db);
            GenerateData.GenerateAllData(_db, DbSize);
        }

        [Benchmark]
        public void TestRead_Relacje1N()
        {
            try
            {
                var droneKeys = GetKeysByCategory("Drone");
                var drones = new List<Drone>();

                foreach (var droneKey in droneKeys)
                {
                    var droneJson = _db.Get(droneKey);
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
                    foreach (var missionId in drone.MissionIds)
                    {
                        var missionKey = $"Mission:{missionId}";
                        var missionBytes = _db.Get(missionKey);
                        var mission = JsonConvert.DeserializeObject<Mission>(missionBytes);
                    }
                    foreach (var locationId in drone.LocationIds)
                    {
                        var locationKey = $"Location:{locationId}";
                        var locationBytes = _db.Get(locationKey);
                        var location = JsonConvert.DeserializeObject<Location>(locationBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd: {ex.Message}");
            }
        }
        [Benchmark]
        public void TestRead_Relacja1_1()
        {
            var pilots = new List<Pilot>();
            var pilotKeys1 = GetKeysByCategory("Pilot");
            var pilotKeys = pilotKeys1.Where(key => key.StartsWith("Pilot:")).ToList();

            foreach (var pilotKey in pilotKeys)
            {
                var pilotBytes = _db.Get(pilotKey);
                if (pilotBytes != null && pilotBytes.Length > 0)
                {
                    var pilot = JsonConvert.DeserializeObject<Pilot>(pilotBytes);
                    if (pilot != null)
                    {
                        pilots.Add(pilot);
                    }
                }
            }

            foreach (var pilot in pilots)
            {
                var insuranceKey = $"Insurance:{pilot.InsuranceId}";
                var insuranceBytes = _db.Get(insuranceKey);
                if (insuranceBytes != null && insuranceBytes.Length > 0)
                {
                    var insurance = JsonConvert.DeserializeObject<Insurance>(insuranceBytes);
                }
            }
        }
        [Benchmark]
        public void TestRead_BezRelacji()
        {
            var pilots = new List<Pilot>();
            var pilotKeys1 = GetKeysByCategory("Pilot");
            var pilotKeys = pilotKeys1.Where(key => key.StartsWith("Pilot:")).ToList();

            foreach (var pilotKey in pilotKeys)
            {
                var pilotBytes = _db.Get(pilotKey);
                if (pilotBytes != null && pilotBytes.Length > 0)
                {
                    var pilot = JsonConvert.DeserializeObject<Pilot>(pilotBytes);
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
            var pilotMissionKeys = GetKeysByCategory("PilotMission");

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
                    var pilotBytes = _db.Get(pilotKeyForDetails);
                    if (pilotBytes != null)
                    {
                        var pilot = JsonConvert.DeserializeObject<Pilot>(pilotBytes);
                    }
                    else
                    {
                        continue;
                    }

                    var missionKeyForDetails = $"Mission:{pilotMission.MissionId}";
                    var missionBytes = _db.Get(missionKeyForDetails);
                    if (missionBytes != null)
                    {
                        var mission = JsonConvert.DeserializeObject<Mission>(missionBytes);
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        private List<string> GetKeysByCategory(string category)
        {
            List<string> keys = new List<string>();
            var iterator = _db.NewIterator();
            iterator.SeekToFirst();

            while (iterator.Valid())
            {
                var key = iterator.Key();

                string keyString = System.Text.Encoding.UTF8.GetString(key);
                if (keyString.StartsWith(category + ":"))
                {
                    keys.Add(keyString);
                }

                iterator.Next();
            }
            return keys;
        }


        [GlobalCleanup]
        public void Cleanup()
        {
            _db?.Dispose();
            if (Directory.Exists(_dbPath))
            {
                Directory.Delete(_dbPath, true);
            }
        }
        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
