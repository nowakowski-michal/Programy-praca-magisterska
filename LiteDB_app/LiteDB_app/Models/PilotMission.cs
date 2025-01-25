
using LiteDB_app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace LiteDB_app.Models
{
    public class PilotMission
    {
        [BsonId(true)]
        public int PilotMissionId { get; set; } 

        public int PilotId { get; set; } // Referencja do pilota
        public int MissionId { get; set; } // Referencja do misji
    }
}
