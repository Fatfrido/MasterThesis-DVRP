using DVRP.Communication;
using DVRP.Domain;
using DVRP.Optimizer.ACS;
using DVRP.Optimizer.GA;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DVRP.Optimizer
{
    class Program
    {
        static TaskCompletionSource<double> tcs = null;
        private static OptimizerQueue queue;

        private static IPeriodicOptimizer PeriodicOptimizer;
        private static IContinuousOptimizer ContinuousOptimizer;

        static void Main(string[] args) {
            // Create optimizer
            //Optimizer = new SimpleConstructionHeuristic();
            //Optimizer = new TabuSearch();
            //PeriodicOptimizer = new ACSSolver(100, 3, 0.5, 0.5, 0.1);
            PeriodicOptimizer = new GAOptimizer(4, 2);
            //ContinuousOptimizer = new GAOptimizer();
            //ContinuousOptimizer.NewBestSolutionFound += HandleNewBestSolution;

            queue = new OptimizerQueue("tcp://*:12346", "tcp://localhost:12345");
            queue.OnEvent += (sender, args) => {
                switch (args.Topic) {
                    case "event":
                        HandleEvent(args.Message);
                        break;
                    case "score":
                        HandleScore(args.Message);
                        break;
                    default:
                        throw new Exception($"Cannot handle event in topic {args.Topic}");
                }
            };

            while(true) {
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Publishes the new best solution found by an continuous optimizer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="solution"></param>
        private static void HandleNewBestSolution(object sender, Domain.Solution solution) {
            queue.Publish(solution);
        }

        private static void HandleEvent(string message) {
            var problem = JsonConvert.DeserializeObject<Problem>(message);
            //ContinuousOptimizer.HandleNewProblem(problem);
            queue.Publish(PeriodicOptimizer.Solve(problem));

            //queue.Publish(TabuSearch.Solve(problem));
            //queue.Publish(ACSSolver.Solve(problem, 100, 3));
        }

        private static void HandleScore(string message) {
            tcs?.SetResult(double.Parse(message));
        }
    }
}
