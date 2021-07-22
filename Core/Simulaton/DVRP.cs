using System;
using System.Collections.Generic;
using SimSharp;
using DVRP.Communication;
using DVRP.Domain;
using System.Linq;
using System.Threading;
using System.Text.Json;

namespace DVRP.Simulaton
{
    public class DVRP
    {
        private Domain.Request[] advancedRequests;
        private Dictionary<int, Domain.Request> dynamicRequests;
        private Dictionary<int, Domain.Request> currentRequests = new Dictionary<int, Domain.Request>();

        private int vehicleCount = 5;
        private int vehicleCapacity = 100;
        private TimeSpan serviceTime = TimeSpan.FromMinutes(5);
        private Domain.Request depot = new Domain.Request(0, 0, 0);

        private int realTimeEnforcer = 0;
        private Solution solution;

        private Store[] pipes;
        private Vehicle[] vehicles;

        private int[] currentSolutionIdx;

        private Communication.SimulationQueue eventQueue;
        private Store requestPipe;

        private IEnumerable<Event> DynamicRequestHandler(PseudoRealtimeSimulation env) {
            env.Log("Publish");
            realTimeEnforcer++;
            env.SetRealtime();
            eventQueue.Publish(new Problem(advancedRequests, vehicleCount, vehicleCapacity));
            Thread.Sleep(500);
            eventQueue.Publish(new Problem(advancedRequests, vehicleCount, vehicleCapacity));

            foreach (var request in dynamicRequests) {
                yield return env.Timeout(TimeSpan.FromSeconds(request.Key));
                currentRequests.Add(currentRequests.Count, request.Value);
                realTimeEnforcer++;
                env.SetRealtime();
                eventQueue.Publish(new Problem(currentRequests, vehicleCount, vehicleCapacity));
            }
        }

        private IEnumerable<Event> Dispatcher(PseudoRealtimeSimulation env, Store pipe) {
            // problem: dispatcher needs to be able to take initiative to give orders at the start.
            // otherwise vehicles that have no initial orders will not start to drive later if necessary!
            while(true) {
                var get = pipe.Get();
                yield return get;

                var vehicle = (int) get.Value;

                while (solution == null) {
                    //env.Log($"Waiting for initial solution");
                    Thread.Sleep(100);
                }

                if(vehicle == -1) {
                    for(int i = 0; i < vehicleCount; i++) {
                        if (solution.Data[i].Count() > currentSolutionIdx[i]) {
                            pipes[i].Put(solution.Data[i].ElementAt<int>(currentSolutionIdx[i]));
                            currentSolutionIdx[i]++;
                        }
                    }
                } else if(solution.Data[vehicle].Count() > currentSolutionIdx[vehicle]) {
                    pipes[vehicle].Put(solution.Data[vehicle].ElementAt<int>(currentSolutionIdx[vehicle]));
                    currentSolutionIdx[vehicle]++;
                }
            }
        }

        /// <summary>
        /// A vehicle that drives to requests as it is told
        /// </summary>
        public class Vehicle : ActiveObject<PseudoRealtimeSimulation> {
            
            /// <summary>
            /// Identifier
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// How much the vehicle can carry
            /// </summary>
            public int Capacity { get; set; }

            /// <summary>
            /// Ctor
            /// </summary>
            /// <param name="env">Simulation environment</param>
            /// <param name="capacity">Carrying capacity of the vehicle</param>
            /// <param name="pipe">Contains the next order</param>
            /// <param name="id">Unique identifier</param>
            /// <param name="request">Store where the vehicle can request an order by putting in its id</param>
            public Vehicle(PseudoRealtimeSimulation env, int capacity, Store pipe, int id, Store request) : base(env) {
                Capacity = capacity;

                env.Process(Working(id, env, pipe, request));
            }

            /// <summary>
            /// Work loop
            /// </summary>
            /// <param name="id">Unique identifier</param>
            /// <param name="env">Simulation environment</param>
            /// <param name="pipe">Contains the next order</param>
            /// <param name="request">Store where the vehicle can request an order by putting in its id</param>
            /// <returns></returns>
            private IEnumerable<Event> Working(int id, PseudoRealtimeSimulation env, Store pipe, Store request) {
                while(true) {
                    env.Log($"[{id}] Requesting next assignment");
                    request.Put(id);

                    env.Log($"[{id}] Waiting for next assignment");
                    var get = pipe.Get();
                    yield return get;

                    var assignment = (int) get.Value;
                    env.Log($"[{id}] Driving to customer {assignment}.");

                    // travel time
                    yield return env.TimeoutD(15);

                    env.Log($"[{id}] Arrived at customer {assignment}.");

                    // service time
                    yield return env.TimeoutD(3);
                    env.Log($"[{id}] Serviced customer {assignment}.");
                }
            }
        }

        public void Simulate(string pubConnectionStr, string subConnectionString, int rseed = 42) {
            var start = DateTime.Now;
            var env = new PseudoRealtimeSimulation(start, rseed);

            currentSolutionIdx = new int[vehicleCount];

            // load problem instance
            advancedRequests = LoadAdvancedRequestsMock();
            dynamicRequests = LoadDynamicRequestsMock();

            foreach(var request in advancedRequests) {
                currentRequests.Add(currentRequests.Count, request);
            }

            // publish advanced requests to queue
            eventQueue = new SimulationQueue(pubConnectionStr, subConnectionString);

            eventQueue.OnEvent += (sender, args) => {
                env.Log("Received msg");
                switch (args.Topic) {
                    case "decision":
                        HandleDecision(args.Message);
                        realTimeEnforcer--;
                        if(realTimeEnforcer == 0) {
                            env.SetVirtualtime();
                        }
                        
                        break;
                    case "score":
                        env.Log("Received score request");
                        HandleScore(args.Message);
                        break;
                    default:
                        throw new Exception($"Could not handle event in topic {args.Topic}");
                }
            };

            env.Process(DynamicRequestHandler(env));

            requestPipe = new Store(env);
            env.Process(Dispatcher(env, requestPipe));

            // create pipes
            pipes = Enumerable.Range(0, vehicleCount).Select(x => new Store(env)).ToArray();

            // create vehicles
            var vehicles = Enumerable.Range(0, vehicleCount).Select(x => new Vehicle(env, vehicleCapacity, pipes[x], x, requestPipe)).ToArray();

            env.Run();
        }

        private void HandleDecision(string message) {
            realTimeEnforcer--;
            solution = JsonSerializer.Deserialize<Solution>(message);
            requestPipe.Put(-1);
            //Console.WriteLine($"Received decision: {message}");
        }

        private void HandleScore(string message) {
            var score = CalcScore(JsonSerializer.Deserialize<Solution>(message));
            Console.WriteLine($"Publish score: {score}");
            eventQueue.Publish(score);
        }

        private double CalcScore(Solution solution) {
            double totalLength = 0;

            foreach(var route in solution.Data) {
                Domain.Request prev = depot;
                var capacity = vehicleCapacity;
                double length = 0;

                foreach(var request in route) {
                    capacity -= currentRequests[request].Amount;

                    if (capacity < 0) // infeasible solution
                        return -1.0;

                    length += prev.Distance(currentRequests[request]);
                    prev = currentRequests[request];
                }

                totalLength += length;
            }

            return totalLength;
        }

        private Domain.Request[] LoadAdvancedRequestsMock() {
            return new Domain.Request[] {
                new Domain.Request(12, 4, 10),
                new Domain.Request(2, 3, 15),
                new Domain.Request(20, 40, 30),
                new Domain.Request(12, 33, 2),
                new Domain.Request(4, 44, 37),
                new Domain.Request(21, 23, 50)
            };
        }
        private Dictionary<int, Domain.Request> LoadDynamicRequestsMock() {
            return new Dictionary<int, Domain.Request>() {
                { 1, new Domain.Request(10, 3, 5) },
                { 4, new Domain.Request(40, 23, 70) }
            };
        }
    }
}
