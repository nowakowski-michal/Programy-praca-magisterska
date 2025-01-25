using Couchbase_app;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Couchbase_app.Models
{
    //Tabela pomocnicza przy relacji n:m
    public class PilotMission
    {
        public int PilotId { get; set; }
        // Relacja N:1 - Każda misja jest przypisana do jednego pilota
        public Pilot Pilot { get; set; }
        public int MissionId { get; set; }
        // Relacja N:1 - Każda misja jest przypisana do jednego pilota
        public Mission Mission { get; set; }
    }
}
