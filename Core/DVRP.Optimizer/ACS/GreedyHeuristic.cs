using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Optimizer.ACS
{
    public class GreedyHeuristic : IPeriodicOptimizer
    {
        public Domain.Solution Solve(Problem problem)
        {
            var solution = new Domain.Solution(problem.VehicleCount);

            // Key is the index, value the request
            var requests = Enumerable.Range(0, problem.Requests.Length)
                .ToDictionary(x => x + 1, x => problem.Requests[x]);

            // Create a tour for every vehicle
            for (int i = 0; i < problem.VehicleCount; i++)
            {
                var route = new List<int>();
                var capacity = problem.VehicleCapacity[i];
                var lastRequest = problem.Start[i];
                var stop = false;

                // Assign as many requests to the vehicle as possible
                while (capacity > 0 && requests.Count > 0 && !stop)
                {
                    // Add request with minimal cost
                    var best = -1;
                    long bestCost = -1;

                    foreach (var r in requests)
                    {
                        var cost = problem.CostMatrix[lastRequest, r.Key];

                        if (capacity - r.Value.Amount >= 0)
                        {
                            if (bestCost < 0 || cost < bestCost)
                            {
                                bestCost = cost;
                                best = r.Key;
                            }
                        }
                    }

                    // Only add valid options to route
                    if (best > 0)
                    {
                        route.Add(best);
                        capacity -= requests[best].Amount;
                        lastRequest = best;
                        requests.Remove(best);
                    }
                    else
                    { // Stop adding requests to this vehicle
                        stop = true;
                    }
                }

                solution.AddRoute(i, route.ToArray());
            }

            solution.ApplyMapping(problem.Mapping);

            return solution;
        }
    }
}
