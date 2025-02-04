using LiteDB;

namespace LiteDB_app.Models
{
    public class Drone
    {
        [BsonId(true)]
        public int DroneId { get; set; }

        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public int YearOfManufacture { get; set; }
        public string Specifications { get; set; }

        public List<Mission> Missions { get; set; } // Lista misji powiązanych z dronem
        public List<Location> Locations { get; set; } // Lista lokalizacji powiązanych z dronem
    }
}
