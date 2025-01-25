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
    public class UpdateBenchmark
    {
        [Params(100)]
        public int NumberOfRows;
        private static GremlinClient _client;
        [GlobalSetup]
        public void Setup()
        {
            _client = AppDbContext.client;
        }
        [Benchmark]
        public async Task TestUpdate_WithRelationship()
        {
            var random = new Random(12345);

            // Zapytanie Gremlina dla pilotów i ich ubezpieczeń z limitem
            var query = $"g.V().hasLabel('pilot').as('p').both('HAS_INSURANCE').as('i').select('p', 'i').by(valueMap()).by(valueMap()).limit({NumberOfRows})";

            var result = await _client.SubmitAsync<dynamic>(query);

            foreach (var record in result)
            {
                var pilot = record["p"];
                var insurance = record["i"];

                var insuranceId = insurance["InsuranceId"][0];
                if (insuranceId == null) continue;

                var newPolicyNumber = $"NEW-POLICY-{random.Next(0, 10000)}";

                // Aktualizowanie numeru polisy w Gremlin
                var updateQuery = $"g.V().has('insurance', 'InsuranceId', {insuranceId}).property('PolicyNumber', '{newPolicyNumber}')";
                await _client.SubmitAsync<dynamic>(updateQuery);
            }
        }
        [Benchmark]
        public async Task TestUpdate_SingleTable()
        {
            var random = new Random(12345);

            // Zapytanie Gremlina dla dronów z limitem
            var query = $"g.V().hasLabel('drone').valueMap().limit({NumberOfRows})";

            var result = await _client.SubmitAsync<dynamic>(query);

            foreach (var record in result)
            {
                var droneId = record["DroneId"][0];
                var newSpecification = $"Updated Specification {random.Next(0, 10)}";

                // Aktualizowanie specyfikacji drona w Gremlin
                var updateQuery = $"g.V().has('drone', 'DroneId', {droneId}).property('Specifications', '{newSpecification}')";
                await _client.SubmitAsync<dynamic>(updateQuery);
            }
        }
    }
}
