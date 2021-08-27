using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DVRP.Optimizer.GA
{
    public class GeneticAlgorithm : IContinuousOptimizer
    {
        private static int InstanceCounter = 0;
        private int instance = -1;

        public event EventHandler<Solution> NewBestSolutionFound;

        private Problem problem;
        private int populationSize;
        private int k;
        private int initialIterations;
        private int elites;
        private double mutationProbability;
        private double randomInsertionRate;
        private Random random = new Random();

        private Individual[] population;
        private Task optimizationTask;
        private CancellationTokenSource tokenSource;

        public GeneticAlgorithm(int populationSize, int k, int initialIterations, int elites, double mutationProbability, double randomInsertionRate) {
            this.populationSize = populationSize;
            this.k = k;
            this.initialIterations = initialIterations;
            this.elites = elites;
            this.mutationProbability = mutationProbability;
            this.randomInsertionRate = randomInsertionRate;

            instance = InstanceCounter;
            InstanceCounter++;
        }

        public void HandleNewProblem(Problem problem) {
            // Stop current execution
            Stop();

            // Update population
            if(population == null) {
                var requests = problem.Requests.Select(x => x.Id).ToArray();
                population = GenerateInitialPopulation(populationSize, requests, problem.VehicleCount);
            } else {
                UpdatePopulation(this.problem, problem);
            }

            // Update problem
            this.problem = problem;

            // Find solutions
            tokenSource = new CancellationTokenSource();
            optimizationTask = Task.Run(() => Run(initialIterations, elites, mutationProbability, tokenSource.Token));
        }

        public void Stop() {
            if (optimizationTask != null) {
                tokenSource.Cancel();
                optimizationTask.Wait();
            }
        }

        /// <summary>
        /// Updates the individuals of the popultation so that they contain the correct requests
        /// </summary>
        /// <param name="oldProblem"></param>
        /// <param name="newProblem"></param>
        private void UpdatePopulation(Problem oldProblem, Problem newProblem) {
            if(oldProblem != newProblem) {
                var toRemove = new List<int>();
                var toAdd = new List<int>();

                var newRequests = newProblem.Requests.Select(x => x.Id).ToArray();
                var oldRequests = oldProblem.Requests.Select(x => x.Id).ToArray();

                // Find requests that are new and must be added to the individuals
                foreach (var newRequest in newRequests) {
                    if (!oldRequests.Contains(newRequest)) {
                        toAdd.Add(newRequest);
                    }
                }

                // Find requests that are already commited and need to be removed from the individuals
                foreach (var oldRequest in oldRequests) {
                    if (!newRequests.Contains(oldRequest)) {
                        toRemove.Add(oldRequest);
                    }
                }

                // Remove requests
                foreach (var individual in population) {
                    individual.RemoveRequests(toRemove.ToArray());

                    foreach (var request in toAdd) {
                        if(random.NextDouble() < randomInsertionRate) {
                            individual.InsertRequestRandom(request, newProblem);
                        } else {
                            individual.InsertRequest(request, newProblem);
                        }
                    }
                }
            }
        }

        private void Run(int initialIterations, int elites, double mutationProbability, CancellationToken token) {
            // Best solution found for the current problem
            var bestSolutionFitness = -1.0;

            // Evaluate population
            foreach(var individual in population) {
                individual.CalculateFitness(problem);
            }

            // Sort (to get elites later on)
            population = population.OrderByDescending(x => x.Fitness).ToArray();
            
            while (!token.IsCancellationRequested && problem.Requests.Length > 0) {
                var childPopulation = new List<Individual>();

                for(int i = 0; i < populationSize; i++) {
                    // Stop eventually
                    if(token.IsCancellationRequested) {
                        return;
                    }

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
                for (int i = 0; i < elites; i++) {
                    newGeneration[newGeneration.Length - 1 - i] = population[i];
                }

                population = newGeneration;
                population = population.OrderByDescending(x => x.Fitness).ToArray();

                // Do not publish the result of the first few iterations
                if (initialIterations > 0) {
                    initialIterations--;
                }

                Console.WriteLine($"[{InstanceCounter}] Best solution: {population[0].Fitness}, Worst Solution: {population[populationSize - 1].Fitness}");
                if(population[0].Fitness > bestSolutionFitness && initialIterations <= 0) {
                    var res = population[0].ToSolution(problem);
                    bestSolutionFitness = population[0].Fitness;
                    NewBestSolutionFound(this, res);
                }
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
