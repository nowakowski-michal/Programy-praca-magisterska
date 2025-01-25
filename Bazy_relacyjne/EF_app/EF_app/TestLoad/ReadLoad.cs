using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Ef_app;
using Ef_app.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ef_app.TestLoad
{
    //przy sprawdzaniu obciążenia zostanie wykonana tylko 3 iteracja testu oraz dwie rozgrzewające, 
    //aby łatwiej zmierzyć użycie zasobów przez aplikacje, dodatkowo zostana wyswietlone informacje o alokacji pamięci
    [MemoryDiagnoser]
    [SimpleJob(RunStrategy.Monitoring, invocationCount: 1, iterationCount: 3, warmupCount: 2)]
    public class Read_Load
    {
        static AppDbContext context = new AppDbContext();
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
