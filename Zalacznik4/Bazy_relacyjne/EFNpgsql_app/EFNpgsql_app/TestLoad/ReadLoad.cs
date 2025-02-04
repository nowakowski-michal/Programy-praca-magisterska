using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using EFNpgsql_app; 
using EFNpgsql_app.Models;  
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EFNpgsql_app.TestLoad 
{
    // Przy sprawdzaniu obciążenia zostanie wykonana tylko 3 iteracja testu oraz dwie rozgrzewające, 
    // aby łatwiej zmierzyć użycie zasobów przez aplikacje, dodatkowo zostaną wyświetlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 3, warmupCount: 2)]
    public class Read_Load
    {
        static AppDbContext context = new AppDbContext();

        // Test wydajnościowy dla relacji 1:N (Drony -> Misje i Lokalizacje)
        [Benchmark]
        public void TestRead_Relacje1N()
        {
            var drones = context.Drones
                .Select(d => new
                {
                    d.DroneId,
                    d.Model,
                    Missions = d.Missions.Select(m => new
                    {
                        m.MissionId,
                        m.MissionName
                    }).ToList(),
                    Locations = d.Locations.Select(l => new
                    {
                        l.LocationId,
                        l.Altitude
                    }).ToList()
                })
                .ToList();
        }

        // Test wydajnościowy dla relacji 1:1 (Piloci -> Ubezpieczenia)
        [Benchmark]
        public void TestRead_Relacja1_1()
        {
            var pilotsWithInsurance = context.Pilots
                .Include(p => p.Insurance)
                .Select(p => new
                {
                    p.PilotId,
                    p.FirstName,
                    p.LastName,
                    p.LicenseNumber,
                    InsuranceProvider = p.Insurance.InsuranceProvider,
                    PolicyNumber = p.Insurance.PolicyNumber,
                    EndDate = p.Insurance.EndDate
                }).ToList();
        }

        // Test wydajnościowy dla braku relacji (Piloci bez ubezpieczeń)
        [Benchmark]
        public void TestRead_BezRelacji()
        {
            var pilots = context.Pilots
                .Select(p => new
                {
                    p.PilotId,
                    p.FirstName,
                    p.LastName,
                    p.LicenseNumber
                })
                .ToList();
        }

        // Test wydajnościowy dla relacji wiele do wielu (Piloci -> Misje)
        [Benchmark]
        public void TestRead_RelacjaNM()
        {
            var pilots = context.Pilots
                .Include(p => p.PilotMissions)
                .ThenInclude(pm => pm.Mission)
                .ToList();
        }
    }
}
