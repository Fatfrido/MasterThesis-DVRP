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

        public SimulationResult() { }
        public SimulationResult(Solution solution, double cost) {
            Solution = solution;
            Cost = cost;
        }
    }
}
