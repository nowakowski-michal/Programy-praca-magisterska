using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB_app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB_app.Models
{
    public class PilotMission
    {
        [BsonId]
        public ObjectId PilotMissionId { get; set; } 

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId PilotId { get; set; } // Referencja do pilota

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId MissionId { get; set; } // Referencja do misji
        public Pilot Pilot { get; set; }  
        public Mission Mission { get; set; } 
    }
}
