
using Bogus;
using Microsoft.Data.SqlClient;
using Msql_app.Models;
using System.Data;
using System.Reflection;


namespace MSQL_APP.Models
{
    public class GenerateData
    {
        private static string connectionString = AppDbContext.connectionString;
        public int Count { get; set; }
        public void CleanDatabase()
        {
            string[] tables = new[]
            {
        "PilotMission",
        "Missions",
        "Insurance",
        "Pilots",
        "Locations",
        "Drones"
    };
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // Iteracja po tabelach i usuwanie danych
                    foreach (var table in tables)
                    {
                        using (var command = new SqlCommand($"DELETE FROM {table}", connection, transaction))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                } 
            }
        }
        public void GenerateAllData()
        {
            CleanDatabase();
            int seed = 12345;

            // Faker dla klasy Drone
            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            var drones = droneFaker.Generate(Count);

            // Faker dla klasy Insurance
            var insuranceFaker = new Faker<Insurance>()
                .RuleFor(i => i.InsuranceProvider, f => f.Company.CompanyName())
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.EndDate, f => f.Date.Future())
                .UseSeed(seed);

            var insurances = insuranceFaker.Generate(Count);

            // Faker dla klasy Pilot
            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            var pilots = pilotFaker.Generate(Count);

            // Faker dla klasy Mission
            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            var missions = missionFaker.Generate(Count);

            // Faker dla klasy Location
            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            var locations = locationFaker.Generate(Count);

            // Relacja 1:1 dla pilota i jego ubezpieczenia
            for (int i = 0; i < pilots.Count; i++)
            {
                pilots[i].Insurance = insurances[i];
                pilots[i].Insurance.PilotId = pilots[i].PilotId;
            }
            Random rand = new Random(seed);

            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                 
                    foreach (var pilot in pilots)
                    {
                        string sql = @"
                INSERT INTO Pilots (FirstName, LastName, LicenseNumber) 
                OUTPUT INSERTED.PilotId
                VALUES (@FirstName, @LastName, @LicenseNumber);
                INSERT INTO Insurance (InsuranceProvider, PolicyNumber, EndDate, PilotId) 
                VALUES (@InsuranceProvider, @PolicyNumber, @EndDate, SCOPE_IDENTITY());";

                        using (var command = new SqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = pilot.FirstName;
                            command.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = pilot.LastName;
                            command.Parameters.Add("@LicenseNumber", SqlDbType.VarChar, 20).Value = pilot.LicenseNumber;
                            command.Parameters.Add("@InsuranceProvider", SqlDbType.NVarChar, 100).Value = pilot.Insurance.InsuranceProvider;
                            command.Parameters.Add("@PolicyNumber", SqlDbType.VarChar, 20).Value = pilot.Insurance.PolicyNumber;
                            command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = pilot.Insurance.EndDate;

                            pilot.PilotId = Convert.ToInt32(command.ExecuteScalar());
                        }
                    }
                    foreach (var drone in drones)
                    {
                        using (var command = new SqlCommand(@"
                INSERT INTO Drones (Model, Manufacturer, YearOfManufacture, Specifications)
                OUTPUT INSERTED.DroneId
                VALUES (@Model, @Manufacturer, @YearOfManufacture, @Specifications);", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Model", drone.Model);
                            command.Parameters.AddWithValue("@Manufacturer", drone.Manufacturer);
                            command.Parameters.AddWithValue("@YearOfManufacture", drone.YearOfManufacture);
                            command.Parameters.AddWithValue("@Specifications", drone.Specifications);

                            drone.DroneId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        // Dodawanie lokalizacji dronów
                        var randomLocations = availableLocations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                        drone.Locations = randomLocations;
                        availableLocations.RemoveAll(loc => randomLocations.Contains(loc));

                        foreach (var location in randomLocations)
                        {
                            using (var command = new SqlCommand(@"
                    INSERT INTO Locations (Latitude, Longitude, Altitude, Timestamp, DroneId)
                    VALUES (@Latitude, @Longitude, @Altitude, @Timestamp, @DroneId);", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Latitude", location.Latitude);
                                command.Parameters.AddWithValue("@Longitude", location.Longitude);
                                command.Parameters.AddWithValue("@Altitude", location.Altitude);
                                command.Parameters.AddWithValue("@Timestamp", location.Timestamp);
                                command.Parameters.AddWithValue("@DroneId", drone.DroneId);

                                command.ExecuteNonQuery();
                            }
                        }

                        // Dodawanie misji dla drona
                        var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(3).ToList();
                        drone.Missions = randomMissions;
                        availableMissions.RemoveAll(m => randomMissions.Contains(m));

                        foreach (var mission in randomMissions)
                        {
                            using (var command = new SqlCommand(@"
                    INSERT INTO Missions (MissionName, StartTime, EndTime, Status, DroneId)
                    OUTPUT INSERTED.MissionId
                    VALUES (@MissionName, @StartTime, @EndTime, @Status, @DroneId);", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@MissionName", mission.MissionName);
                                command.Parameters.AddWithValue("@StartTime", mission.StartTime);
                                command.Parameters.AddWithValue("@EndTime", mission.EndTime);
                                command.Parameters.AddWithValue("@Status", mission.Status);
                                command.Parameters.AddWithValue("@DroneId", drone.DroneId);

                                mission.MissionId = Convert.ToInt32(command.ExecuteScalar());
                            }

                            // Dodawanie pilota do misji
                            var randomPilot = pilots[rand.Next(pilots.Count)];
                            using (var command = new SqlCommand(@"
                    INSERT INTO PilotMission (PilotId, MissionId)
                    VALUES (@PilotId, @MissionId);", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@PilotId", randomPilot.PilotId); 
                                command.Parameters.AddWithValue("@MissionId", mission.MissionId);

                                command.ExecuteNonQuery();
                            }
                        }
                    }
                    transaction.Commit();
                } 
            } 
        }
        public void GenerateForDelete()
        {
            CleanDatabase();
            int seed = 12345;

            // Faker dla klasy Drone
            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            var drones = droneFaker.Generate(Count);

            // Faker dla klasy Pilot
            var pilotFaker = new Faker<Pilot>()
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            var pilots = pilotFaker.Generate(Count);

            // Faker dla klasy Mission
            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            var missions = missionFaker.Generate(Count);

            // Faker dla klasy Location
            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            var locations = locationFaker.Generate(Count);

            Random rand = new Random(seed);

            var availableMissions = new List<Mission>(missions);
            var availableLocations = new List<Location>(locations);
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {

                    foreach (var pilot in pilots)
                    {
                        string sql = @"
                INSERT INTO Pilots (FirstName, LastName, LicenseNumber) 
                OUTPUT INSERTED.PilotId
                VALUES (@FirstName, @LastName, @LicenseNumber);";

                        using (var command = new SqlCommand(sql, connection, transaction))
                        {
                            command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = pilot.FirstName;
                            command.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = pilot.LastName;
                            command.Parameters.Add("@LicenseNumber", SqlDbType.VarChar, 20).Value = pilot.LicenseNumber;
                            pilot.PilotId = Convert.ToInt32(command.ExecuteScalar());
                        }
                    }
                    foreach (var drone in drones)
                    {
                        using (var command = new SqlCommand(@"
                INSERT INTO Drones (Model, Manufacturer, YearOfManufacture, Specifications)
                OUTPUT INSERTED.DroneId
                VALUES (@Model, @Manufacturer, @YearOfManufacture, @Specifications);", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Model", drone.Model);
                            command.Parameters.AddWithValue("@Manufacturer", drone.Manufacturer);
                            command.Parameters.AddWithValue("@YearOfManufacture", drone.YearOfManufacture);
                            command.Parameters.AddWithValue("@Specifications", drone.Specifications);

                            drone.DroneId = Convert.ToInt32(command.ExecuteScalar());
                        }
                        var randomLocations = availableLocations.OrderBy(l => rand.Next()).Take(rand.Next(0, 8)).ToList();
                        drone.Locations = randomLocations;
                        availableLocations.RemoveAll(loc => randomLocations.Contains(loc));

                        foreach (var location in randomLocations)
                        {
                            using (var command = new SqlCommand(@"
                    INSERT INTO Locations (Latitude, Longitude, Altitude, Timestamp, DroneId)
                    VALUES (@Latitude, @Longitude, @Altitude, @Timestamp, @DroneId);", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@Latitude", location.Latitude);
                                command.Parameters.AddWithValue("@Longitude", location.Longitude);
                                command.Parameters.AddWithValue("@Altitude", location.Altitude);
                                command.Parameters.AddWithValue("@Timestamp", location.Timestamp);
                                command.Parameters.AddWithValue("@DroneId", drone.DroneId);

                                command.ExecuteNonQuery();
                            }
                        }
                        var randomMissions = availableMissions.OrderBy(m => rand.Next()).Take(3).ToList();
                        drone.Missions = randomMissions;
                        availableMissions.RemoveAll(m => randomMissions.Contains(m));

                        foreach (var mission in randomMissions)
                        {
                            using (var command = new SqlCommand(@"
                    INSERT INTO Missions (MissionName, StartTime, EndTime, Status, DroneId)
                    OUTPUT INSERTED.MissionId
                    VALUES (@MissionName, @StartTime, @EndTime, @Status, @DroneId);", connection, transaction))
                            {
                                command.Parameters.AddWithValue("@MissionName", mission.MissionName);
                                command.Parameters.AddWithValue("@StartTime", mission.StartTime);
                                command.Parameters.AddWithValue("@EndTime", mission.EndTime);
                                command.Parameters.AddWithValue("@Status", mission.Status);
                                command.Parameters.AddWithValue("@DroneId", drone.DroneId);

                                mission.MissionId = Convert.ToInt32(command.ExecuteScalar());
                            }
                        }
                    }
                    transaction.Commit();
                } 
            }
        }
    }
}
