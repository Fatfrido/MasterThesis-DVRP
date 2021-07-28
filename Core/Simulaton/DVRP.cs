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
        private Dictionary<int, Domain.Request> dynamicRequests;

        private int vehicleCount = 5;
        private int vehicleCapacity = 100;
        private TimeSpan serviceTime = TimeSpan.FromMinutes(5);
        private Domain.Request depot;

        private int realTimeEnforcer = 0;

        private Store[] pipes;
        private Vehicle[] vehicles;

        private SimulationQueue eventQueue;
        private Store requestPipe;

        public static WorldState WorldState { get; set; }

        private IEnumerable<Event> DynamicRequestHandler(PseudoRealtimeSimulation env) {
            env.Log("Publish initial problem");
            realTimeEnforcer++;
            env.SetRealtime();
            Thread.Sleep(500); // TODO: this is very ugly => https://stackoverflow.com/questions/11634830/zeromq-always-loses-the-first-message/11654892
            eventQueue.Publish(WorldState.ToProblem());

            foreach (var request in dynamicRequests) {
                yield return env.Timeout(TimeSpan.FromSeconds(request.Key));
                realTimeEnforcer++;
                env.SetRealtime();
                Console.WriteLine(">>> New Request <<<");
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

                while (WorldState.Solution == null) {
                    //env.Log($"Waiting for initial solution");
                    Thread.Sleep(100);
                }

                if(vehicle == -1) { // A new solution is available
                    for(int i = 0; i < vehicleCount; i++) { // for every vehicle
                        if(vehicles[i].IsIdle) { // if there is a customer planned for the vehicle and it is not doing anything
                            int nextRequest;

                            if(WorldState.TryCommitNextRequest(i, out nextRequest)) {
                                pipes[i].Put(nextRequest);
                                eventQueue.Publish(WorldState.ToProblem());
                            }

                        }
                    }
                } else {
                    int nextRequest;

                    if(WorldState.TryCommitNextRequest(vehicle, out nextRequest)) {
                        pipes[vehicle].Put(nextRequest);
                        eventQueue.Publish(WorldState.ToProblem());
                    }
                } 
                
                /*else if(WorldState.Solution.Data[vehicle].Count() > currentSolutionIdx[vehicle]) {
                    var nextRequest = WorldState.Solution.Data[vehicle].ElementAt<int>(currentSolutionIdx[vehicle]);

                    // Check if vehicle is not already at the next request (empty routes)
                    if(WorldState.CurrentRequests[vehicle] != nextRequest) {
                        if(WorldState.CommitRequest(vehicle, nextRequest)) {
                            pipes[vehicle].Put(nextRequest);
                            currentSolutionIdx[vehicle]++;
                            eventQueue.Publish(WorldState.ToProblem());
                        }
                    }
                }*/
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
            public int CurrentRequest { get; set; }

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
                CurrentRequest = depot.Id; // start at the depot

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

                    // Driving to the current position is not possible. Ignore and continue
                    if (assignment != CurrentRequest) {
                        var travelTime = WorldState.CostMatrix[CurrentRequest, assignment];
                        CurrentRequest = assignment;

                        env.Log($"[{Id}] Driving to customer {assignment}.");

                        // travel time
                        yield return env.Timeout(TimeSpan.FromSeconds(travelTime));

                        env.Log($"[{Id}] Arrived at customer {assignment}.");

                        // service time
                        yield return env.Timeout(TimeSpan.FromSeconds(1));
                        env.Log($"[{Id}] Serviced customer {assignment}.");
                    }
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

            // load problem instance
            depot = LoadDepotMock();
            dynamicRequests = LoadDynamicRequestsMock();

            // publish advanced requests to queue
            eventQueue = new SimulationQueue(pubConnectionStr, subConnectionString);

            eventQueue.OnEvent += (sender, args) => {
                switch (args.Topic) {
                    case "decision":
                        HandleDecision(args.Message);
                        realTimeEnforcer--;
                        if(realTimeEnforcer == 0) {
                            env.SetVirtualtime();
                        }
                        
                        break;
                    case "score":
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

            // TODO send result to optimizer
            Console.WriteLine(WorldState.GetFinalSolution());
            Console.WriteLine($"Final cost: {WorldState.EvaluateCurrentSolution()}");

            Console.ReadKey();
        }

        private void HandleDecision(string message) {
            realTimeEnforcer--;
            var solution = JsonSerializer.Deserialize<Solution>(message);

            // Check if the solution is still feasible for the current world state
            var cost = WorldState.EvaluateSolution(solution);
            Console.WriteLine($"Received solution with cost: {cost}");
            Console.WriteLine(solution);

            if (WorldState.TrySetNewSolution(solution)) {
                // notify dispatcher
                requestPipe.Put(-1);
            }
        }

        private void HandleScore(string message) {
            /*var score = CalcScore(JsonSerializer.Deserialize<Solution>(message));
            Console.WriteLine($"Publish score: {score}");
            eventQueue.Publish(score);*/
        }

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
