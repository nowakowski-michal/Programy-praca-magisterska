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
    public class AggregationBenchmark
    {
        [Params(500)]
        public int Count { get; set; }
        private static IDriver _driver;
        [GlobalSetup]
        public void Setup()
        {
            _driver = AppDbContext._driver;
        }
        [Benchmark]
        public async Task GroupLocationsByDate()
        {
            var session = _driver.AsyncSession();

            try
            {
                var query = @"
                MATCH (l:Location)
                WITH l,
                  toInteger(substring(l.Timestamp, 6, 4)) AS year,
                  toInteger(substring(l.Timestamp, 3, 2)) AS month,
                  toInteger(substring(l.Timestamp, 0, 2)) AS day,
                  toInteger(substring(l.Timestamp, 11, 2)) AS hour,
                  toInteger(substring(l.Timestamp, 14, 2)) AS minute,
                  toInteger(substring(l.Timestamp, 17, 2)) AS second
                WITH l, datetime({year: year, month: month, day: day, hour: hour, minute: minute, second: second}) AS parsedTimestamp
                WITH date(parsedTimestamp) AS locationDate, COUNT(l) AS locationCount
                RETURN locationDate, locationCount
                ORDER BY locationDate;";

                var result = await session.RunAsync(query);

                var records = await result.ToListAsync();

                foreach (var record in records)
                {
                    var locationDate = record["locationDate"].As<DateTime>();
                    var locationCount = record["locationCount"].As<int>();
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
        public async Task TestGroupByDrones()
        {
            var session = _driver.AsyncSession();

            try
            {
                var query = @"
                MATCH (d:Drone)-[:HAS_LOCATION]->(l:Location)
                WITH d.DroneId AS droneId, COUNT(l) AS locationCount
                RETURN droneId, locationCount
                ORDER BY locationCount DESC;";

                var result = await session.RunAsync(query);

                var records = await result.ToListAsync();

                foreach (var record in records)
                {
                    var droneId = record["droneId"].As<int>();
                    var locationCount = record["locationCount"].As<int>();

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
