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
    public class DeleteBenchmark
    {
        [Params(100)]
        public int NumberOfRows;
        private static GremlinClient _client;
        [GlobalSetup]
        public void Setup()
        {
            _client = AppDbContext.client;
        }
        [IterationSetup]
        public void IterationSetup()
        {
           GenerateData generateData = new GenerateData();
            generateData.Count = 60;
            generateData.GenerateDataForDelete();
        }
        [Benchmark]
        public async Task TestDelete_PilotWithoutInsurance()
        {
            try
            {
                var queryVertices = $@"
            g.V().hasLabel('drone').limit({NumberOfRows}).drop().iterate()";

                // Wykonanie zapytania usuwającego wierzchołki
                await _client.SubmitAsync<dynamic>(queryVertices);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas usuwania: {ex.Message}");
            }
        }
        [Benchmark]
        public async Task TestDelete_DronesWithCascade()
        {
            try
            {
                var queryEdges = $@"
            g.V().hasLabel('drone').limit({NumberOfRows}).bothE('HAS_MISSION', 'HAS_LOCATION').drop().iterate()";

                await _client.SubmitAsync<dynamic>(queryEdges);

                var queryVertices = $@"
            g.V().hasLabel('drone').limit({NumberOfRows}).drop().iterate()";

                await _client.SubmitAsync<dynamic>(queryVertices);
                var queryVertices1 = $@"
            g.V().hasLabel('location').limit({NumberOfRows}).drop().iterate()";

                await _client.SubmitAsync<dynamic>(queryVertices1);
                var queryVertices2 = $@"
            g.V().hasLabel('mission').limit({NumberOfRows}).drop().iterate()";

                await _client.SubmitAsync<dynamic>(queryVertices2);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas usuwania: {ex.Message}");
            }
        }

    }
}
