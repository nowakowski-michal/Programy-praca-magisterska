using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace MongoDB_app.Models
{
    public class Drone
    {
        [BsonId]
        public ObjectId DroneId { get; set; }

        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public int YearOfManufacture { get; set; }
        public string Specifications { get; set; }

        public List<Mission> Missions { get; set; } // Lista misji powiązanych z dronem
        public List<Location> Locations { get; set; } // Lista lokalizacji powiązanych z dronem
    }
}
