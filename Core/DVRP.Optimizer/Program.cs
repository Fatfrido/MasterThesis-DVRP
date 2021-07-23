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
            /*var requests = new Request[] {
                new Request(0, 0, 0, 0),
                new Request(12, 4, 10, 1),
                new Request(2, 3, 15, 2),
                new Request(20, 40, 30, 3),
                new Request(12, 33, 2, 4),
                new Request(4, 44, 37, 5),
                new Request(21, 23, 50, 6)
            };

            //var start = Enumerable.Repeat(0, 5).ToArray();
            var start = Enumerable.Range(0, 5).ToArray();

            var requestNumber = requests.Length; // mind the depot!
            var matrix = new long[requestNumber, requestNumber];

            for (int fromNode = 0; fromNode < requestNumber; fromNode++) {
                for (int toNode = 0; toNode < requestNumber; toNode++) {
                    if (fromNode == toNode) {
                        matrix[fromNode, toNode] = 1;
                    } else {
                        // Handle depot since it is not contained in KnownRequests

                        // TODO add history!!
                        var toRequest = requests[toNode];
                        var fromRequest = requests[fromNode];

                        matrix[fromNode, toNode] = (long) Math.Sqrt(Math.Pow(toRequest.X - fromRequest.X, 2) +
                                                             Math.Pow(toRequest.Y - fromRequest.Y, 2));
                    }
                }
            }

            var problem = new Problem(
                requests,
                5,
                100,
                start,
                matrix
                );
            var solution = TabuSearch.Solve(problem);*/

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

            queue.Publish(TabuSearch.Solve(problem));
            //queue.Publish(ACSSolver.Solve(problem, 10, 3));
        }

        private static void HandleScore(string message) {
            tcs?.SetResult(double.Parse(message));
        }
    }
}
