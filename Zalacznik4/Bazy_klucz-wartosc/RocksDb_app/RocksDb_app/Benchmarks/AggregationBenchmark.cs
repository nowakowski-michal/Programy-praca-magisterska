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
    public class AggregationBenchmark
    {
        [Params(1000)]
        public int Count { get; set; }
        private RocksDb _db;
        private string _dbPath;
        private int DbSize = 1000;

        [GlobalSetup]
        public void Setup()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Opcje dla bazy
            var options = new DbOptions().SetCreateIfMissing(true);

            // Otwarcie nowej, tymczasowej bazy danych
            _db = RocksDb.Open(options, _dbPath);
            GenerateData.CleanDatabase(_db);
            GenerateData.GenerateAllData(_db, DbSize);
        }
        [Benchmark]
        public void TestGroupByDrones()
        {
            try
            {
                var locationKeys1 = GetKeysByCategory("Location");
                var locationKeys = locationKeys1.Where(key => key.StartsWith("Location:")).ToList();

                var locationsByDate = new Dictionary<string, int>();
                foreach (var locationKey in locationKeys)
                {
                    var locationJson = _db.Get(locationKey);
                    if (locationJson != null)
                    {
                        var location = JsonConvert.DeserializeObject<Location>(locationJson);
                        var timestamp = location.Timestamp;
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
                var locationKeys1 = GetKeysByCategory("Location");
                var locationKeys = locationKeys1.Where(key => key.StartsWith("Location:")).ToList();
                var droneLocationCounts = new Dictionary<int, int>();
                foreach (var locationKey in locationKeys)
                {
                    var locationJson = _db.Get(locationKey);
                    if (locationJson != null)
                    {
                        var location = JsonConvert.DeserializeObject<Location>(locationJson);
                        var droneId = location.DroneId;
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas grupowania dronów: {ex.Message}");
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
