using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Msql_app.Models
{
    public class Location
    {
        public int LocationId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public DateTime Timestamp { get; set; }
        public int DroneId { get; set; }
        // Relacja N:1 Każda lokalizacja należy do jednego drona
        public Drone Drone { get; set; }
    }
}
