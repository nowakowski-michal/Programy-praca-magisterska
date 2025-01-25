using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using MongoDB_app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB_app.Models
{
    public class Pilot
    {
        [BsonId]
        public ObjectId PilotId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string LicenseNumber { get; set; }

        public Insurance Insurance { get; set; } // Powiązane ubezpieczenie pilota
    }
}
