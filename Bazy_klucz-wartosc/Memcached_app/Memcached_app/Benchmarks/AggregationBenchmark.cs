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
    public class AggregationBenchmark
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
        public void TestGroupByDrones()
        {
            try
            {
                // Pobieranie wszystkich kluczy lokalizacji z Memcached
                var locationKeys1 = AppDbContext.GetKeysByCategory("Location");
                var locationKeys = locationKeys1.Where(key => key.StartsWith("Location:")).ToList();

                var locationsByDate = new Dictionary<string, int>();

                // Iteracja przez wszystkie lokalizacje
                foreach (var locationKey in locationKeys)
                {
                    // Pobieranie danych lokalizacji z Memcached
                    var locationJson = _memcachedClient.Get<string>(locationKey);
                    if (locationJson != null)
                    {
                        var location = JsonConvert.DeserializeObject<Location>(locationJson);
                        var timestamp = location.Timestamp;

                        // Odczytanie znacznika czasu i wyodrębnienie samej daty
                        var date = Convert.ToDateTime(timestamp).ToString("yyyy-MM-dd");

                        // Grupowanie lokalizacji po dacie
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
                // Pobranie wszystkich kluczy lokalizacji z Memcached
                var locationKeys1 = AppDbContext.GetKeysByCategory("Location");
                var locationKeys = locationKeys1.Where(key => key.StartsWith("Location:")).ToList();

                var droneLocationCounts = new Dictionary<int, int>();

                // Dla każdej lokalizacji, sprawdzamy, do którego drona jest przypisana
                foreach (var locationKey in locationKeys)
                {
                    // Pobieranie danych lokalizacji z Memcached
                    var locationJson = _memcachedClient.Get<string>(locationKey);
                    if (locationJson != null)
                    {
                        // Deserializacja danych lokalizacji
                        var location = JsonConvert.DeserializeObject<Location>(locationJson);
                        var droneId = location.DroneId; 

                        // Jeżeli dron już istnieje w słowniku, zwiększamy liczbę przypisanych lokalizacji
                        if (droneLocationCounts.ContainsKey(droneId))
                        {
                            droneLocationCounts[droneId]++;
                        }
                        else
                        {
                            // Inicjalizujemy liczbę lokalizacji dla nowego drona
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


    }
}
