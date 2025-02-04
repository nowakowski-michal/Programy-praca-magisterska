using BenchmarkDotNet.Attributes;
using EFMongo_app.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMongo_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 rzeczywistych pomiarów oraz 3 rozgrzewające
    public class AggregationBenchmark
    {
        [Params(10000)]
        public int Count { get; set; }
        static AppDbContext context = new AppDbContext();

        // Grupowanie dronów i zliczanie liczby lokalizacji przypisanych do każdego drona
        [Benchmark]
        public void TestGroupByDrones()
        {
            // Pobieranie wszystkich dronów
            var drones = context.Drones
                .AsEnumerable()
                .ToList();

            // Pobieranie wszystkich lokalizacji
            var locations = context.Locations
                .AsEnumerable() 
                .ToList();

            // Grupowanie dronów według DroneId i zliczanie liczby lokalizacji przypisanych do każdego drona
            var droneLocationCounts = drones.Select(d => new
            {
                d.DroneId,
                d.Model,
                LocationCount = locations.Count(l => l.DroneId == d.DroneId)
            }).ToList();
        }

        // Grupowanie lokalizacji po dacie (ignorujemy czas)
        [Benchmark]
        public void TestGroupByDate()
        {
            var locations = context.Locations
                .AsEnumerable()  
                .ToList();

            var locationsByDate = locations
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
