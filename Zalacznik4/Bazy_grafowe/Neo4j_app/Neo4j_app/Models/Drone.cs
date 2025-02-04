using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j_app.Models
{
    public class Drone
    {
        public int DroneId { get; set; }
        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public int YearOfManufacture { get; set; }
        public string Specifications { get; set; }

        
        public List<int> MissionIds { get; set; }  

        
        public List<int> LocationIds { get; set; }  
    }

}
