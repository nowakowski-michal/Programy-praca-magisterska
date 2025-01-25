using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Data;
using Msql_app.Models;
using MSQL_APP.Benchmarks;
using MSQL_APP.Models;
using System.Diagnostics.CodeAnalysis;
namespace Msql_app
{
    public class Program
    {
        static void Main(string[] args)
        {
            AppDbContext appDbContext = new AppDbContext();
            appDbContext.CreateTables();
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
                        //następuje generowanie danych zgodnie z tym co podał użytkownik
                        GenerateData generowanie = new GenerateData();
                        generowanie.Count =  count;
                        generowanie.GenerateAllData();
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
                    BenchmarkSwitcher.FromAssembly(typeof(CreateBenchmark).Assembly).Run();
                    Console.ReadKey();
                }
                else if (key == ConsoleKey.Q) break;
            }
        }
    }

}
