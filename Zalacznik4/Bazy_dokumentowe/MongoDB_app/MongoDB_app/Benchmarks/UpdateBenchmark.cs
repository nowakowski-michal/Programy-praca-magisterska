using BenchmarkDotNet.Attributes;
using MongoDB.Driver;
using MongoDB_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class UpdateBenchmark
    {
        private static IMongoDatabase database = new MongoClient(AppDbContext.clientString).GetDatabase(AppDbContext.databaseString);
        private IMongoCollection<Drone> dronesCollection => database.GetCollection<Drone>("Drones");
        private IMongoCollection<Pilot> pilotsCollection => database.GetCollection<Pilot>("Pilots");
        private IMongoCollection<Insurance> insuranceCollection => database.GetCollection<Insurance>("Insurance");
        [Params(100, 1000)]
        public int NumberOfRows;

        [Benchmark]
        public void TestUpdate_SingleTable()
        {
            var random = new Random();
            var filter = Builders<Drone>.Filter.Empty;

            var droneIds = dronesCollection.Find(filter)
                                           .Limit(NumberOfRows)
                                           .Project(d => d.DroneId)
                                           .ToList();

            foreach (var droneId in droneIds)
            {
                var update = Builders<Drone>.Update.Set(d => d.Specifications, "Updated Specification " + random.Next(0, 10));
                dronesCollection.UpdateOne(d => d.DroneId == droneId, update);
            }
        }

        [Benchmark]
        public void TestUpdate_WithRelationship()
        {
            var random = new Random();

            var pilotFilter = Builders<Pilot>.Filter.Exists(p => p.Insurance.PolicyNumber);
            var pilotIds = pilotsCollection.Find(pilotFilter)
                                           .Limit(NumberOfRows)
                                           .Project(p => new { p.PilotId, p.Insurance.InsuranceId })
                                           .ToList();

            foreach (var pilot in pilotIds)
            {
                var update = Builders<Insurance>.Update.Set(i => i.PolicyNumber, "NEW-POLICY-" + random.Next(0, 10));
                insuranceCollection.UpdateOne(i => i.InsuranceId == pilot.InsuranceId, update);
            }
        }
    }
}
