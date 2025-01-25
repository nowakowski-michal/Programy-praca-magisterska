
using LiteDB_app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace LiteDB_app.Models
{
    public class Pilot
    {
        [BsonId(true)]
        public int PilotId { get; set; } 

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string LicenseNumber { get; set; }

        public Insurance Insurance { get; set; } // Powiązane ubezpieczenie pilota
    }

}
