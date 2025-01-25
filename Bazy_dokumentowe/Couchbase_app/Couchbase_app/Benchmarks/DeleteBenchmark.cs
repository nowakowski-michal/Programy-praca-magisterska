using Couchbase.Query;
using Couchbase;
using BenchmarkDotNet.Attributes;
namespace Couchbase_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 2)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class DeleteBenchmark
    {
        private ICluster _cluster;
        //wskazanie na ilość usuwanych wierszy w każdym teście
        [Params(100, 1000)]
        public int NumberOfRows;
        [Benchmark]
        public async Task DeletePilotsWithoutInsurance()
        {
            var options = new ClusterOptions
            {
                UserName = "minx1",
                Password = "minx111"
            };

            _cluster = await Cluster.ConnectAsync("couchbase://localhost", options);
            var query = "DELETE FROM `DronesBucket`.`DronesScope`.`Pilots` WHERE insurance IS NULL LIMIT $limit";
            try
            {
                var result = await _cluster.QueryAsync<dynamic>(query, new QueryOptions().Parameter("limit", NumberOfRows));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing query: {ex.Message}");
            }

        }
        [Benchmark]
        public async Task DeleteDronesWithCascade()
        {
            var options = new ClusterOptions
            {
                UserName = "minx1",
                Password = "minx111"
            };

            _cluster = await Cluster.ConnectAsync("couchbase://localhost", options);

            try
            {
                // 1. Usuwa misje powiązane z dronami
                var deleteMissionsQuery = @"
                DELETE FROM `DronesBucket`.`DronesScope`.`Missions` AS m
                WHERE m.droneId IN (
                    SELECT RAW d.droneId
                    FROM `DronesBucket`.`DronesScope`.`Drones` AS d
                    LIMIT $limit
                );";

                var missionsResult = await _cluster.QueryAsync<dynamic>(deleteMissionsQuery, new QueryOptions().Parameter("limit", NumberOfRows));

                // 2. Usuwa lokalizacje powiązane z dronami
                var deleteLocationsQuery = @"
                DELETE FROM `DronesBucket`.`DronesScope`.`Locations` AS l
                WHERE l.droneId IN (
                    SELECT RAW d.droneId
                    FROM `DronesBucket`.`DronesScope`.`Drones` AS d
                    LIMIT $limit
                );";

                var locationsResult = await _cluster.QueryAsync<dynamic>(deleteLocationsQuery, new QueryOptions().Parameter("limit", NumberOfRows));

                // 3. Usuwa drony
                var deleteDronesQuery = @"
                DELETE FROM `DronesBucket`.`DronesScope`.`Drones` AS d
                WHERE d.droneId IN (
                    SELECT RAW innerD.droneId
                    FROM `DronesBucket`.`DronesScope`.`Drones` AS innerD
                    LIMIT $limit
                );";

                var dronesResult = await _cluster.QueryAsync<dynamic>(deleteDronesQuery, new QueryOptions().Parameter("limit", NumberOfRows));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cascade deletion: {ex.Message}");
            }
        }
    }
}

