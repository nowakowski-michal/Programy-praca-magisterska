using EFMongo_app;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMongo_app.Models
{
    public class Pilot
    {
        [BsonId]
        public ObjectId PilotId { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string LicenseNumber { get; set; }

        // Relacja 1:1 - ubezpieczenie pilota
        public Insurance Insurance { get; set; }

        // Relacja wiele do wielu - misje pilota
        public ICollection<PilotMission> PilotMissions { get; set; }
    }
}
