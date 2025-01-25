using Ef_app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ef_app.Models
{
    public class Mission
    {
        public int MissionId { get; set; }
        public string MissionName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public int DroneId { get; set; }
        // Relacja N:1 Każda misja jest przypisana do jednego drona
        public Drone Drone { get; set; }
        // Relacja 1:N Jedna misja może mieć wielu pilotów
        public ICollection<PilotMission> PilotMissions { get; set; }
    }
}
