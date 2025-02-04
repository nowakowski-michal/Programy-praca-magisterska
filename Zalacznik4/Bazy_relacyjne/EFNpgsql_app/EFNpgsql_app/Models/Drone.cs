using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFNpgsql_app.Models
{
    public class Drone
    {
        public int DroneId { get; set; }
        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public int YearOfManufacture { get; set; }
        public string Specifications { get; set; }
        // Relacja 1:N Jeden dron może mieć wiele lokalizacji
        public ICollection<Location> Locations { get; set; }
        // Relacja 1:N Jeden dron może mieć wiele misji
        public ICollection<Mission> Missions { get; set; }
    }
}
