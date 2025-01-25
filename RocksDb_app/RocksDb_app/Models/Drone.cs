using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocksDb_app.Models
{
    public class Drone
    {
        public int DroneId { get; set; }
        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public int YearOfManufacture { get; set; }
        public string Specifications { get; set; }

        // Relacja 1:N z Missions w Redis - lista lub zbiór misji
        public List<int> MissionIds { get; set; } 

        // Relacja 1:N z Locations w Redis - lista lub zbiór lokalizacji
        public List<int> LocationIds { get; set; } 
    }

}
