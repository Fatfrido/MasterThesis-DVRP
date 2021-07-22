using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Domain
{
    public class WorldState
    {
        // vehicle (value) that served the customer (key)
        private Dictionary<int, int> history;
        public Solution Solution { get; set; }

        public WorldState(int vehicles) {
            history = new Dictionary<int, int>();
        }

        public void SetDone(int vehicle, int customer) {
            history.Add(customer, vehicle);
        }
    }
}
