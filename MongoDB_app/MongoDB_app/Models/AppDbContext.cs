
using MongoDB.Driver;
using MongoDB_app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB_app.Models
{
    public class AppDbContext
    {
        private IMongoDatabase _database;
        private MongoClient _client;
        public static string databaseString = "DroneMongo2";
        public static string clientString = "mongodb://localhost:27017";

        public AppDbContext()
        {
            _client = new MongoClient(clientString);
            _database = _client.GetDatabase(databaseString);
        }

        public void CreateCollections()
        {
            var existingCollections = _database.ListCollectionNames().ToList();

            if (!existingCollections.Contains("Drones"))
                _database.CreateCollection("Drones");
            if (!existingCollections.Contains("Pilots"))
                _database.CreateCollection("Pilots");
            if (!existingCollections.Contains("Insurance"))
                _database.CreateCollection("Insurance");
            if (!existingCollections.Contains("Missions"))
                _database.CreateCollection("Missions");
            if (!existingCollections.Contains("Locations"))
                _database.CreateCollection("Locations");
            if (!existingCollections.Contains("PilotMission"))
                _database.CreateCollection("PilotMission");

            Console.WriteLine("Kolekcje zostały utworzone (lub już istnieją).");
        }

        // Kolekcje (tabele) w MongoDB
        public IMongoCollection<Drone> Drones => _database.GetCollection<Drone>("Drones");
        public IMongoCollection<Pilot> Pilots => _database.GetCollection<Pilot>("Pilots");
        public IMongoCollection<Insurance> Insurances => _database.GetCollection<Insurance>("Insurance");
        public IMongoCollection<Mission> Missions => _database.GetCollection<Mission>("Missions");
        public IMongoCollection<Location> Locations => _database.GetCollection<Location>("Locations");
        public IMongoCollection<PilotMission> PilotMissions => _database.GetCollection<PilotMission>("PilotMission");

        // Metoda pomocnicza do czyszczenia bazy danych 
        public void CleanDatabase()
        {
            _database.DropCollection("PilotMission");
            _database.DropCollection("Missions");
            _database.DropCollection("Insurance");
            _database.DropCollection("Pilots");
            _database.DropCollection("Locations");
            _database.DropCollection("Drones");
        }
    }
}
