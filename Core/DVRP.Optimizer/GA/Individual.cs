using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Optimizer.GA
{
    public class Individual
    {
        private static Random random = new Random();

        public Chromosome RouteChromosome { get; private set; }
        public Chromosome VehicleChromosome { get; private set; }
        public double Fitness { get; private set; }

        public Individual(int[] requests, int vehicleCount) {
            RouteChromosome = new Chromosome(requests.ToArray().Shuffle());
            VehicleChromosome = new Chromosome(vehicleCount);
        }

        public Individual(Chromosome routeChromosome, Chromosome vehicleChromosome) {
            RouteChromosome = routeChromosome;
            VehicleChromosome = vehicleChromosome;
        }

        /// <summary>
        /// Removes a request from the chromosome
        /// </summary>
        /// <param name="request"></param>
        public void RemoveRequests(int[] requests) {
            RouteChromosome.Remove(requests);
        }

        /// <summary>
        /// Inserts a request at the best possible position
        /// </summary>
        /// <param name="request"></param>
        /// <param name="problem"></param>
        public void InsertRequest(int request, Problem problem) {
            // Find the best position to insert the new request
            var bestFitness = -1.0;
            Chromosome bestChromosome = null;

            for(int i = 0; i < RouteChromosome.Length + 1; i++) {
                var newRouteChromosome = RouteChromosome.Insert(i, request);
                var individual = new Individual(newRouteChromosome, VehicleChromosome);
                var fitness = individual.CalculateFitness(problem);

                if(fitness > bestFitness || bestChromosome == null) {
                    bestFitness = fitness;
                    bestChromosome = newRouteChromosome;
                }
            }

            // Replace old chromosome with a new feasible one
            RouteChromosome = bestChromosome;
        }

        /// <summary>
        /// Applies the partially mapped crossover to both chromosomes
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public Individual PartiallyMappedCrossover(Individual parent) {
            var childRouteChromosome = RouteChromosome.PartiallyMappedCrossover(parent.RouteChromosome);
            var childVehicleChromosome = VehicleChromosome.PartiallyMappedCrossover(parent.VehicleChromosome);

            return new Individual(childRouteChromosome, childVehicleChromosome);
        }

        /// <summary>
        /// Applies mutation to <see cref="RouteChromosome"/> and <see cref="VehicleChromosome"/>
        /// </summary>
        /// <param name="probability">Probability for one of the chromosomes to mutate. 0 <= probability <= 1</param>
        public void Mutate(double probability) {
            Mutate(probability, RouteChromosome);
            Mutate(probability, VehicleChromosome);
        }

        /// <summary>
        /// Applies mutation with a certain probability to a chromosome
        /// </summary>
        /// <param name="probability"></param>
        /// <param name="chromosome"></param>
        public void Mutate(double probability, Chromosome chromosome) {
            var r = random.NextDouble();

            if (r < probability) {
                chromosome.ApplyInversionMutation();
            }
        }

        /// <summary>
        /// Calculates fitness of this individual. <see cref="Fitness"/> is automatically updated
        /// </summary>
        /// <param name="problem"></param>
        /// <returns></returns>
        public double CalculateFitness(Problem problem) {
            var i = 0;
            var cost = 0.0;

            // Create inverse request mapping (id => index)
            var idToIndexMapping = new Dictionary<int, int>();
            idToIndexMapping.Add(0, 0); // depot
            for (int j = 0; j < problem.Requests.Length; j++) {
                idToIndexMapping.Add(problem.Requests[j].Id, j);
            }

            var vehicle = VehicleChromosome[i];
            var capacity = problem.VehicleCapacity[vehicle - 1];
            var lastRequest = problem.Mapping[problem.Start[vehicle - 1]];

            foreach(var request in RouteChromosome) {
                // Check capacity constraint
                var demand = problem.Requests[idToIndexMapping[request]].Amount;
                while (capacity - demand < 0) {
                    // Drive vehicle back to depot
                    cost += problem.GetCost(lastRequest, 0);

                    i++;

                    if(i >= VehicleChromosome.Length) {
                        throw new IndexOutOfRangeException("Not enough vehicles to solve problem.");
                    }

                    vehicle = VehicleChromosome[i];
                    capacity = problem.VehicleCapacity[vehicle - 1];
                    lastRequest = problem.Mapping[problem.Start[vehicle - 1]];
                }

                capacity -= demand;
                cost += problem.GetCost(lastRequest, request);
            }

            var fitness = 1 / cost;
            Fitness = fitness;

            return fitness;
        }

        /// <summary>
        /// Transform individual to a solution
        /// </summary>
        /// <param name="problem"></param>
        /// <returns></returns>
        public Solution ToSolution(Problem problem) {
            var solution = new Solution(VehicleChromosome.Length);
            var vehicleIndex = 0;
            var vehicle = VehicleChromosome[vehicleIndex];
            var capacity = problem.VehicleCapacity[vehicle - 1];
            var route = new List<int>();

            foreach(var request in RouteChromosome) {
                var demand = problem.Requests.Where(x => x.Id == request).First().Amount;
                capacity -= demand;

                while(capacity < 0) {
                    // Save current route
                    solution.AddRoute(vehicle, route.ToArray());
                    route.Clear();

                    // Next vehicle
                    vehicleIndex++;
                    vehicle = VehicleChromosome[vehicleIndex];
                    capacity = problem.VehicleCapacity[vehicle - 1];
                    capacity -= demand;
                }

                route.Add(request);
            }

            solution.AddRoute(vehicle, route.ToArray());

            return solution;
        }
    }
}
