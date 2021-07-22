using DVRP.Domain;
using SimSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Simulaton
{
    public class Dispatcher : ActiveObject<PseudoRealtimeSimulation>
    {
        private Solution Solution { get; set; }
        private Store Pipe { get; set; }
        private PseudoRealtimeSimulation Env { get; set; }

        public Dispatcher(PseudoRealtimeSimulation env, Store pipe) : base(env) { 

        }
    }
}
