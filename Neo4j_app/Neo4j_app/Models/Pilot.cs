using Neo4j_app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo4j_app.Models
{
    public class Pilot
    {
        public int PilotId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string LicenseNumber { get; set; }

        
        public int? InsuranceId { get; set; }
    }
}
