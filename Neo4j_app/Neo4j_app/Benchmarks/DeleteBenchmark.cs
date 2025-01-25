using BenchmarkDotNet.Attributes;
using Bogus;
using Neo4j.Driver;
using Neo4j_app.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] 
    public class DeleteBenchmark
    {
        private static IDriver _driver;
        [Params(100)]
        public int NumberOfRows;
        [GlobalSetup]
        public void Setup()
        {
            _driver = AppDbContext._driver;
        }
       
        [Benchmark]
        public async Task DeleteDronesWithCascadeAsync()
        {
            var session = _driver.AsyncSession();

            try
            {
                
                var result = await session.RunAsync(
                    @"MATCH (d:Drone)-[:HAS_MISSION]->(m:Mission), (d)-[:HAS_LOCATION]->(l:Location)
              WITH d, m, l
              LIMIT $NumberOfRows
              DETACH DELETE d, m, l",
                    new { NumberOfRows } 
                );

                
                await result.ConsumeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas usuwania: {ex.Message}");
            }
            finally
            {
                await session.CloseAsync();
            }
        }
        [Benchmark]
        public async Task DeletePilotsWithoutInsuranceAsync()
        {
            var session = _driver.AsyncSession();

            try
            {
                var result = await session.RunAsync(
                    @"MATCH (p:Pilot)
                      WHERE NOT EXISTS((p)-[:HAS_INSURANCE]->(:Insurance))  
                      WITH p
                      LIMIT $NumberOfRows
                      DETACH DELETE p",  
                    new { NumberOfRows } 
                );

                
                await result.ConsumeAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas usuwania pilotów: {ex.Message}");
            }
            finally
            {
                await session.CloseAsync();
            }
        }
    }
}
