﻿using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer
{
    public class SimpleConstructionHeuristic
    {
        public static Solution Solve(Problem problem) {
            var solution = new Solution(problem.VehicleCount);
            int currentVehicle = 0;
            int currentVehicleCapacity = problem.VehicleCapacity;
            var route = new List<int>();

            foreach(var request in problem.Requests) {
                if(currentVehicleCapacity - request.Amount < 0) {
                    // commit previous route
                    solution.AddRoute(currentVehicle, route.ToArray());

                    // select next vehicle
                    currentVehicle++;
                    currentVehicleCapacity = problem.VehicleCapacity;
                    route.Clear();
                }

                currentVehicleCapacity -= request.Amount;
                route.Add(request.Id);
            }

            solution.AddRoute(currentVehicle, route.ToArray());

            // create empty routes for unused vehicles
            if(currentVehicle < problem.VehicleCount - 1) {
                route.Clear();

                for(int i = currentVehicle + 1; i < problem.VehicleCount; i++) {
                    solution.AddRoute(i, route.ToArray());
                }
            }

            Console.WriteLine(solution);
            return solution;
        }
    }
}
