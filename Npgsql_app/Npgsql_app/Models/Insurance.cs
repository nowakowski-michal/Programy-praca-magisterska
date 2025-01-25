using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Npgsql_app.Models
{
    public class Insurance
    {
        public int InsuranceId { get; set; }
        public string InsuranceProvider { get; set; }
        public string PolicyNumber { get; set; }
        public DateTime EndDate { get; set; }
        public int PilotId { get; set; }
        // Relacja N:1 (Każde ubezpieczenie jest przypisane do jednego pilota)
        public Pilot Pilot { get; set; }
    }
}
