using Microsoft.Data.SqlClient;
using Msql_app;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msql_app.Models
{
    public class AppDbContext
    {
        public static string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=nowaMSQ;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False";

        public void CreateTables()
        {
            string query = @"
    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Drones' AND xtype='U')
    CREATE TABLE Drones (
        DroneId INT IDENTITY PRIMARY KEY,
        Model VARCHAR(100),
        Manufacturer VARCHAR(100),
        YearOfManufacture INT,
        Specifications TEXT
    );

    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Locations' AND xtype='U')
    CREATE TABLE Locations (
        LocationId INT IDENTITY PRIMARY KEY,
        Latitude FLOAT,
        Longitude FLOAT,
        Altitude FLOAT,
        Timestamp DATETIME,
        DroneId INT FOREIGN KEY REFERENCES Drones(DroneId) ON DELETE CASCADE
    );

    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Missions' AND xtype='U')
    CREATE TABLE Missions (
        MissionId INT IDENTITY PRIMARY KEY,
        MissionName VARCHAR(100),
        StartTime DATETIME,
        EndTime DATETIME,
        Status VARCHAR(50),
        DroneId INT FOREIGN KEY REFERENCES Drones(DroneId) ON DELETE CASCADE
    );

    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Pilots' AND xtype='U')
    CREATE TABLE Pilots (
        PilotId INT IDENTITY PRIMARY KEY,
        FirstName VARCHAR(100),
        LastName VARCHAR(100),
        LicenseNumber VARCHAR(20)
    );

    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Insurance' AND xtype='U')
    CREATE TABLE Insurance (
        InsuranceId INT IDENTITY PRIMARY KEY,
        InsuranceProvider VARCHAR(100),
        PolicyNumber VARCHAR(20),
        EndDate DATETIME,
        PilotId INT UNIQUE FOREIGN KEY REFERENCES Pilots(PilotId)
    );

    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PilotMission' AND xtype='U')
    CREATE TABLE PilotMission (
        PilotId INT FOREIGN KEY REFERENCES Pilots(PilotId) ON DELETE CASCADE,
        MissionId INT FOREIGN KEY REFERENCES Missions(MissionId) ON DELETE CASCADE,
        PRIMARY KEY (PilotId, MissionId)
    );
    ";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

    }
}
