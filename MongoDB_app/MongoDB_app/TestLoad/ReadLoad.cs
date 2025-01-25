using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Npgsql_app.TestLoad
{
    //przy sprawdzaniu obciążenia zostanie wykonana tylko 3 iteracja testu oraz dwie rozgrzewające, 
    //aby łatwiej zmierzyć użycie zasobów przez aplikacje, dodatkowo zostana wyswietlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 1, warmupCount: 1)]
    public class Read_Load
    {

        private static IMongoDatabase database = new MongoClient(AppDbContext.clientString).GetDatabase(AppDbContext.databaseString);
        private IMongoCollection<Drone> dronesCollection => database.GetCollection<Drone>("Drones");
        private IMongoCollection<Mission> missionsCollection => database.GetCollection<Mission>("Missions");
        private IMongoCollection<Location> locationsCollection => database.GetCollection<Location>("Locations");
        private IMongoCollection<Pilot> pilotsCollection => database.GetCollection<Pilot>("Pilots");
        private IMongoCollection<PilotMission> pilotMissionsCollection => database.GetCollection<PilotMission>("PilotMission");
        private IMongoCollection<Insurance> insuranceCollection => database.GetCollection<Insurance>("Insurance");

        [Benchmark]
        public void TestRead_Relacje1N()
        {
            List<Drone> dronesWithDetails = new List<Drone>();

            // Pobranie danych z kolekcji
            var dronesList = dronesCollection
                .AsQueryable()
                .ToList();
            var locationsList = locationsCollection
                .AsQueryable()
                .Where(l => l.DroneId != ObjectId.Empty) 
                .ToList();
            var missionsList = missionsCollection
                .AsQueryable()
                .Where(m => m.DroneId != ObjectId.Empty) 
                .ToList();

            foreach (var drone in dronesList)
            {
                var droneLocations = locationsList
                    .Where(loc => loc.DroneId == drone.DroneId)
                    .ToList();

                var droneMissions = missionsList
                    .Where(mission => mission.DroneId == drone.DroneId)
                    .ToList();

                var fullDrone = new Drone
                {
                    DroneId = drone.DroneId,
                    Model = drone.Model,
                    Manufacturer = drone.Manufacturer,
                    YearOfManufacture = drone.YearOfManufacture,
                    Specifications = drone.Specifications,
                    Locations = droneLocations, 
                    Missions = droneMissions   
                };

                dronesWithDetails.Add(fullDrone); 
            }
        }
        [Benchmark]
        public void TestRead_Relacja1_1()
        {
            List<Pilot> pilots = new List<Pilot>();

            var pilotList = pilotsCollection
                .AsQueryable()
                .ToList();
            var insuranceList = insuranceCollection
                .AsQueryable()
                .ToList();
            foreach (var pilot in pilotList)
            {
                var insurance = insuranceList.FirstOrDefault(i => i.PilotId == pilot.PilotId);

                if (insurance != null)
                {
                    var fullPilot = new Pilot
                    {
                        PilotId = pilot.PilotId,
                        FirstName = pilot.FirstName,
                        LastName = pilot.LastName,
                        LicenseNumber = pilot.LicenseNumber,
                        Insurance = new Insurance
                        {
                            InsuranceId = insurance.InsuranceId,
                            InsuranceProvider = insurance.InsuranceProvider,
                            PolicyNumber = insurance.PolicyNumber,
                            EndDate = insurance.EndDate
                        }
                    };

                    pilots.Add(fullPilot);
                }
            }
        }

        [Benchmark]
        public void TestRead_BezRelacji()
        {
            var pilots = pilotsCollection.Find(Builders<Pilot>.Filter.Empty).ToList();

        }
        [Benchmark]
        public void TestRead_RelacjaNM()
        {
            var pilotMissions = pilotMissionsCollection.Find(Builders<PilotMission>.Filter.Empty).ToList();
            foreach (var pilotMission in pilotMissions)
            {
                var pilot = pilotMission.Pilot;
                var mission = pilotMission.Mission;
            }
        }
    }
}
