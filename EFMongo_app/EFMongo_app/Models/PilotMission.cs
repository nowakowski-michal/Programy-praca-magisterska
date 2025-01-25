using EFMongo_app;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EFMongo_app.Models
{
    public class PilotMission
    {
        public ObjectId PilotId { get; set; }
        public Pilot Pilot { get; set; }

        public ObjectId MissionId { get; set; }
        public Mission Mission { get; set; }
    }
}
