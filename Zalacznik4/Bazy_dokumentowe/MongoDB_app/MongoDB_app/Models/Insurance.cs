using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB_app.Models
{
    public class Insurance
    {
        [BsonId]
        public ObjectId InsuranceId { get; set; } 

        public string InsuranceProvider { get; set; }
        public string PolicyNumber { get; set; }
        public DateTime EndDate { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId PilotId { get; set; } // Referencja do pilota
    }
}
