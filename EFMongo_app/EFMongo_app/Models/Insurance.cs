using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMongo_app.Models
{
    public class Insurance
    {
        [BsonId]
        public ObjectId InsuranceId { get; set; }

        public string InsuranceProvider { get; set; }
        public string PolicyNumber { get; set; }
        public DateTime EndDate { get; set; }

        // Relacja 1:1 z pilota
        public ObjectId PilotId { get; set; }
        public Pilot Pilot { get; set; }
    }
}
