using BenchmarkDotNet.Attributes;
using EFMongo_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMongo_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class UpdateBenchmark
    {
        static AppDbContext context = new AppDbContext();

        [Params(100,1000)]
        public int NumberOfRows;

        [Benchmark]
        public void TestUpdate_SingleTable()
        {
            Random random = new Random();

            // Pobieranie dronów do aktualizacji
            var dronesToUpdate = context.Drones
                .Take(NumberOfRows)
                .ToList();

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

            // Pobieranie pilotów do aktualizacji, łącznie z ubezpieczeniem 
            var pilotsWithInsurance = context.Pilots
                .Take(NumberOfRows)
                .ToList();

            foreach (var pilot in pilotsWithInsurance)
            {
                var pilotInsurance = context.Insurances
                    .FirstOrDefault(i => i.PilotId == pilot.PilotId);

                if (pilotInsurance != null)
                {
                    // Zaktualizowanie numeru polisy, jeśli ubezpieczenie istnieje
                    pilotInsurance.PolicyNumber = "NEW-POLICY-" + random.Next(0, 10);
                }
            }
            context.SaveChanges();
        }
    }
}
