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
    public class Mission
    {
        [BsonId]
        public ObjectId MissionId { get; set; }

        public string MissionName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }

        // Relacja wiele do wielu - misja-pilot
        public ICollection<PilotMission> PilotMissions { get; set; }

        // Relacja 1:N - dron
        public ObjectId DroneId { get; set; }
        public Drone Drone { get; set; }
    }
}
