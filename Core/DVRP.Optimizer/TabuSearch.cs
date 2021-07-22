using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DVRP.Domain;
using Google.OrTools;
using Google.OrTools.ConstraintSolver;

namespace DVRP.Optimizer
{
    public class TabuSearch
    {

        public static Solution Solve(Problem problem) {
            RoutingIndexManager manager = 
                new RoutingIndexManager(problem.CostMatrix.GetLength(0), problem.VehicleCount, problem.Start, 
                                        Enumerable.Repeat(problem.Depot, problem.VehicleCount).ToArray());

            RoutingModel routing = new RoutingModel(manager);

            int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) => {
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                return problem.CostMatrix[fromNode, toNode];
            });

            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            //routing.SetArcCostEvaluatorOfVehicle(transitCallbackIndex, vehicle);

            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;

            Console.WriteLine("Calc solution...");
            Assignment solution = routing.SolveWithParameters(searchParameters);
            return ConvertToSolution(solution, problem, manager, routing);
        }

        private static Solution ConvertToSolution(in Assignment assignment, Problem problem, in RoutingIndexManager manager, in RoutingModel routing) {
            //Console.WriteLine("Convert assignment to solution...");
            var solution = new Solution(problem.VehicleCount);

            for(int i = 0; i < problem.VehicleCount; i++) {
                Console.WriteLine($"Process vehicle {i}:");
                var route = new List<int>();
                var index = routing.Start(i);

                while(!routing.IsEnd(index)) {
                    var node = manager.IndexToNode((int) index);
                    Console.WriteLine($"\t- {node}");
                    route.Add(node);
                    index = assignment.Value(routing.NextVar(index));
                }
                solution.AddRoute(i, route.ToArray());
            }

            Console.WriteLine("... done");

            return solution;
        }
    }
}
