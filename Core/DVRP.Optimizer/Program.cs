﻿using DVRP.Communication;
using DVRP.Domain;
using DVRP.Optimizer.ACS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
            //var problem = JsonSerializer.Deserialize<Problem>(message);
            var problem = JsonConvert.DeserializeObject<Problem>(message);

            /*var solution = new List<int>[problem.VehicleCount];
            for (int i = 0; i < problem.VehicleCount; i++) {
                solution[i] = new List<int>();
            }

            int capacityLeft = problem.VehicleCapacity;
            int vehicle = 0;

            for (int i = 0; i < problem.Requests.Length; i++) {
                var amount = problem.Requests[i].Amount;

                if (capacityLeft >= amount) {
                    capacityLeft -= amount;
                } else {
                    vehicle++;
                    capacityLeft = problem.VehicleCapacity - amount;
                }

                solution[vehicle].Add(i);
            }

            Console.WriteLine("Requesting score...");

            queue.RequestScore(new Solution(solution));
            tcs = new TaskCompletionSource<double>();
            var score = await tcs.Task;

            Console.WriteLine($"... done. Score: {score}");

            queue.Publish(new Solution(solution));*/

            queue.Publish(TabuSearch.Solve(problem));
            //queue.Publish(ACSSolver.Solve(problem, 10, 3));
        }

        private static void HandleScore(string message) {
            tcs?.SetResult(double.Parse(message));
        }
    }
}