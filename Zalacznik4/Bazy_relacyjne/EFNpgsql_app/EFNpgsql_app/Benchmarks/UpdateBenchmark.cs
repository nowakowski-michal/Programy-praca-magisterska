using BenchmarkDotNet.Attributes;
using EFNpgsql_app;
using EFNpgsql_app.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFNpgsql_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class UpdateBenchmark
    {
        static AppDbContext context = new AppDbContext();

        //testy dla różnych ilości rekordów

        [Params(100,1000)]
        public int NumberOfRows;

        [Benchmark]
        public void TestUpdate_SingleTable()
        {
            Random random = new Random();

            // Pobieranie dronów do aktualizacji
            var dronesToUpdate = context.Drones.Take(NumberOfRows).ToList();

            foreach (var drone in dronesToUpdate)
            {
                drone.Specifications = "Updated Specification " + random.Next(0, 10);
            }

            context.SaveChanges();
        }

        [Benchmark]
        public void TestUpdate_WithRelationship()
        {
            Random random = new Random();

            // Pobieranie pilotów z ubezpieczeniem do aktualizacji
            var pilotsWithInsurance = context.Pilots
                .Include(p => p.Insurance)
                .Where(p => p.Insurance != null)
                .Take(NumberOfRows)
                .ToList();

            foreach (var pilot in pilotsWithInsurance)
            {
                pilot.Insurance.PolicyNumber = "NEW-POLICY-" + random.Next(0, 10);
            }

            context.SaveChanges();
        }
    }
}
