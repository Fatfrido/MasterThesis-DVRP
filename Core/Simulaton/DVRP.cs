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
        //private Domain.Request[] advancedRequests;
        private Dictionary<int, Domain.Request> dynamicRequests;
        //private Dictionary<int, Domain.Request> currentRequests = new Dictionary<int, Domain.Request>();

        private int vehicleCount = 5;
        private int vehicleCapacity = 100;
        private TimeSpan serviceTime = TimeSpan.FromMinutes(5);
        private Domain.Request depot;

        private int realTimeEnforcer = 0;
        private Solution solution;

        private Store[] pipes;
        private Vehicle[] vehicles;

        private int[] currentSolutionIdx;

        private SimulationQueue eventQueue;
        private Store requestPipe;

        public WorldState WorldState { get; set; }

        private IEnumerable<Event> DynamicRequestHandler(PseudoRealtimeSimulation env) {
            env.Log("Publish");
            realTimeEnforcer++;
            env.SetRealtime();
            eventQueue.Publish(WorldState.ToProblem());
            Thread.Sleep(500);
            eventQueue.Publish(WorldState.ToProblem());

            foreach (var request in dynamicRequests) {
                yield return env.Timeout(TimeSpan.FromSeconds(request.Key));
                realTimeEnforcer++;
                env.SetRealtime();
                WorldState.AddRequest(request.Value);
                eventQueue.Publish(WorldState.ToProblem());
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

                if(vehicle == -1) { // A new solution is available
                    for(int i = 0; i < vehicleCount; i++) { // for every vehicle
                        if (solution.Data[i].Count() > 1 && vehicles[i].IsIdle) { // if there is a customer (not depot) planned for the vehicle and it is not doing anything
                            var nextRequest = solution.Data[i].First();

                            WorldState.CommitRequest(i, nextRequest);
                            pipes[i].Put(nextRequest); // assign the first customer on the route
                            currentSolutionIdx[i]++;
                        }
                    }
                } else if(solution.Data[vehicle].Count() > currentSolutionIdx[vehicle]) {
                    var nextRequest = solution.Data[vehicle].ElementAt<int>(currentSolutionIdx[vehicle]);

                    // Check if vehicle is not already at the next request (empty routes)
                    if(WorldState.CurrentRequests[vehicle].Id != nextRequest) {
                        pipes[vehicle].Put(nextRequest);
                        WorldState.CommitRequest(vehicle, nextRequest);
                        currentSolutionIdx[vehicle]++;
                    }
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
            /// The current request the vehicle is serving or driving to
            /// </summary>
            public Domain.Request CurrentRequest { get; set; }

            public bool IsIdle { get; private set; } = false;

            /// <summary>
            /// Ctor
            /// </summary>
            /// <param name="env">Simulation environment</param>
            /// <param name="capacity">Carrying capacity of the vehicle</param>
            /// <param name="pipe">Contains the next order</param>
            /// <param name="id">Unique identifier</param>
            /// <param name="dispatcherRequest">Store where the vehicle can request an order by putting in its id</param>
            public Vehicle(PseudoRealtimeSimulation env, int capacity, Store pipe, int id, Store dispatcherRequest, Domain.Request depot) : base(env) {
                Capacity = capacity;
                Id = id;
                CurrentRequest = depot; // start at the depot

                env.Process(Working(env, pipe, dispatcherRequest));
            }

            /// <summary>
            /// Work loop
            /// </summary>
            /// <param name="env">Simulation environment</param>
            /// <param name="pipe">Contains the next order</param>
            /// <param name="dispatcherRequest">Store where the vehicle can request an order by putting in its id</param>
            /// <returns></returns>
            private IEnumerable<Event> Working(PseudoRealtimeSimulation env, Store pipe, Store dispatcherRequest) {
                while(true) {
                    env.Log($"[{Id}] Requesting next assignment");
                    dispatcherRequest.Put(Id);
                    IsIdle = true;

                    env.Log($"[{Id}] Waiting for next assignment");
                    var get = pipe.Get();
                    yield return get;
                    IsIdle = false;

                    var assignment = (int) get.Value;
                    env.Log($"[{Id}] Driving to customer {assignment}.");

                    // travel time
                    yield return env.TimeoutD(15);

                    env.Log($"[{Id}] Arrived at customer {assignment}.");

                    // service time
                    yield return env.TimeoutD(3);
                    env.Log($"[{Id}] Serviced customer {assignment}.");
                }
            }
        }

        /// <summary>
        /// Starts the simulation
        /// </summary>
        /// <param name="pubConnectionStr"></param>
        /// <param name="subConnectionString"></param>
        /// <param name="rseed"></param>
        public void Simulate(string pubConnectionStr, string subConnectionString, int rseed = 42) {
            var start = DateTime.Now;
            var env = new PseudoRealtimeSimulation(start, rseed);

            currentSolutionIdx = new int[vehicleCount];

            // load problem instance
            depot = LoadDepotMock();
            dynamicRequests = LoadDynamicRequestsMock();

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

            WorldState = new WorldState(vehicleCount,
                depot, LoadAdvancedRequestsMock(),
                Enumerable.Repeat(vehicleCapacity,
                vehicleCount).ToArray()); // TODO: different capacities

            env.Process(DynamicRequestHandler(env));

            requestPipe = new Store(env);
            env.Process(Dispatcher(env, requestPipe));

            // create pipes
            pipes = Enumerable.Range(0, vehicleCount).Select(x => new Store(env)).ToArray();

            // create vehicles
            vehicles = Enumerable.Range(0, vehicleCount).Select(x => new Vehicle(env, vehicleCapacity, pipes[x], x, requestPipe, depot)).ToArray();

            env.Run();
        }

        private void HandleDecision(string message) {
            realTimeEnforcer--;
            solution = JsonSerializer.Deserialize<Solution>(message);

            // reset index of current solution
            for(int i = 0; i < currentSolutionIdx.Length; i++) {
                currentSolutionIdx[i] = 0;
            }

            requestPipe.Put(-1);
            //Console.WriteLine($"Received decision: {message}");
        }

        private void HandleScore(string message) {
            /*var score = CalcScore(JsonSerializer.Deserialize<Solution>(message));
            Console.WriteLine($"Publish score: {score}");
            eventQueue.Publish(score);*/
        }

        /*private double CalcScore(Solution solution) {
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
        }*/

        private Domain.Request LoadDepotMock() {
            return new Domain.Request(0, 0, 0, 0);
        }

        private Domain.Request[] LoadAdvancedRequestsMock() {
            return new Domain.Request[] {
                new Domain.Request(12, 4, 10, 1),
                new Domain.Request(2, 3, 15, 2),
                new Domain.Request(20, 40, 30, 3),
                new Domain.Request(12, 33, 2, 4),
                new Domain.Request(4, 44, 37, 5),
                new Domain.Request(21, 23, 50, 6)
            };
        }

        private Dictionary<int, Domain.Request> LoadDynamicRequestsMock() {
            return new Dictionary<int, Domain.Request>() {
                { 1, new Domain.Request(10, 3, 5, 7) },
                { 4, new Domain.Request(40, 23, 70, 8) }
            };
        }
    }
}
