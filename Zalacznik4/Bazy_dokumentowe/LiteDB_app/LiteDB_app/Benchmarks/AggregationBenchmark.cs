using BenchmarkDotNet.Attributes;
using LiteDB;
using LiteDB_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class AggregationBenchmark : IDisposable
    {
        [Params(10000)]
        public int Count { get; set; }
        private static LiteDatabase _database = new LiteDatabase(AppDbContext.connectionString);
        private ILiteCollection<Location> _locationsCollection = _database.GetCollection<Location>("Locations");
       
        [Benchmark]
        public void TestGroupByDrones()
        {
            // Pobranie wszystkich lokalizacji
            var locations = _locationsCollection.FindAll().ToList();

            // Grupowanie po DroneId i liczenie lokalizacji
            var groupedData = locations
                .GroupBy(l => l.DroneId)
                .Select(g => new
                {
                    DroneId = g.Key,
                    LocationCount = g.Count()
                })
                .ToList();

        }

        [Benchmark]
        public void TestGroupByDate()
        {
            // Pobranie wszystkich lokalizacji
            var locations = _locationsCollection.FindAll().ToList();

            // Grupowanie lokalizacji po dacie
            var groupedData = locations
                .GroupBy(l => l.Timestamp.ToString("yyyy-MM-dd"))
                .Select(g => new
                {
                    Date = g.Key,
                    LocationCount = g.Count()
                })
                .ToList();
        }
        public void Dispose()
        {
           _database.Dispose();
        }
    }
}
