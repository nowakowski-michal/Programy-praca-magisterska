using BenchmarkDotNet.Attributes;
using EFNpgsql_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EFNpgsql_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 rzeczywistych pomiarów oraz 3 rozgrzewające
    public class AggregationBenchmark
    {
        [Params(10000)]
        public int Count;
        static AppDbContext context = new AppDbContext();

        [Benchmark]
        public void TestGroupByDrones()
        {
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
