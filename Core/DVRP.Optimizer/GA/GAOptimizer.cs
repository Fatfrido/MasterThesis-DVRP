using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Optimizer.GA
{
    public class GAOptimizer : IContinuousOptimizer
    {
        public event EventHandler<Solution> NewBestSolutionFound;

        private Problem problem;
        private int populationSize;
        private int k;
        private Random random = new Random();

        private bool stop = false;

        public GAOptimizer(int populationSize, int k) {
            this.populationSize = populationSize; // TODO: variable??
        }

        public void HandleNewProblem(Problem problem) {
            if(this.problem == null) {
                this.problem = problem;
            }
            //TODO
        }

        public void HandleStop() {
            stop = true;
        }

        private void Run(int initialCalculationTime, int elites, double mutationProbability) {
            // Make sure the problem is not null
            if(problem == null) {
                throw new ArgumentNullException("Problem must not be null");
            }

            var requests = problem.Requests.Select(x => x.Id).ToArray();

            // Generate initial population
            var population = GenerateInitialPopulation(populationSize, requests, problem.VehicleCount);

            // Sort (to get elites later on)
            population = population.OrderByDescending(x => x.Fitness).ToArray();

            // Evaluate population
            foreach(var individual in population) {
                individual.CalculateFitness(problem);
            }

            while(!stop) {
                var childPopulation = new List<Individual>();

                for(int i = 0; i < populationSize; i++) {
                    // Selection
                    var parent1 = SelectKTournament(k, population);
                    var parent2 = SelectKTournament(k, population);

                    // Crossover
                    var child = parent1.PartiallyMappedCrossover(parent2);

                    // Mutate
                    child.Mutate(mutationProbability);

                    // Evaluate
                    child.CalculateFitness(problem);

                    // Add to children to child population
                    childPopulation.Add(child);
                }

                // Replacement
                var newGeneration = childPopulation.OrderByDescending(x => x.Fitness).ToArray();

                // Elitism
                for(int i = 0; i < elites; i++) {
                    newGeneration[newGeneration.Length - 1 - i] = population[i];
                }

                population = newGeneration;
            }
        }

        /// <summary>
        /// K-tournament selection
        /// </summary>
        /// <param name="k"></param>
        /// <param name="population"></param>
        /// <returns></returns>
        private Individual SelectKTournament(int k, Individual[] population) {
            Individual bestIndividual = null;

            // Choose random individuals and return best one
            for(int i = 0; i < k; i++) {
                var individual = population[random.Next(0, population.Length - 1)];

                if(bestIndividual == null || bestIndividual.Fitness < individual.Fitness) {
                    bestIndividual = individual;
                }
            }

            return bestIndividual;
        }

        /// <summary>
        /// Creates an initial population randomly
        /// </summary>
        /// <param name="populationSize"></param>
        /// <param name="requests"></param>
        /// <param name="vehicleCount"></param>
        /// <returns></returns>
        private Individual[] GenerateInitialPopulation(int populationSize, int[] requests, int vehicleCount) {
            var population = new Individual[populationSize];

            for (int i = 0; i < population.Length; i++) {
                population[i] = new Individual(requests, vehicleCount);
            }

            return population;
        }
    }
}
