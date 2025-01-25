using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMongo_app.Models
{
    public class Location
    {
        [BsonId]
        public ObjectId LocationId { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public DateTime Timestamp { get; set; }

        // Relacja 1:N - lokalizacja-dron
        public ObjectId DroneId { get; set; }
        public Drone Drone { get; set; }
    }
}
