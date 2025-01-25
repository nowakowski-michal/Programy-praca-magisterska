using BenchmarkDotNet.Running;
using MongoDB.Driver;
using MongoDB_app.Benchmarks;
using MongoDB_app.Models;

namespace MongoDB_app
{
    public class Program
    {
        static void Main(string[] args)
        {
            var appDbContext = new AppDbContext();
            appDbContext.CreateCollections();
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
                        var generateData = new GenerateData();
                        generateData.Count = count;
                        generateData.GenerateAllData();
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
    }
}

