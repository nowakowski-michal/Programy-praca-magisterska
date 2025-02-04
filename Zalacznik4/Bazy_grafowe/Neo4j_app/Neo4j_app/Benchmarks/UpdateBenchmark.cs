using BenchmarkDotNet.Attributes;
using Neo4j.Driver;
using Neo4j_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] 
    public class UpdateBenchmark
    {
        private static IDriver _driver;
        [Params(100,1000)]
        public int NumberOfRows;
        [GlobalSetup]
        public void Setup()
        {
            _driver = AppDbContext._driver;
        }
        [Benchmark]
        public async Task TestUpdate_WithRelationship()
        {
            var session = _driver.AsyncSession();
            var random = new Random(12345);

            try
            {
                
                var result = await session.RunAsync(
                    @"MATCH (p:Pilot)-[:HAS_INSURANCE]->(i:Insurance)
              RETURN p.PilotId AS PilotId, p.FirstName AS FirstName, 
                     i.InsuranceId AS InsuranceId, i.PolicyNumber AS PolicyNumber
              LIMIT $NumberOfRows",
                    new { NumberOfRows } 
                );

                var records = await result.ToListAsync();
                foreach (var record in records)
                {
                    var insuranceId = record["InsuranceId"].As<int?>();
                    if (insuranceId == null) continue;

                    var newPolicyNumber = $"NEW-POLICY-{random.Next(0, 10000)}";

                    
                    await session.RunAsync(
                        @"MATCH (i:Insurance {InsuranceId: $insuranceId})
                  SET i.PolicyNumber = $newPolicyNumber",
                        new { insuranceId, newPolicyNumber }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await session.CloseAsync();
            }
        }
        [Benchmark]
        public async Task TestUpdate_SingleTable()
        {
            var session = _driver.AsyncSession();
            var random = new Random(12345);

            try
            {
                
                var result = await session.RunAsync(
                    @"MATCH (d:Drone)
              RETURN d.DroneId AS DroneId, d.Specifications AS Specifications
              LIMIT $NumberOfRows",
                    new { NumberOfRows } 
                );

                var records = await result.ToListAsync();
                foreach (var record in records)
                {
                    var droneId = record["DroneId"].As<int>();
                    var newSpecification = $"Updated Specification {random.Next(0, 10)}";

                    
                    await session.RunAsync(
                        @"MATCH (d:Drone {DroneId: $droneId})
                  SET d.Specifications = $newSpecification",
                        new { droneId, newSpecification }
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                await session.CloseAsync();
            }
        }

    }
}
