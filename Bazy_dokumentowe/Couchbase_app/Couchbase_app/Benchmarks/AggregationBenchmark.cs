using BenchmarkDotNet.Attributes;
using Couchbase;


namespace Couchbase_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class AggregationBenchmark
    {
        [Params(1000)]
        public int Count { get; set; }
        [Benchmark]
        public async Task GroupByDrones()
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

            // Kolekcje
            var locationsCollection = scope.Collection("Locations");
            var dronesCollection = scope.Collection("Drones");

            // Zapytanie N1QL do grupowania danych z tabeli Locations
            var query = @"
                SELECT l.droneId, 
                       COUNT(*) AS LocationCount
                FROM `DronesBucket`.`DronesScope`.`Locations` l
                JOIN `DronesBucket`.`DronesScope`.`Drones` d 
                ON l.droneId = d.droneId
                GROUP BY l.droneId;";

            var result = await cluster.QueryAsync<dynamic>(query);

            await cluster.DisposeAsync();
        }

        [Benchmark]
        public async Task TestGroupByDate()
        {
            var options = new ClusterOptions
            {
                UserName = "minx1",
                Password = "minx111"
            };

            var cluster = await Cluster.ConnectAsync("couchbase://localhost", options);
            var bucket = await cluster.BucketAsync("DronesBucket");
            var scope = bucket.Scope("DronesScope");
            var locationsCollection = scope.Collection("Locations");

            // Zapytanie N1QL do grupowania lokalizacji po dacie
            var query = @"
                SELECT DATE_FORMAT_STR(l.timestamp, '%Y-%m-%d') AS Date, COUNT(*) AS LocationCount
                FROM `DronesBucket`.`DronesScope`.`Locations` l
                GROUP BY DATE_FORMAT_STR(l.timestamp, '%Y-%m-%d');";

            var result = await cluster.QueryAsync<dynamic>(query);
            await cluster.DisposeAsync();
        }
    }
}
