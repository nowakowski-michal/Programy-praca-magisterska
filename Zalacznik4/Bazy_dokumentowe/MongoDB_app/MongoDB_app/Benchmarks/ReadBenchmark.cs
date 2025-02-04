using BenchmarkDotNet.Attributes;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Collections;
using System.Security.Cryptography;

namespace MongoDB_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class ReadBenchmark
    {
        [Params(10000)]
        public int Count { get; set; }
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
                .Where(l => l.DroneId != ObjectId.Empty) // Filtr: tylko rekordy z przypisanym DroneId
                .ToList();
            var missionsList = missionsCollection
                .AsQueryable()
                .Where(m => m.DroneId != ObjectId.Empty) // Filtr: tylko rekordy z przypisanym DroneId
                .ToList();

            // Iteracja przez drony i łączenie danych
            foreach (var drone in dronesList)
            {
                // Pobranie lokalizacji dla danego drona
                var droneLocations = locationsList
                    .Where(loc => loc.DroneId == drone.DroneId)
                    .ToList();

                // Pobranie misji dla danego drona
                var droneMissions = missionsList
                    .Where(mission => mission.DroneId == drone.DroneId)
                    .ToList();

                // Utworzenie pełnego obiektu Drone z powiązanymi danymi
                var fullDrone = new Drone
                {
                    DroneId = drone.DroneId,
                    Model = drone.Model,
                    Manufacturer = drone.Manufacturer,
                    YearOfManufacture = drone.YearOfManufacture,
                    Specifications = drone.Specifications,
                    Locations = droneLocations, // Dodanie listy lokalizacji
                    Missions = droneMissions   // Dodanie listy misji
                };

                dronesWithDetails.Add(fullDrone); // Dodanie do wyników
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
