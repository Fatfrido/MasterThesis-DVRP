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

        public Solution(int vehicleCount)
        {
            this.vehicleCount = vehicleCount;
        }

        public Solution Swap(int x, int y)
        {
            var solution = new Solution(vehicleCount);

            var copy = Route.ToArray();

            // swap
            var tmp = copy[x];
            copy[x] = copy[y];
            copy[y] = tmp;

            solution.Route = copy;

            return solution;
        }

        public Solution MoveRequest(int from, int to)
        {
            var solution = new Solution(vehicleCount);

            var request = Route[from];
            var newRoute = Route.ToList();

            newRoute.RemoveAt(from);
            newRoute.Insert(to, request);

            solution.Route = newRoute.ToArray();

            return solution;
        }

        public bool IsValid() => Cost >= 0;

        public DVRP.Domain.Solution ConvertToDomainSolution()
        {
            var solution = new DVRP.Domain.Solution(vehicleCount);
            int vehicle = -1;
            var route = new List<int>();

            // Split the big route at the duplicated depots to optain a single route for each vehicle
            for (int i = 0; i < Route.Length; i++)
            {
                if (IsDummyDepot(i))
                { // set current vehicle if a dummy depot is encountered 
                    if (vehicle >= 0)
                    { // not the first dummy depot
                        solution.AddRoute(vehicle, route.ToArray());
                        route.Clear();
                    }
                    vehicle = Route[i] - 1; // vehicles in the ACS start with 1 since 0 is the real depot
                }
                else
                { // request
                    route.Add(Route[i] - vehicleCount);
                }
            }

            solution.AddRoute(vehicle, route.ToArray());

            return solution;
        }

        /// <summary>
        /// Check if the request at given index is a dummy depot
        /// </summary>
        /// <param name="index"></param>
        /// <returns>True if the request is a dummy depot</returns>
        public bool IsDummyDepot(int index)
        {
            return Route[index] <= vehicleCount;
        }

        /// <summary>
        /// Get the actual index of a request without the influence of vehicles
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public int GetRealIndex(int index)
        {
            var realIndex = index - vehicleCount;
            return realIndex >= 0 ? realIndex : 0;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if (Route != null)
            {
                sb.AppendJoin('-', Route);
            }
            else
            {
                sb.Append("null");
            }

            return $"[{Cost}] ({sb})";
        }
    }
}
