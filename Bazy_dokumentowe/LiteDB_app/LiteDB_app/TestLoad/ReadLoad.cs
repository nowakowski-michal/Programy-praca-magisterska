using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using LiteDB;
using LiteDB_app.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB_app.TestLoad
{
    //przy sprawdzaniu obciążenia zostanie wykonana tylko 3 iteracja testu oraz dwie rozgrzewające, 
    //aby łatwiej zmierzyć użycie zasobów przez aplikacje, dodatkowo zostana wyswietlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 3, warmupCount: 2)]
    public class Read_Load : IDisposable
    {
        private static LiteDatabase _database = new LiteDatabase(AppDbContext.connectionString);
        private ILiteCollection<Drone> _dronesCollection = _database.GetCollection<Drone>("Drones");
        private ILiteCollection<Pilot> _pilotsCollection = _database.GetCollection<Pilot>("Pilots");
        private ILiteCollection<Insurance> _insuranceCollection = _database.GetCollection<Insurance>("Insurance");
        private ILiteCollection<Mission> _missionsCollection = _database.GetCollection<Mission>("Missions");
        private ILiteCollection<Location> _locationsCollection = _database.GetCollection<Location>("Locations");
        private ILiteCollection<PilotMission> _pilotMissionsCollection = _database.GetCollection<PilotMission>("PilotMission");


        [Benchmark]
        public void TestRead_Relacja1_1()
        {
            var pilots = _pilotsCollection.FindAll().ToList();
            var insurances = _insuranceCollection.FindAll().ToList();
            foreach (var pilot in pilots)
            {
                var insurance = insurances.FirstOrDefault(i => i.PilotId == pilot.PilotId);
            }
        }
        [Benchmark]
        public void TestRead_Relacje1N()
        {
            // Pobranie wszystkich dronów z bazy
            var drones = _dronesCollection.FindAll().ToList();

            // Pobranie wszystkich misji i lokalizacji w jednym zapytaniu
            var droneIds = drones.Select(d => d.DroneId).ToList();
            var missions = _missionsCollection.Find(m => droneIds.Contains(m.DroneId)).ToList();
            var locations = _locationsCollection.Find(l => droneIds.Contains(l.DroneId)).ToList();

            // Przypisanie misji i lokalizacji do odpowiednich dronów
            foreach (var drone in drones)
            {
                // Filtruj misje i lokalizacje dla danego drona
                drone.Missions = missions.Where(m => m.DroneId == drone.DroneId).ToList();
                drone.Locations = locations.Where(l => l.DroneId == drone.DroneId).ToList();
            }
        }

        [Benchmark]
        public void TestRead_BezRelacji()
        {
            var pilots = _pilotsCollection.FindAll().ToList();
        }
        [Benchmark]
        public void TestRead_RelacjaNM()
        {
            // Pobranie wszystkich powiązań między pilotami a misjami z kolekcji PilotMission
            var pilotMissions = _pilotMissionsCollection.FindAll().ToList();
            var pilots = new List<Pilot>();

            foreach (var pilotMission in pilotMissions)
            {
                var pilot = _pilotsCollection
                    .Find(p => p.PilotId == pilotMission.PilotId)
                    .FirstOrDefault();
                var mission = _missionsCollection
                    .Find(m => m.MissionId == pilotMission.MissionId)
                    .FirstOrDefault();
            }
        }
        public void Dispose()
        {
            _database.Dispose();
        }

    }
}
