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
    public class Mission
    {
        [BsonId]
        public ObjectId MissionId { get; set; } 

        public string MissionName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } 

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId DroneId { get; set; } // Referencja do drona
    }
}
