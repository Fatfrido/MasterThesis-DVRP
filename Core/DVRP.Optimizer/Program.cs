using DVRP.Communication;
using DVRP.Domain;
using DVRP.Optimizer.ACS;
using DVRP.Optimizer.GA;
using DVRP.Optimizer.TS;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
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

        private static List<SimulationResult> simulationResults = new List<SimulationResult>();

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
                simulationResults.Add(results);
            };

            InitializeOptimizer(optimizerConfig.Optimizer, config);

            // Settings depending in the optimizer type
            if(PeriodicOptimizer != null) {
                allowFastSimulation = true; // Simulation speed will be increased if the optimizer is not busy
                queue.ProblemReceived += (sender, problem) => PublishSolution(null, PeriodicOptimizer.Solve(problem));
            } else {
                queue.ProblemReceived += (sender, problem) => ContinuousOptimizer.HandleNewProblem(problem);
            }

            Thread.Sleep(1000); // TODO wait until connection is ready

            Console.WriteLine("Optimizer is ready");

            // Run multiple simulations as defined in appsettings
            for (int i = 0; i < optimizerConfig.Iterations; i++) {
                // Create actual optimizer
                if(i < 1) {
                    InitializeOptimizer(optimizerConfig.Optimizer, config);
                }

                // Start simulation
                finished = false;
                queue.PublishStart(allowFastSimulation);

                while (!finished) {
                    Thread.Sleep(200);
                }
            }

            // Print results
            Console.WriteLine("====== RESULTS =====");
            foreach(var res in simulationResults) {
                Console.WriteLine($"Executed solution with score {res.Cost}:");
                Console.WriteLine(res.Solution);
            }

            File.WriteAllText($"results/{optimizerConfig.Optimizer}.json", JsonConvert.SerializeObject(new Report(simulationResults)));

            Console.ReadKey();
        }

        private static void InitializeOptimizer(string optimizer, IConfigurationRoot root) {
            var section = root.GetSection(optimizer);

            switch (optimizer) {
                case Optimizer.Heuristic:
                    PeriodicOptimizer = new SimpleConstructionHeuristic();
                    break;
                case Optimizer.TabuSearch:
                    var tsConfig = section.Get<TabuSearchConfig>();
                    PeriodicOptimizer = new TabuSearch();
                    break;
                case Optimizer.AntColonySystem:
                    var acsConfig = section.Get<AntColonySystemConfig>();
                    PeriodicOptimizer = new ACSSolver(acsConfig.Iterations, acsConfig.Ants, acsConfig.EvaporationRate, acsConfig.PheromoneImportance, acsConfig.InitialPheromoneValue);
                    break;
                case Optimizer.GeneticAlgorithm:
                    var gaConfig = section.Get<GeneticAlgorithmConfig>();
                    
                    if(ContinuousOptimizer != null) {
                        ContinuousOptimizer.NewBestSolutionFound -= PublishSolution;
                    }

                    ContinuousOptimizer = new GAOptimizer(gaConfig.PopulationSize, gaConfig.KTournament);
                    ContinuousOptimizer.NewBestSolutionFound += PublishSolution;
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
