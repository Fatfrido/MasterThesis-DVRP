using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Optimizer.ACS
{
    public class Solution
    {
        public int[] Route { get; set; }
        public double Cost { get; set; }
        private int vehicleCount;

        public Solution(int vehicleCount) {
            this.vehicleCount = vehicleCount;
        }

        public Solution Swap(int x, int y) {
            var solution = new Solution(vehicleCount);

            var copy = Route.ToArray();

            // swap
            var tmp = copy[x];
            copy[x] = copy[y];
            copy[y] = tmp;

            solution.Route = copy;

            return solution;
        }

        public DVRP.Domain.Solution ConvertToDomainSolution() {
            var solution = new DVRP.Domain.Solution();
            int vehicle = -1;
            var route = new List<int>();

            // Split the big route at the duplicated depots to optain a single route for each vehicle
            for(int i = 0; i < Route.Length; i++) {
                if(IsDummyDepot(i)) { // Change current vehicle
                    if(vehicle < 0) { // not the first vehicle
                        solution.AddRoute(vehicle, route.ToArray());
                        route.Clear();
                    }
                    vehicle = Route[i];
                } else { // Add customer to route
                    route.Add(Route[i]);
                }
            }

            solution.AddRoute(vehicle, route.ToArray());

            return solution;
        }

        public bool IsDummyDepot(int index) {
            return 0 <= index && index <= vehicleCount;
        }

        public int GetRealIndex(int index) {
            var realIndex = index - vehicleCount;
            return realIndex >= 0 ? realIndex : 0;
        }

        public override string ToString() {
            var sb = new StringBuilder();

            if(Route != null) {
                sb.AppendJoin('-', Route);
            } else {
                sb.Append("null");
            }

            return $"[{Cost}] ({sb})";
        }
    }
}
