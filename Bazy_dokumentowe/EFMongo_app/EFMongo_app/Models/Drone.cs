using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMongo_app.Models
{
    public class Drone
    {
        [BsonId] 
        public ObjectId DroneId { get; set; }

        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public int YearOfManufacture { get; set; }
        public string Specifications { get; set; }

        // Relacja 1:N - lokalizacje
        public ICollection<Location> Locations { get; set; }

        // Relacja 1:N - misje
        public ICollection<Mission> Missions { get; set; }
    }
}
