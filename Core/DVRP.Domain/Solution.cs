using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Domain
{
    public class Solution
    {
        public int[][] Data { get; set; }

        public Solution() { }

        public Solution(int vehicleNumber) {
            Data = new int[vehicleNumber][];
        }

        public void AddRoute(int vehicle, int[] route) {
            Data[vehicle] = route;
        }

        public int[] GetRoute(int vehicle) {
            return Data[vehicle];
        }
    }
}
