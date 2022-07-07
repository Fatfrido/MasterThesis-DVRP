using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Domain
{
    [ProtoContract]
    public class StartSimulationMessage
    {
        [ProtoMember(1)]
        public ProblemInstance ProblemInstance { get; set; }

        [ProtoMember(2)]
        public bool AllowFastSimulation { get; set; }

        public StartSimulationMessage() { }
        public StartSimulationMessage(bool allowFastSimulation, ProblemInstance problemInstance)
        {
            ProblemInstance = problemInstance;
            AllowFastSimulation = allowFastSimulation;
        }
    }
}
