using BenchmarkDotNet.Running;
using Neo4j.Driver;
using Neo4j_app.Benchmarks;
using Neo4j_app.Models;

namespace Neo4j_app
{
    internal class Program
    {
        static async Task Main()
        {
            while (true)
            {   
                Console.Clear();
                Console.WriteLine("1. Generuj dane\n2. Uruchom benchmarki\nQ. Zakończ");
                var key = Console.ReadKey(true).Key;
                if (key == ConsoleKey.D1)
                {
                            
                    Console.WriteLine("\nPodaj liczbę danych do wygenerowania (100, 500, 1000, 3000):");
                    int count;
                    if (int.TryParse(Console.ReadLine(), out count) && (count == 100 || count == 500 || count == 1000 || count == 3000))
                    {
                        
                        var generateData = new GenerateData();
                        generateData.Count = count;
                        await generateData.GenerateAllData();
                    }
                    else
                    {
                        
                        Console.WriteLine("Nieprawidłowa liczba. Wybierz jedną z opcji: 1000, 10000, 100000, 1000000.");
                        Console.ReadKey();
                    }
                }
                else if (key == ConsoleKey.D2)
                {
                    
                    BenchmarkSwitcher.FromAssembly(typeof(ReadBenchmark).Assembly).Run();
                    Console.ReadKey();
                }
                else if (key == ConsoleKey.Q) break;
            }
        }
    }
}
