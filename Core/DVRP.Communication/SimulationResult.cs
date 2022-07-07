using DVRP.Domain;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Communication
{
    [ProtoContract]
    public class SimulationResult
    {
        [ProtoMember(1)]
        public Solution Solution { get; set; }

        [ProtoMember(2)]
        public double Cost { get; set; }

        [ProtoMember(3)]
        public string Instance { get; set; }

        public SimulationResult() { }
        public SimulationResult(Solution solution, double cost, string instance)
        {
            Solution = solution;
            Cost = cost;
            Instance = instance;
        }
    }
}
