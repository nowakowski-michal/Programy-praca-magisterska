using Npgsql_app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Npgsql_app.Models
{
    public class Pilot
    {
        public int PilotId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string LicenseNumber { get; set; }
        // 1:1 Jeden pilot ma jedno ubezpieczenie
        public Insurance? Insurance { get; set; }
        // Relacja 1:N Jeden pilot może mieć wiele przypisanych misji
        public ICollection<PilotMission> PilotMissions { get; set; }
    }
}
