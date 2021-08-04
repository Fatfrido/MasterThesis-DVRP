using DVRP.Communication;
using DVRP.Domain;
using DVRP.Optimizer.ACS;
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

        static void Main(string[] args) {
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

        private static void HandleEvent(string message) {
            var problem = JsonConvert.DeserializeObject<Problem>(message);

            //queue.Publish(SimpleConstructionHeuristic.Solve(problem));
            queue.Publish(TabuSearch.Solve(problem));
            //queue.Publish(ACSSolver.Solve(problem, 100, 3));
        }

        private static void HandleScore(string message) {
            tcs?.SetResult(double.Parse(message));
        }
    }
}
