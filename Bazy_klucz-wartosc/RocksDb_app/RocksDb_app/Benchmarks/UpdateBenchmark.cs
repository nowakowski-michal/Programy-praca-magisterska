using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using RocksDb_app.Models;
using RocksDbApp.Models;
using RocksDbSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocksDb_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class UpdateBenchmark
    {
        private RocksDb _db;
        private string _dbPath;
        private int DbSize = 1000;
        [Params(100, 1000)]
        public int NumberOfRows;

        // Przed każdą iteracją benchmarku tworzysz tymczasową bazę danych
        [GlobalSetup]
        public void Setup()
        {
            // Generowanie unikalnej ścieżki do tymczasowej bazy
            _dbPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Opcje dla bazy
            var options = new DbOptions().SetCreateIfMissing(true);

            // Otwarcie nowej, tymczasowej bazy danych
            _db = RocksDb.Open(options, _dbPath);
            GenerateData.CleanDatabase(_db);
            GenerateData.GenerateAllData(_db, DbSize);
        }
        [Benchmark]
        public void TestUpdate_WithRelationship()
        {
            // Pobranie kluczy pilotów z RocksDB
            var pilotKeys1 = GetKeysByCategory("Pilot");
            var pilotKeys = pilotKeys1.Where(key => key.StartsWith("Pilot:")).ToList();

            // Wybieranie losowej próbki kluczy pilotów (np. 300)
            var random = new Random(12345);
            var selectedPilotKeys = pilotKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();

            foreach (var pilotKey in selectedPilotKeys)
            {
                // Pobieramy dane pilota z RocksDB
                var pilotJson = _db.Get(pilotKey);
                if (pilotJson == null)
                {
                    continue; // Jeśli brak pilota, przechodzimy do następnego
                }

                // Deserializacja danych pilota
                var pilot = JsonConvert.DeserializeObject<Pilot>(pilotJson);
                if (pilot == null)
                {
                    continue; // Jeśli pilot nie jest poprawny, przechodzimy dalej
                }

                // Sprawdzamy, czy pilot ma przypisane ID ubezpieczenia
                if (pilot.InsuranceId == null)
                {
                    continue; // Jeśli brak InsuranceId, przechodzimy do kolejnego pilota
                }

                // Klucz ubezpieczenia
                var insuranceKey = $"Insurance:{pilot.InsuranceId}";

                // Sprawdzamy, czy klucz ubezpieczenia istnieje w RocksDB
                var insuranceJson = _db.Get(insuranceKey);
                if (insuranceJson == null)
                {
                    continue; // Jeśli brak ubezpieczenia, przechodzimy do następnego
                }

                // Deserializujemy dane ubezpieczenia
                var insurance = JsonConvert.DeserializeObject<Insurance>(insuranceJson);
                if (insurance == null)
                {
                    continue; // Jeśli ubezpieczenie nie jest poprawne, przechodzimy dalej
                }

                // Generujemy nowy numer polisy
                var newPolicyNumber = $"NEW-POLICY-{random.Next(0, 10000)}";

                // Aktualizujemy numer polisy w ubezpieczeniu
                insurance.PolicyNumber = newPolicyNumber;
                var updatedInsuranceJson = JsonConvert.SerializeObject(insurance);

                // Zapisujemy zaktualizowane dane ubezpieczenia do RocksDB
                _db.Put(insuranceKey, updatedInsuranceJson);
            }
        }
        [Benchmark]
        public void TestUpdate_SingleTable()
        {
            // Pobieranie wszystkich kluczy dronów z RocksDB
            var droneKeys1 = GetKeysByCategory("Drone");
            var droneKeys = droneKeys1.Where(key => key.StartsWith("Drone:")).ToList();

            // Wybieranie 5 losowych kluczy dronów
            var random = new Random(12345);
            var selectedDroneKeys = droneKeys.OrderBy(x => random.Next()).Take(NumberOfRows).ToList();

            foreach (var droneKey in selectedDroneKeys)
            {
                // Pobieramy dane drona z RocksDB
                var droneJson = _db.Get(droneKey);
                if (droneJson == null)
                {
                    continue; // Jeśli brak drona, przechodzimy do następnego
                }

                // Deserializacja danych drona
                var drone = JsonConvert.DeserializeObject<Drone>(droneJson);
                if (drone == null)
                {
                    continue; // Jeśli dron nie jest poprawny, przechodzimy dalej
                }

                // Generowanie nowej specyfikacji
                var newSpecification = "Updated Specification " + random.Next(0, 10);

                // Aktualizacja specyfikacji drona
                drone.Specifications = newSpecification;

                // Serializujemy zaktualizowane dane drona
                var updatedDroneJson = JsonConvert.SerializeObject(drone);

                // Przechowywanie zaktualizowanego drona w RocksDB
                _db.Put(droneKey, updatedDroneJson);
            }
        }

        private List<string> GetKeysByCategory(string category)
        {
            List<string> keys = new List<string>();
            var iterator = _db.NewIterator();
            iterator.SeekToFirst();

            while (iterator.Valid())
            {
                var key = iterator.Key();

                string keyString = System.Text.Encoding.UTF8.GetString(key);

                if (keyString.StartsWith(category + ":")) 
                {
                    keys.Add(keyString); 
                }
                iterator.Next();
            }
            return keys;
        }
        [GlobalCleanup]
        public void Cleanup()
        {
            _db?.Dispose();
            if (Directory.Exists(_dbPath))
            {
                Directory.Delete(_dbPath, true);
            }
        }
        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
