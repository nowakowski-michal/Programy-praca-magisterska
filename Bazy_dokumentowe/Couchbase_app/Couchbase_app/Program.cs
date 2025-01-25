using Couchbase.KeyValue;
using Couchbase.Management.Buckets;
using Couchbase;
using Couchbase_app.Models;
using Couchbase.Management.Collections;
using Couchbase_app.Benchmarks;
using BenchmarkDotNet.Running;

namespace Couchbase_app
{

   public class Program
    {
        public static async Task Main(string[] args)
        {
            var appContext = new AppDbContext("minx1", "minx111");
            await appContext.InitializeAsync();
            while (true)
            {
                //użytkownik wybiera czy generuje dane czy uruchamia benchmarki lub zamyka program
                Console.Clear();
                Console.WriteLine("1. Generuj dane\n2. Uruchom benchmarki\nQ. Zakończ");
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.D1)
                {
                    //wciśnięcie klawisza "1" pozwala na wybranie użytkownikowi ile danych chce wygenerować
                    // Pobranie liczby danych do wygenerowania
                    Console.WriteLine("\nPodaj liczbę danych do wygenerowania (1000, 10000, 100000, 1000000):");
                    int count;
                    if (int.TryParse(Console.ReadLine(), out count) && (count == 1000 || count == 10000 || count == 100000 || count == 1000000))
                    {
                        GenerateData generateData = new GenerateData();
                        generateData.Count = count;
                        await generateData.GenerateAllDataAsync();
                    }
                    else
                    {
                        //w przypadku wpisanie niepoprawniej infomracji zostanie wyświetlona stosowna informacja
                        Console.WriteLine("Nieprawidłowa liczba. Wybierz jedną z opcji: 1000, 10000, 100000, 1000000.");
                        Console.ReadKey();
                    }
                }
                else if (key == ConsoleKey.D2)
                {
                    //naciśnięcie klawisza 2 uruchamia menu do wybrania klasy typu benchmark
                     BenchmarkSwitcher.FromAssembly(typeof(ReadBenchmark).Assembly).Run();
                    Console.ReadKey();
                }

                else if (key == ConsoleKey.Q) break;
            }




        }
        /*
        private const string BucketName = "DronesBucket";
        private const string ScopeName = "DronesScope";
        private static readonly string[] Collections = { "Drones", "Insurances", "Locations", "Missions", "Pilots", "PilotMissions" };

        public static async Task Main(string[] args)
        {
            var options = new ClusterOptions
            {
                UserName = "minx1",
                Password = "minx111"
            };

            await using var cluster = await Cluster.ConnectAsync("couchbase://localhost", options);

            var bucketManager = cluster.Buckets;

            if (!await BucketExistsAsync(bucketManager, BucketName))
            {
                await bucketManager.CreateBucketAsync(new BucketSettings
                {
                    Name = BucketName,
                    BucketType = BucketType.Couchbase,
                    RamQuotaMB = 100
                });
                Console.WriteLine($"Bucket '{BucketName}' został utworzony.");
            }

            var bucket = await cluster.BucketAsync(BucketName);
            await EnsureScopeAndCollectionsAsync(bucket);

            var droneData = new
            {
                DroneId = 1,
                Model = "DJI Phantom 4",
                Manufacturer = "DJI",
                YearOfManufacture = 2019,
                Specifications = "4K Camera, GPS, 30 mins flight time"
            };

            var droneCollection = bucket.Scope(ScopeName).Collection("Drones");
            await droneCollection.UpsertAsync("drone::1", droneData);
            Console.WriteLine("Przykładowe dane zostały wstawione do kolekcji 'Drones'.");

            var getResult = await droneCollection.GetAsync("drone::1");
            Console.WriteLine($"Odczytane dane: {getResult.ContentAs<dynamic>()}");
        }

        private static async Task EnsureScopeAndCollectionsAsync(IBucket bucket)
        {
            var collectionManager = bucket.Collections;

            // Tworzenie scope'a, jeśli nie istnieje
            try
            {
                await collectionManager.CreateScopeAsync(ScopeName);
                Console.WriteLine($"Scope '{ScopeName}' został utworzony.");
            }
            catch (ScopeExistsException)
            {
                Console.WriteLine($"Scope '{ScopeName}' już istnieje.");
            }

            // Tworzenie kolekcji, jeśli nie istnieją
            foreach (var collection in Collections)
            {
                try
                {
                    await collectionManager.CreateCollectionAsync(new CollectionSpec(ScopeName, collection));
                    Console.WriteLine($"Kolekcja '{collection}' została utworzona.");
                }
                catch (CollectionExistsException)
                {
                    Console.WriteLine($"Kolekcja '{collection}' już istnieje.");
                }
            }
        }

        private static async Task<bool> BucketExistsAsync(IBucketManager bucketManager, string bucketName)
        {
            var buckets = await bucketManager.GetAllBucketsAsync();
            return buckets.ContainsKey(bucketName);
        }
        /*
        static void Main(string[] args)
        {
            var appDbContext = new AppDbContext();

            // Upewnij się, że bucket istnieje
             appDbContext.Initialize();

            // Inicjalizacja GenerateData
            //var generateData = new GenerateData(appDbContext);
           // generateData.Count = 40;
            // Generowanie danych i zapisanie do Couchbase
            //await generateData.GenerateAllDataAsync();
        }
        */
    }
}
