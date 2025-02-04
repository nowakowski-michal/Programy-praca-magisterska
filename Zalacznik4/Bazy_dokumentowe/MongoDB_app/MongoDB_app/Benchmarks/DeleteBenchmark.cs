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
    public class DeleteBenchmark
    {
        private static IMongoDatabase database = new MongoClient(AppDbContext.clientString).GetDatabase(AppDbContext.databaseString);
        private IMongoCollection<Drone> dronesCollection => database.GetCollection<Drone>("Drones");
        private IMongoCollection<Mission> missionsCollection => database.GetCollection<Mission>("Missions");
        private IMongoCollection<Location> locationsCollection => database.GetCollection<Location>("Locations");
        private IMongoCollection<Pilot> pilotsCollection => database.GetCollection<Pilot>("Pilots");
        private IMongoCollection<PilotMission> pilotMissionsCollection => database.GetCollection<PilotMission>("PilotMission");
        private IMongoCollection<Insurance> insuranceCollection => database.GetCollection<Insurance>("Insurance");

        [Params(100, 1000)]
        public int NumberOfRows;
        [IterationSetup]
        public void IterationSetup()
        {
            GenerateData generateData = new GenerateData
            {
                Count = 1000
            };
            generateData.GenerateForDelete();
        }

        [Benchmark]
        public void TestDelete_PilotWithoutInsurance()
        {
            var filter = Builders<Pilot>.Filter.Eq(p => p.Insurance, null);
            var pilotsToDelete = pilotsCollection.Find(filter).Limit(NumberOfRows).ToList();

            foreach (var pilot in pilotsToDelete)
            {
                pilotsCollection.DeleteOne(Builders<Pilot>.Filter.Eq(p => p.PilotId, pilot.PilotId));
            }
        }

        [Benchmark]
        public void TestDelete_DronesWithCascade()
        {
            var dronesToDelete = dronesCollection.Find(Builders<Drone>.Filter.Empty).Limit(NumberOfRows).ToList();

            foreach (var drone in dronesToDelete)
            {
                missionsCollection.DeleteMany(Builders<Mission>.Filter.Eq(m => m.DroneId, drone.DroneId));
                locationsCollection.DeleteMany(Builders<Location>.Filter.Eq(l => l.DroneId, drone.DroneId));
                dronesCollection.DeleteOne(Builders<Drone>.Filter.Eq(d => d.DroneId, drone.DroneId));
            }
        }
    }
}
