using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using RocksDb_app.Models;
using RocksDbApp.Models;
using RocksDbSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocksDb_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class DeleteBenchmark
    {
        private RocksDb _db;
        private string _dbPath;
        private int DbSize = 1000;
        [Params(100, 1000)]
        public int NumberOfRows;
        [GlobalSetup]
        public void Setup()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var options = new DbOptions().SetCreateIfMissing(true);
            _db = RocksDb.Open(options, _dbPath);
            GenerateData.CleanDatabase(_db);
            GenerateData.GenerateDataForDelete(_db, DbSize);
        }
        [IterationSetup]
        public void IterationSetup()
        {
            GenerateData.CleanDatabase(_db);
            GenerateData.GenerateDataForDelete(_db, DbSize);
        }
        [Benchmark]
        public void TestDelete_PilotWithoutInsurance()
        {
            var pilotKeys1 = GetKeysByCategory("Pilot");
            var pilotKeys = pilotKeys1.Where(key => key.StartsWith("Pilot:")).ToList();
            var random = new Random(12345);
            var keysToRemove = pilotKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();
            foreach (var key in keysToRemove)
            {
                var pilotJson = _db.Get(key);
                if (pilotJson != null)
                {
                    var pilot = JsonConvert.DeserializeObject<Pilot>(pilotJson);
                    if (pilot.InsuranceId == null)
                    {
                        _db.Remove(key);
                    }
                }
            }
        }
        [Benchmark]
        public void TestDelete_DronesWithCascade()
        {
            var droneKeys1 = GetKeysByCategory("Drone");
            var droneKeys = droneKeys1.Where(key => key.StartsWith("Drone:")).ToList();

            var random = new Random();
            var selectedDroneKeys = droneKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();

            foreach (var droneKey in selectedDroneKeys)
            {
                var droneJson = _db.Get(droneKey);
                if (droneJson != null)
                {
                    var drone = JsonConvert.DeserializeObject<Drone>(droneJson);

                    var missionIdsList = drone.MissionIds;
                    var locationIdsList = drone.LocationIds;

                    foreach (var missionId in missionIdsList)
                    {
                        var missionKey = $"Mission:{missionId}";
                        _db.Remove(missionKey);
                    }

                    foreach (var locationId in locationIdsList)
                    {
                        var locationKey = $"Location:{locationId}";
                        _db.Remove(locationKey);
                    }
                    _db.Remove(droneKey);
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
