using BenchmarkDotNet.Attributes;
using Couchbase.Query;
using Couchbase;

namespace Couchbase_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] 
    public class UpdateBenchmark
    {
        private ICluster _cluster;
        [Params(100, 1000)]
        public int NumberOfRows;
        [Benchmark]
        public async Task TestUpdate_SingleTable()
        {
            var options = new ClusterOptions
            {
                UserName = "minx1",
                Password = "minx111"
            };
            _cluster = await Cluster.ConnectAsync("couchbase://localhost", options);

            var random = new Random();
            var randomSpecification = $"Updated Specification {random.Next(0, 10)}";

            // Zapytanie N1QL do zaktualizowania Specyfikacji dla określonej liczby dronów
            var updateQuery = @"
                UPDATE `DronesBucket`.`DronesScope`.`Drones` AS d
                SET d.specifications = $specifications
                WHERE d.droneId IN (
                    SELECT RAW innerD.droneId
                    FROM `DronesBucket`.`DronesScope`.`Drones` AS innerD
                    LIMIT $limit
                );";

            var result = await _cluster.QueryAsync<dynamic>(updateQuery, new QueryOptions()
                .Parameter("specifications", randomSpecification)
                .Parameter("limit", NumberOfRows));

            await _cluster.DisposeAsync();
        }
        [Benchmark]
        public async Task UpdatePolicyNumberAsync()
        {
            // Konfiguracja połączenia z Couchbase
            var options = new ClusterOptions
            {
                UserName = "minx1",
                Password = "minx111"
            };

            var cluster = await Cluster.ConnectAsync("couchbase://localhost", options);
            var bucket = await cluster.BucketAsync("DronesBucket");
            var scope = bucket.Scope("DronesScope");

            // Zapytanie N1QL do zaktualizowania policyNumber w Insurances
            var updateQuery = @"
                UPDATE `DronesBucket`.`DronesScope`.`Insurances` AS i
                SET i.policyNumber = $policyNumber
                WHERE i.insuranceId IN (
                    SELECT RAW p.insurance.insuranceId
                    FROM `DronesBucket`.`DronesScope`.`Pilots` AS p
                    WHERE p.insurance.insuranceId IS NOT NULL
                    LIMIT $limit
                );";


            var parameters = new QueryOptions()
                .Parameter("policyNumber", "NEW-POLICY-1234")
                .Parameter("limit", NumberOfRows);

            var result = await cluster.QueryAsync<dynamic>(updateQuery, parameters);

            await cluster.DisposeAsync();
        }
    }
}
