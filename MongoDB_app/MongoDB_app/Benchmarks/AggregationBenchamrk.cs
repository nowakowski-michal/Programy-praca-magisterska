using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 rzeczywistych pomiarów oraz 3 rozgrzewające
    public class AggregationBenchmark
    {
        [Params(10000)]
        public int Count { get; set; }
        private static IMongoDatabase database = new MongoClient(AppDbContext.clientString).GetDatabase(AppDbContext.databaseString);
        private IMongoCollection<Drone> dronesCollection => database.GetCollection<Drone>("Drones");
        private IMongoCollection<Location> locationsCollection => database.GetCollection<Location>("Locations");
        [Benchmark]
        public void TestGroupByDrones()
        {
            var aggregationResult = locationsCollection.Aggregate()
                .Group(
                    new BsonDocument
                    {
                        { "_id", "$DroneId" },
                        { "LocationCount", new BsonDocument("$sum", 1) }
                    })
                .Lookup(
                    foreignCollectionName: "Drones",
                    localField: "_id",
                    foreignField: "DroneId",
                    @as: "DroneInfo"
                )
                .ToList();

        }

        // Benchmark dla grupowania lokalizacji po dacie
        [Benchmark]
        public void TestGroupByDate()
        {
            var aggregationResult = locationsCollection.Aggregate()
                .Group(new BsonDocument
                {
                    { "_id", new BsonDocument("$dateToString", new BsonDocument
                        {
                            { "format", "%Y-%m-%d" },
                            { "date", "$Timestamp" }
                        })
                    },
                    { "LocationCount", new BsonDocument("$sum", 1) }
                })
                .ToList();
        }

    }
}
