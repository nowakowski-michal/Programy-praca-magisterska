using BenchmarkDotNet.Attributes;
using LiteDB;
using LiteDB_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class UpdateBenchmark :IDisposable
    {
        private static LiteDatabase _database = new LiteDatabase(AppDbContext.connectionString);
        // Kolekcje LiteDB
        private ILiteCollection<Drone> _dronesCollection = _database.GetCollection<Drone>("Drones");
        private ILiteCollection<Pilot> _pilotsCollection = _database.GetCollection<Pilot>("Pilots");
        private ILiteCollection<Insurance> _insuranceCollection = _database.GetCollection<Insurance>("Insurance");
        private ILiteCollection<Mission> _missionsCollection = _database.GetCollection<Mission>("Missions");
        private ILiteCollection<Location> _locationsCollection = _database.GetCollection<Location>("Locations");
        private ILiteCollection<PilotMission> _pilotMissionsCollection = _database.GetCollection<PilotMission>("PilotMission");
        [Params(100, 1000)]
        public int NumberOfRows;
        [Benchmark]
        public void TestUpdate_SingleTable()
        {
            var random = new Random();
            var droneIds = _dronesCollection.FindAll()
                                            .Select(d => d.DroneId)
                                            .Take(NumberOfRows)
                                            .ToList();

            foreach (var droneId in droneIds)
            {
                var drone = _dronesCollection.FindById(droneId);
                if (drone != null)
                {
                    drone.Specifications = "Updated Specification " + random.Next(0, 10);
                    _dronesCollection.Update(drone);
                }
            }
        }
        [Benchmark]
        public void TestUpdate_WithRelationship()
        {
            var random = new Random();
            var pilots = _pilotsCollection.FindAll().ToList();
            var insurances = _insuranceCollection.FindAll().ToList();

            foreach (var pilot in pilots)
            {
                var insurance = insurances.FirstOrDefault(i => i.PilotId == pilot.PilotId);

                if (insurance != null)
                {
                    insurance.PolicyNumber = "NEW-POLICY-" + random.Next(0, 10);
                    _insuranceCollection.Update(insurance);
                }
            }
        }



        public void Dispose()
        {
            _database.Dispose();
        }
    }
}
