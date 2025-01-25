using Npgsql;
using System;

namespace Npgsql_app.Models
{
    public class AppDbContext
    {
        public static string connectionString = "Host=localhost;Port=5432;Username=postgres;Password=admin1;Database=Drony2";

        public void CreateTables()
        {
                string query = @"
        CREATE TABLE IF NOT EXISTS Drones (
            DroneId SERIAL PRIMARY KEY,
            Model VARCHAR(100),
            Manufacturer VARCHAR(100),
            YearOfManufacture INT,
            Specifications TEXT
        );

    CREATE TABLE IF NOT EXISTS Locations (
        LocationId SERIAL PRIMARY KEY,
        Latitude DOUBLE PRECISION,
        Longitude DOUBLE PRECISION,
        Altitude DOUBLE PRECISION,
        Timestamp TIMESTAMPTZ,
        DroneId INT REFERENCES Drones(DroneId) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS Missions (
        MissionId SERIAL PRIMARY KEY,
        MissionName VARCHAR(100),
        StartTime TIMESTAMPTZ,
        EndTime TIMESTAMPTZ,
        Status VARCHAR(50),
        DroneId INT REFERENCES Drones(DroneId) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS Pilots (
        PilotId SERIAL PRIMARY KEY,
        FirstName VARCHAR(100),
        LastName VARCHAR(100),
        LicenseNumber VARCHAR(20)
    );

    CREATE TABLE IF NOT EXISTS Insurance (
        InsuranceId SERIAL PRIMARY KEY,
        InsuranceProvider VARCHAR(100),
        PolicyNumber VARCHAR(20),
        EndDate TIMESTAMPTZ,
        PilotId INT UNIQUE REFERENCES Pilots(PilotId)
    );

    CREATE TABLE IF NOT EXISTS PilotMission (
        PilotId INT REFERENCES Pilots(PilotId) ON DELETE CASCADE,
        MissionId INT REFERENCES Missions(MissionId) ON DELETE CASCADE,
        PRIMARY KEY (PilotId, MissionId)
    );
    ";

            // Use Npgsql to connect to PostgreSQL and execute the query
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
