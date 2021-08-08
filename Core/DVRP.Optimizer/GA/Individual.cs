using DVRP.Domain;
using System;
using System.Collections.Generic;
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
            RouteChromosome = new Chromosome(requests);
            VehicleChromosome = new Chromosome(vehicleCount);
        }

        public Individual(Chromosome routeChromosome, Chromosome vehicleChromosome) {
            RouteChromosome = routeChromosome;
            VehicleChromosome = vehicleChromosome;
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
            for (int j = 0; j < problem.Requests.Length; j++) {
                idToIndexMapping.Add(problem.Requests[i].Id, i);
            }

            var vehicle = VehicleChromosome[i];
            var capacity = problem.VehicleCapacity[vehicle - 1];
            var lastRequest = idToIndexMapping[problem.Start[vehicle]];

            foreach(var request in RouteChromosome) {
                // Check capacity constraint
                var demand = problem.Requests[idToIndexMapping[request]].Amount;
                while (capacity - demand < 0) {
                    i++;

                    if(i >= VehicleChromosome.Length) {
                        throw new IndexOutOfRangeException("Not enough vehicles to solve problem.");
                    }

                    vehicle = VehicleChromosome[i];
                    capacity = problem.VehicleCapacity[vehicle - 1];
                    lastRequest = idToIndexMapping[problem.Start[vehicle]];
                }

                capacity -= demand;
                cost += problem.CostMatrix[lastRequest, request];
            }

            var fitness = 1 / cost;
            Fitness = fitness;

            return fitness;
        }
    }
}
