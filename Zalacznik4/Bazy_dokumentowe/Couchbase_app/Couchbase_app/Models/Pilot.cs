using Couchbase_app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Couchbase_app.Models
{
    public class Pilot
    {
        public int PilotId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string LicenseNumber { get; set; }
        // Relacja 1:1 - Jeden pilot ma jedno ubezpieczenie (opcjonalnie)
        public Insurance? Insurance { get; set; }
    }
}
