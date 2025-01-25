
using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;

namespace LiteDB_app.Models
{
    public class Location
    {
        [BsonId(true)]
        public int LocationId { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public DateTime Timestamp { get; set; }

        public int DroneId { get; set; } // Referencja do drona
    }
}
