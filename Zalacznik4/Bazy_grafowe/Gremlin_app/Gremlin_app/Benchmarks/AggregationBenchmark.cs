using BenchmarkDotNet.Attributes;
using Gremlin.Net.Driver;
using Gremlin_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremlin_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class AggregationBenchmark
    {
        [Params(100)]
        public int Count { get; set; }
        private static GremlinClient _client;
        [GlobalSetup]
        public void Setup()
        {
            _client = AppDbContext.client;
        }
        [Benchmark]
        public async Task TestGroupByDrones()
        {
            try
            {
                var query = @"
            g.V().hasLabel('drone')
              .project('droneId', 'locationCount')
              .by('droneid')
              .by(both('has_location').count())
              .group()
              .by('droneId')
              .by('locationCount')
              .next()";

                var result = await _client.SubmitAsync<dynamic>(query);
                Console.WriteLine("Wynik zapytania:");
                foreach (var record in result)
                {
                    Console.WriteLine(record);
                }
                foreach (var record in result)
                {
                    var droneId = record["droneId"];
                    var locationCount = record["locationCount"];

                    if (droneId != null && locationCount != null)
                    {
                        Console.WriteLine($"DroneId: {droneId}, LocationCount: {locationCount}");
                    }
                    else
                    {
                        Console.WriteLine("Błąd: Nie znaleziono wartości dla droneId lub locationCount.");
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        [Benchmark]
        public async Task TestGroupByDate()
        {
            try
            {
                // Zapytanie Gremlina, które grupuje lokalizacje po dacie (yyyy-MM-dd) i liczy ich wystąpienia
                var query = @"
            g.V().hasLabel('location')
              .project('date', 'count')
              .by(values('Timestamp').map{it.toString().substring(0, 10)})
              .by(count())
              .group()
              .by('date')
              .by('count')
              .next()";
                var result = await _client.SubmitAsync<dynamic>(query);
                if (result != null)
                {
                    foreach (var record in result)
                    {
                        var date = record["date"];
                        var count = record["count"];
                        Console.WriteLine($"Date: {date}, Count: {count}");
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }

    }
}
