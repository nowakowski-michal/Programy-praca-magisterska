using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gremlin_app.Models
{
    public class Insurance
    {
        public int InsuranceId { get; set; }
        public string InsuranceProvider { get; set; }
        public string PolicyNumber { get; set; }
        public DateTime EndDate { get; set; }

        // Relacja 1:1 z Pilot - zapisujemy tylko PilotId w Redis
        public int PilotId { get; set; }
    }
}
