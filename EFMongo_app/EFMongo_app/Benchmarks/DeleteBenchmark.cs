using BenchmarkDotNet.Attributes;
using EFMongo_app.Models;


namespace EFMongo_app.Benchmarks
{
    [SimpleJob(iterationCount: 10, warmupCount: 3)] // 10 pomiarów, 3 iteracje rozgrzewające
    public class DeleteBenchmark
    {
        static AppDbContext context = new AppDbContext();

        [Params(100, 1000)]
        public int NumberOfRows;

        [GlobalSetup]
        public void Setup()
        {
            context.ChangeTracker.Clear();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            GenerateData generateData = new GenerateData();
            generateData.Count = 1000;
            generateData.GenerateForDelete();
        }

        // Usuwanie pilota bez przypisanego ubezpieczenia
        [Benchmark]
        public void TestDelete_PilotWithoutInsurance()
        {
            var pilotsToDelete = context.Pilots
                .Take(NumberOfRows)
                .ToList();  

            context.Pilots.RemoveRange(pilotsToDelete);
            context.SaveChanges();
        }

        // Usuwanie dronów wraz z powiązanymi misjami i lokalizacjami
        [Benchmark]
        public void TestDelete_DronesWithCascade()
        {
            var dronesToDelete = context.Drones
                .OrderBy(d => d.DroneId)
                .Take(NumberOfRows)
                .ToList();

            foreach (var drone in dronesToDelete)
            {
                // Usunięcie misji przypisanych do drona
                var missionsToDelete = context.Missions
                    .Where(m => m.DroneId == drone.DroneId)
                    .ToList();

                foreach (var mission in missionsToDelete)
                {
                    context.Missions.Remove(mission); 
                }

                // Usunięcie lokalizacji przypisanych do drona
                var locationsToDelete = context.Locations
                    .Where(l => l.DroneId == drone.DroneId)
                    .ToList();

                foreach (var location in locationsToDelete)
                {
                    context.Locations.Remove(location); 
                }
            }
            // Usuwanie dronów z bazy
            foreach (var drone in dronesToDelete)
            {
                context.Drones.Remove(drone); 
            }

            context.SaveChanges(); 
        }
    }
}
