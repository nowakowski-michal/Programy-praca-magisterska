using BenchmarkDotNet.Attributes;
using EFNpgsql_app;
using EFNpgsql_app.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace EFNpgsql_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class DeleteBenchmark
    {
        static AppDbContext context = new AppDbContext();

        [Params(100, 1000)]
        public int NumberOfRows;

        [GlobalSetup]
        public void Setup()
        {
            context.ChangeTracker.Clear();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            GenerateData generateData = new GenerateData();
            generateData.Count = 1000;
            generateData.GenerateForDelete();
        }

        // Usuwanie pilota bez przypisanego ubezpieczenia
        [Benchmark]
        public void TestDelete_PilotWithoutInsurance()
        {
            var pilotsToDelete = context.Pilots
                .Where(p => p.Insurance == null)
                .OrderBy(p => p.PilotId)
                .Take(NumberOfRows)
                .ToList();

            context.Pilots.RemoveRange(pilotsToDelete);
            context.SaveChanges();
        }

        // Usuwanie dronów wraz z kaskadowo przypisanymi misjami i lokalizacjami
        [Benchmark]
        public void TestDelete_DronesWithCascade()
        {
            var dronesToDelete = context.Drones
                .Include(d => d.Missions)
                .Include(d => d.Locations)
                .OrderBy(d => d.DroneId)
                .Take(NumberOfRows)
                .ToList();

            context.Drones.RemoveRange(dronesToDelete);
            context.SaveChanges();
        }
    }
}
