
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace LiteDB_app.Models
{
    public class Mission
    {
        [BsonId(true)]
        public int MissionId { get; set; }

        public string MissionName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } // Status misji (np. "Pending", "Completed", "Failed")

        public int DroneId { get; set; } // Referencja do drona
    }
}
