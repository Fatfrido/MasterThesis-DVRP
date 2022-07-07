using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DVRP.Domain;
using Google.OrTools;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;

namespace DVRP.Optimizer
{
    public class TabuSearch : IPeriodicOptimizer
    {
        private int duration;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="duration">Time the TabuSearch uses to find a solution (in nanoseconds)</param>
        public TabuSearch(int duration)
        {
            this.duration = duration;
        }

        public Solution Solve(Problem problem)
        {
            // Create routing index manager
            RoutingIndexManager manager =
                new RoutingIndexManager(problem.CostMatrix.Dimension, problem.VehicleCount, problem.Start,
                                        Enumerable.Repeat(0, problem.VehicleCount).ToArray());

            // Create routing model
            RoutingModel routing = new RoutingModel(manager);

            // Create and register transit callback
            int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
            {
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                return problem.CostMatrix[fromNode, toNode];
            });

            // Define cost of each edge
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            // Get demand for each request
            var demands = new int[problem.CostMatrix.Dimension];
            problem.Requests.Select(r => r.Amount).ToArray().CopyTo(demands, 1);

            // Add capacity constraint
            int demandCallbackIndex = routing.RegisterUnaryTransitCallback((long fromIndex) =>
            {
                // Convert from routing variable index to demand index
                var fromNode = manager.IndexToNode(fromIndex);
                return demands[fromNode];
            });

            long[] capacities = problem.VehicleCapacity.Select(x => (long)x).ToArray(); // convert to long[]
            routing.AddDimensionWithVehicleCapacity(demandCallbackIndex, 0, capacities, true, "Capacity");

            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            // NOTE: first solution strategy is essential. with cheapest arc strategy this TSP would not be able to even solve easy problems
            // see: https://github.com/google/or-tools/issues/298
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
            searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.TabuSearch;
            searchParameters.TimeLimit = new Duration { Nanos = duration };

            Assignment solution = routing.SolveWithParameters(searchParameters);
            return ConvertToSolution(solution, problem, manager, routing);
        }

        /// <summary>
        /// Converts the results of the TSP optimizer to a <see cref="Solution"/>
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="problem"></param>
        /// <param name="manager"></param>
        /// <param name="routing"></param>
        /// <returns></returns>
        private Solution ConvertToSolution(in Assignment assignment, Problem problem, in RoutingIndexManager manager, in RoutingModel routing)
        {
            var solution = new Solution(problem.VehicleCount);

            for (int i = 0; i < problem.VehicleCount; i++)
            {
                var route = new List<int>();
                var index = routing.Start(i);

                while (!routing.IsEnd(index))
                {
                    var node = manager.IndexToNode((int)index);

                    if (node != 0 && node != problem.Start[i])
                    { // ignore the depot and the current position
                        route.Add(node);
                    }

                    index = assignment.Value(routing.NextVar(index));
                }
                solution.AddRoute(i, route.ToArray());
            }

            solution.ApplyMapping(problem.Mapping);

            // print solution after mapping
            //Console.WriteLine(solution);

            return solution;
        }
    }
}
