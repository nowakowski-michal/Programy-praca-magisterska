using RocksDb_app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocksDb_app.Models
{
    public class Mission
    {
        public int MissionId { get; set; }
        public string MissionName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public int DroneId { get; set; } 
    }
}
