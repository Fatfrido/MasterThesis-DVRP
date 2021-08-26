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

            Thread.Sleep(1000); // TODO: this is very ugly => https://stackoverflow.com/questions/11634830/zeromq-always-loses-the-first-message/11654892

            var executionPlanJson = File.ReadAllText(optimizerConfig.ExecutionPlan);
            var executionPlan = JsonConvert.DeserializeObject<ExecutionPlan[]>(executionPlanJson);

            Console.WriteLine("Optimizer is ready");

            // Execute plan
            foreach(var entry in executionPlan) {
                Console.WriteLine($">>> Executing {entry}");

                var problemInstanceJson = File.ReadAllText($"instances/{entry.ProblemInstance}.json");
                var problemInstance = JsonConvert.DeserializeObject<ProblemInstance>(problemInstanceJson);

                // Run multiple simulations as defined in appsettings
                for (int i = 0; i < entry.Iterations; i++) {
                    Console.WriteLine($"Iteration {i + 1}");

                    // Create actual optimizer
                    InitializeOptimizer(entry.Optimizer, config);

                    // Settings depending in the optimizer type
                    RegisterEventHandlers();

                    // Start simulation
                    finished = false;
                    queue.PublishStart(new StartSimulationMessage(allowFastSimulation, problemInstance));

                    while (!finished) {
                        Thread.Sleep(200);
                    }

                    // Remove event handlers
                    ClearEventHandlers();

                    // Remove optimizer
                    ContinuousOptimizer = null;
                    PeriodicOptimizer = null;
                }

                Directory.CreateDirectory($"results/{problemInstance.Name}");
                File.WriteAllText($"results/{problemInstance.Name}/{problemInstance.Name}-{entry.Optimizer}.json", 
                    JsonConvert.SerializeObject(new Report(simulationResults)));

                var instance = simulationResults.First().Instance;
                Directory.CreateDirectory($"results/{instance}");
                File.WriteAllText($"results/{instance}/{instance}-{entry.Optimizer}.json", JsonConvert.SerializeObject(new Report(simulationResults)));
            }

            Console.WriteLine("Finished execution plan");

            Console.ReadKey();
        }

        private static void RegisterEventHandlers() {
            if (PeriodicOptimizer != null) {
                allowFastSimulation = true; // Simulation speed will be increased if the optimizer is not busy
                queue.ProblemReceived += HandleProblemReceivedPeriodic;
            } else {
                allowFastSimulation = false;
                queue.ProblemReceived += HandleProblemReceivedContinuous;
            }
        }

        private static void ClearEventHandlers() {
            if (PeriodicOptimizer != null) {
                queue.ProblemReceived -= HandleProblemReceivedPeriodic;
            } else {
                queue.ProblemReceived -= HandleProblemReceivedContinuous;
            }
        }

        private static void InitializeOptimizer(string optimizer, IConfigurationRoot root) {
            var section = root.GetSection(optimizer);

            switch (optimizer) {
                case Optimizer.Heuristic:
                    PeriodicOptimizer = new SimpleConstructionHeuristic();
                    break;
                case Optimizer.TabuSearch:
                    var tsConfig = section.Get<TabuSearchConfig>();
                    int duration = (int) Math.Round(TimeSpan.FromSeconds(tsConfig.Seconds).TotalMilliseconds) * 1000000; // nanoseconds are needed for TS
                    PeriodicOptimizer = new TabuSearch(duration);
                    break;
                case Optimizer.AntColonySystem:
                    var acsConfig = section.Get<AntColonySystemConfig>();
                    PeriodicOptimizer = new AntColonySystem(acsConfig.Iterations, acsConfig.Ants, acsConfig.EvaporationRate, acsConfig.PheromoneImportance, 100, 0.3, 0.9);
                    break;
                case Optimizer.GeneticAlgorithm:
                    var gaConfig = section.Get<GeneticAlgorithmConfig>();
                    
                    if(ContinuousOptimizer != null) {
                        ContinuousOptimizer.NewBestSolutionFound -= PublishSolution;
                    }

                    ContinuousOptimizer = new GeneticAlgorithm(gaConfig.PopulationSize, gaConfig.KTournament, gaConfig.InitialIterations, gaConfig.Elites, gaConfig.MutationRate);
                    ContinuousOptimizer.NewBestSolutionFound += PublishSolution;
                    break;
            }
        }

        private static void HandleProblemReceivedPeriodic(object sender, Problem problem) {
            PublishSolution(null, PeriodicOptimizer.Solve(problem));
        }

        private static void HandleProblemReceivedContinuous(object sender, Problem problem) {
            ContinuousOptimizer.HandleNewProblem(problem);
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
