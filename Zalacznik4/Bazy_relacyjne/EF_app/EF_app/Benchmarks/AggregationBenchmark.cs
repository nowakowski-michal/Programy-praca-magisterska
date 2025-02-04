using BenchmarkDotNet.Attributes;
using Ef_app;
using Ef_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ef_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 rzeczywistych pomiarów oraz 3 rozgrzewające

    public class AggregationBenchmark
    {
        [Params(10000)]
        public int Count { get; set; }
        static AppDbContext context = new AppDbContext();

        [Benchmark]
        public void TestGroupByDrones()
        {
            // Grupowanie dronów i zliczanie liczby lokalizacji przypisanych do każdego drona
            var droneLocationCounts = context.Drones
                .Select(d => new
                {
                    DroneId = d.DroneId,
                    DroneModel = d.Model,
                    LocationCount = d.Locations.Count()
                })
                .ToList();
        }
        [Benchmark]
        public void TestGroupByDate()
        {
            // Grupowanie po dacie i zliczanie liczby dornów
            var locationsByDate = context.Locations
                .GroupBy(l => l.Timestamp.Date)  
                .Select(g => new
                {
                    Date = g.Key,
                    LocationCount = g.Count() 
                })
                .ToList();
        }
    }
}
