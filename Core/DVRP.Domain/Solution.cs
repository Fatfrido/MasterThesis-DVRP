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

        public void ApplyMapping(int[] mapping) {
            var vehicles = Data.GetLength(0) - 1;

            for(int i = 0; i < vehicles; i++) {
                for(int j = 0; j < Data[i].Length; j++) {
                    Data[i][j] = mapping[Data[i][j]];
                }
            }
        }
    }
}
