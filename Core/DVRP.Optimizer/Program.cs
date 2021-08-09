using DVRP.Communication;
using DVRP.Domain;
using DVRP.Optimizer.ACS;
using DVRP.Optimizer.GA;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DVRP.Optimizer
{
    class Program
    {
        private static IOptimizerQueue queue;

        private static IPeriodicOptimizer PeriodicOptimizer;
        private static IContinuousOptimizer ContinuousOptimizer;

        private static bool finished = false;
        private static bool allowFastSimulation = false;

        static void Main(string[] args) {
            Console.WriteLine("Initializing optimizer...");

            // Read appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();

            var section = config.GetSection(nameof(OptimizerConfig));
            var optimizerConfig = section.Get<OptimizerConfig>();

            // Create queue
            queue = new OptimizerQueue(optimizerConfig.PublishConnectionString, optimizerConfig.SubscribeConnectionString);

            queue.ResultsReceived += (sender, results) => {
                finished = true;
                Console.WriteLine($"Executed solution with score {results.Cost}:");
                Console.WriteLine(results.Solution);
            };

            InitializeOptimizer(optimizerConfig.Optimizer);

            // Settings depending in the optimizer type
            if(PeriodicOptimizer != null) {
                allowFastSimulation = true; // Simulation speed will be increased if the optimizer is not busy
                queue.ProblemReceived += (sender, problem) => PublishSolution(null, PeriodicOptimizer.Solve(problem));
            } else {
                ContinuousOptimizer.NewBestSolutionFound += PublishSolution;
                queue.ProblemReceived += (sender, problem) => ContinuousOptimizer.HandleNewProblem(problem);
            }

            Thread.Sleep(1000); // TODO wait until connection is ready

            Console.WriteLine("Optimizer is ready");

            // Run multiple simulations as defined in appsettings
            for (int i = 0; i < optimizerConfig.Iterations; i++) {
                // Create actual optimizer
                if(i < 1) {
                    InitializeOptimizer(optimizerConfig.Optimizer);
                }

                // Start simulation
                finished = false;
                queue.PublishStart(allowFastSimulation);

                while (!finished) {
                    Thread.Sleep(200);
                }
            }
        }

        private static void InitializeOptimizer(string optimizer) {
            switch (optimizer) {
                case Optimizer.Heuristic:
                    PeriodicOptimizer = new SimpleConstructionHeuristic();
                    break;
                case Optimizer.TabuSearch:
                    PeriodicOptimizer = new TabuSearch();
                    break;
                case Optimizer.AntColonySystem:
                    PeriodicOptimizer = new ACSSolver(100, 3, 0.5, 0.5, 0.1);
                    break;
                case Optimizer.GeneticAlgorithm:
                    ContinuousOptimizer = new GAOptimizer(4, 1);
                    break;
            }
        }

        /// <summary>
        /// Publishes the new best solution found by an continuous optimizer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="solution"></param>
        private static void PublishSolution(object sender, Domain.Solution solution) {
            queue.Publish(solution);
        }
    }
}
