using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace LiteDB_app.Models
{
    public class Insurance
    {
        [BsonId(true)]
        public int InsuranceId { get; set; }

        public string InsuranceProvider { get; set; }
        public string PolicyNumber { get; set; }
        public DateTime EndDate { get; set; }

        public int PilotId { get; set; } // Referencja do pilota
    }
}
