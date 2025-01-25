using Bogus;
using Couchbase;

namespace Couchbase_app.Models
{
    public class GenerateData
    {
        private readonly Cluster _cluster = AppDbContext._cluster;
        private readonly Random _rand = new Random(12345);
        private readonly IBucket _bucket = AppDbContext._bucket;
        private readonly string _scopeName = AppDbContext.ScopeName;

        //podanie liczby rekordó w każdej z tabel jaka ma zostać wygenerowana
        public int Count { get; set; }

        public async Task CleanDatabaseAsync()
        {
            var collectionManager = _bucket.Collections;
            var scopeName = AppDbContext.ScopeName;
            var scopes = await collectionManager.GetAllScopesAsync();
            foreach (var scope in scopes)
            {
                if (scope.Name == scopeName)
                {
                    foreach (var collection in scope.Collections)
                    {
                        var query = $"DELETE FROM `{_bucket.Name}`.`{scope.Name}`.`{collection.Name}` WHERE TRUE";
                        var result = await _cluster.QueryAsync<dynamic>(query);
                    }
                }
            }
        }
        // Generowanie danych i wstawianie ich do odpowiednich kolekcji
        public async Task GenerateAllDataAsync()
        {
            await CleanDatabaseAsync();
            int seed = 12345;
            // Faker dla klasy Drone
            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.DroneId, f => f.IndexGlobal)
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            var drones = droneFaker.Generate(Count);

            // Faker dla klasy Insurance
            var insuranceFaker = new Faker<Insurance>()
                .RuleFor(i => i.InsuranceId, f => f.IndexGlobal)
                .RuleFor(i => i.InsuranceProvider, f => f.Company.CompanyName())
                .RuleFor(i => i.PolicyNumber, f => f.Random.AlphaNumeric(10).ToUpper())
                .RuleFor(i => i.EndDate, f => f.Date.Future())
                .UseSeed(seed);

            var insurances = insuranceFaker.Generate(Count);

            // Faker dla klasy Pilot
            var pilotFaker = new Faker<Pilot>()
               .RuleFor(p => p.PilotId, f => f.IndexGlobal)
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            var pilots = pilotFaker.Generate(Count);

            // Faker dla klasy Mission
            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionId, f => f.IndexGlobal)
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            var missions = missionFaker.Generate(Count);

            // Faker dla klasy Location
            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.LocationId, f => f.IndexGlobal)
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            var locations = locationFaker.Generate(Count);

            // Relacja 1:1 dla pilota i jego ubezpieczenia
            for (int i = 0; i < pilots.Count; i++)
            {
                var pilot = pilots[i];
                var insurance = insurances[i];
                insurance.PilotId = pilot.PilotId;
                pilot.Insurance = insurance;
            }

            // Dodawanie danych do kolekcji w Couchbase
            try
            {
                foreach (var pilot in pilots)
                {
                    // Wstawianie pilota do kolekcji
                    var pilotCollection = _bucket.Scope(_scopeName).Collection("Pilots");
                    await pilotCollection.UpsertAsync($"pilot::{pilot.PilotId}", pilot);

                    // Wstawianie ubezpieczenia do kolekcji
                    var insuranceCollection = _bucket.Scope(_scopeName).Collection("Insurances");
                    await insuranceCollection.UpsertAsync($"insurance::{pilot.PilotId}", pilot.Insurance);
                }
                foreach (var drone in drones)
                {
                    // Wstawianie drona do kolekcji
                    var droneCollection = _bucket.Scope(_scopeName).Collection("Drones");
                    await droneCollection.UpsertAsync($"drone::{drone.DroneId}", drone);

                    // Dodawanie lokalizacji dla dronów
                    var randomLocations = locations.OrderBy(l => _rand.Next()).Take(_rand.Next(0, 8)).ToList();
                    drone.Locations = randomLocations;

                    foreach (var location in randomLocations)
                    {
                        location.DroneId = drone.DroneId;
                        var locationCollection = _bucket.Scope(_scopeName).Collection("Locations");
                        await locationCollection.UpsertAsync($"location::{location.LocationId}", location);
                    }

                    // Dodawanie misji dla dronów
                    var randomMissions = missions.OrderBy(m => _rand.Next()).Take(3).ToList();
                    drone.Missions = randomMissions;

                    foreach (var mission in randomMissions)
                    {
                        mission.DroneId = drone.DroneId;
                        var missionCollection = _bucket.Scope(_scopeName).Collection("Missions");
                        await missionCollection.UpsertAsync($"mission::{mission.MissionId}", mission);

                        // Dodawanie pilotów do misji
                        var randomPilot = pilots[_rand.Next(pilots.Count)];
                        var pilotMission = new PilotMission
                        {
                            PilotId = randomPilot.PilotId,
                            MissionId = mission.MissionId,
                            Pilot = randomPilot, 
                            Mission = mission 
                        };

                        var pilotMissionCollection = _bucket.Scope(_scopeName).Collection("PilotMissions");
                        await pilotMissionCollection.UpsertAsync($"pilotMission::{pilotMission.PilotId}::{pilotMission.MissionId}", pilotMission);
                    }

                    // Ponowne wstawienie zaktualizowanego drona z lokalizacjami i misjami (1:N)
                    await droneCollection.UpsertAsync($"drone::{drone.DroneId}", drone);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Wystąpił błąd podczas generowania danych: " + ex.Message);
                throw;
            }
        }
        //Funkcja wypełaniająca bazę danymi ktore zostaną usnięte w benchmarku DeleteTest
        //W tej funkcji nie istnieje relacja 1-1 oraz n:m onieważ nie są testowane przy usuwaniu
        public async Task GenerateForDeleteAsync()
        {
            await CleanDatabaseAsync();
            int seed = 12345;

            // Faker dla klasy Drone
            var droneFaker = new Faker<Drone>()
                .RuleFor(d => d.DroneId, f => f.IndexGlobal)
                .RuleFor(d => d.Model, f => f.Vehicle.Model())
                .RuleFor(d => d.Manufacturer, f => f.Vehicle.Manufacturer())
                .RuleFor(d => d.YearOfManufacture, f => f.Date.Past(10).Year)
                .RuleFor(d => d.Specifications, f => f.Lorem.Sentence())
                .UseSeed(seed);

            var drones = droneFaker.Generate(Count);

            // Faker dla klasy Pilot
            var pilotFaker = new Faker<Pilot>()
               .RuleFor(p => p.PilotId, f => f.IndexGlobal)
                .RuleFor(p => p.FirstName, f => f.Name.FirstName())
                .RuleFor(p => p.LastName, f => f.Name.LastName())
                .RuleFor(p => p.LicenseNumber, f => f.Random.AlphaNumeric(8).ToUpper())
                .UseSeed(seed);

            var pilots = pilotFaker.Generate(Count);

            // Faker dla klasy Mission
            var missionFaker = new Faker<Mission>()
                .RuleFor(m => m.MissionId, f => f.IndexGlobal)
                .RuleFor(m => m.MissionName, f => f.Lorem.Word())
                .RuleFor(m => m.StartTime, f => f.Date.Past())
                .RuleFor(m => m.EndTime, (f, m) => f.Date.Soon(2, m.StartTime))
                .RuleFor(m => m.Status, f => f.PickRandom("Pending", "Completed", "Failed"))
                .UseSeed(seed);

            var missions = missionFaker.Generate(Count);

            // Faker dla klasy Location
            var locationFaker = new Faker<Location>()
                .RuleFor(l => l.LocationId, f => f.IndexGlobal)
                .RuleFor(l => l.Latitude, f => f.Address.Latitude())
                .RuleFor(l => l.Longitude, f => f.Address.Longitude())
                .RuleFor(l => l.Altitude, f => f.Random.Double(100, 500))
                .RuleFor(l => l.Timestamp, f => f.Date.Recent())
                .UseSeed(seed);

            var locations = locationFaker.Generate(Count);

            for (int i = 0; i < pilots.Count; i++)
            {
                var pilot = pilots[i];

            }

            // Dodawanie danych do kolekcji w Couchbase
            try
            {
                foreach (var pilot in pilots)
                {
                    // Wstawianie pilota do kolekcji
                    var pilotCollection = _bucket.Scope(_scopeName).Collection("Pilots");
                    await pilotCollection.UpsertAsync($"pilot::{pilot.PilotId}", pilot);

                }
                foreach (var drone in drones)
                {
                    // Wstawianie drona do kolekcji
                    var droneCollection = _bucket.Scope(_scopeName).Collection("Drones");
                    await droneCollection.UpsertAsync($"drone::{drone.DroneId}", drone);

                    // Dodawanie lokalizacji dla dronów
                    var randomLocations = locations.OrderBy(l => _rand.Next()).Take(_rand.Next(0, 8)).ToList();
                    drone.Locations = randomLocations;

                    foreach (var location in randomLocations)
                    {
                        location.DroneId = drone.DroneId;
                        var locationCollection = _bucket.Scope(_scopeName).Collection("Locations");
                        await locationCollection.UpsertAsync($"location::{location.LocationId}", location);
                    }

                    // Dodawanie misji dla dronów
                    var randomMissions = missions.OrderBy(m => _rand.Next()).Take(3).ToList();
                    drone.Missions = randomMissions;

                    foreach (var mission in randomMissions)
                    {
                        mission.DroneId = drone.DroneId;
                        var missionCollection = _bucket.Scope(_scopeName).Collection("Missions");
                        await missionCollection.UpsertAsync($"mission::{mission.MissionId}", mission);
                    }
                    await droneCollection.UpsertAsync($"drone::{drone.DroneId}", drone);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Wystąpił błąd podczas generowania danych: " + ex.Message);
                throw;
            }
        }
    }
}