using System;
using System.Collections.Generic;
using SimSharp;
using DVRP.Communication;
using DVRP.Domain;
using System.Linq;
using System.Threading;

namespace DVRP.Simulaton
{
    public class DVRP
    {
        private Dictionary<int, Domain.Request> dynamicRequests;

        private int vehicleCount = 5;
        private TimeSpan serviceTime = TimeSpan.FromMinutes(5);
        private Domain.Request depot;
        private ProblemInstance problemInstance;

        private int realTimeEnforcer = 0;
        private bool allowFastSimulation;

        private Store[] pipes;
        private Vehicle[] vehicles;

        private ISimulationQueue eventQueue;
        private Store requestPipe;
        private PseudoRealtimeSimulation env;

        public WorldState WorldState { get; set; }

        public DVRP(ISimulationQueue queue, ProblemInstance dvrp) {
            eventQueue = queue;
            problemInstance = dvrp;
        }

        private IEnumerable<Event> DynamicRequestHandler(PseudoRealtimeSimulation env) {
            env.Log("Publish initial problem");
            //Thread.Sleep(500); // TODO: this is very ugly => https://stackoverflow.com/questions/11634830/zeromq-always-loses-the-first-message/11654892
            PublishProblem(env, WorldState.ToProblem());

            foreach (var request in dynamicRequests) {
                yield return env.Timeout(TimeSpan.FromSeconds(request.Key));
                Console.WriteLine(">>> New Request <<<");
                WorldState.AddRequest(request.Value);
                PublishProblem(env, WorldState.ToProblem());
            }
        }

        private IEnumerable<Event> Dispatcher(PseudoRealtimeSimulation env, Store pipe) {
            // problem: dispatcher needs to be able to take initiative to give orders at the start.
            // otherwise vehicles that have no initial orders will not start to drive later if necessary!
            while(true) {
                var get = pipe.Get();
                yield return get;

                var vehicle = (int) get.Value;

                // Wait for initial solution
                while (WorldState.Solution == null) {
                    Thread.Sleep(100);
                }

                if(vehicle == -1) { // A new solution is available
                    for(int i = 0; i < vehicleCount; i++) { // for every vehicle
                        if(vehicles[i].IsIdle) { // if there is a customer planned for the vehicle and it is not doing anything
                            ApplyNextRequest(i, pipes[i], env);
                        }
                    }
                } else {
                    ApplyNextRequest(vehicle, pipes[vehicle], env);
                }
            }
        }

        /// <summary>
        /// Assigns the next request to a vehicle if possible and publishes a new problem
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="pipe"></param>
        /// <param name="env"></param>
        private void ApplyNextRequest(int vehicle, Store pipe, PseudoRealtimeSimulation env) {
            int nextRequest;

            if (WorldState.TryCommitNextRequest(vehicle, out nextRequest)) {
                pipe.Put(nextRequest);
                PublishProblem(env, WorldState.ToProblem());
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
            public Vehicle(PseudoRealtimeSimulation env, Store pipe, int id, Store dispatcherRequest, WorldState worldState, int startIdx = 0) : base(env) {
                Id = id;
                CurrentRequest = startIdx; // start at the depot

                env.Process(Working(env, pipe, dispatcherRequest, worldState));
            }

            /// <summary>
            /// Work loop
            /// </summary>
            /// <param name="env">Simulation environment</param>
            /// <param name="pipe">Contains the next order</param>
            /// <param name="dispatcherRequest">Store where the vehicle can request an order by putting in its id</param>
            /// <returns></returns>
            private IEnumerable<Event> Working(PseudoRealtimeSimulation env, Store pipe, Store dispatcherRequest, WorldState worldState) {
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
                        var travelTime = worldState.CostMatrix[CurrentRequest, assignment];
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
        /// <param name="rseed">Random seed for the simulation</param>
        public void Simulate(bool allowFastSimulation, int rseed = 42) {
            env = new PseudoRealtimeSimulation(DateTime.Now, rseed);
            this.allowFastSimulation = allowFastSimulation;

            // load problem instance
            depot = new Domain.Request(problemInstance.XLocations[0], problemInstance.YLocations[0], 0, 0);
            problemInstance.GetRequests(out var initialRequests, out dynamicRequests);

            // Register event handler
            eventQueue.SolutionReceived += HandleSolution;

            // Create world state
            WorldState = new WorldState(vehicleCount, depot, initialRequests, problemInstance.VehicleTypes);

            // Start dynamic request handler
            env.Process(DynamicRequestHandler(env));

            // Create and start dispatcher
            requestPipe = new Store(env);
            env.Process(Dispatcher(env, requestPipe));

            // create pipes for vehicles
            pipes = Enumerable.Range(0, vehicleCount).Select(x => new Store(env)).ToArray();

            // create vehicles
            vehicles = Enumerable.Range(0, vehicleCount).Select(x => new Vehicle(env, pipes[x], x, requestPipe, WorldState)).ToArray();

            // Run simulation
            env.Run();

            // Remove event handler (will be added again if a new simulation starts)
            eventQueue.SolutionReceived -= HandleSolution;

            // send result to optimizer when simulation has finished
            var finalSolution = WorldState.GetFinalSolution();
            var cost = WorldState.EvaluateCurrentSolution();

            Console.WriteLine(WorldState.GetFinalSolution());
            Console.WriteLine($"Final cost: {WorldState.EvaluateCurrentSolution()}");

            eventQueue.Publish(new SimulationResult(finalSolution, cost));

            //Console.ReadKey();
        }

        /// <summary>
        /// Handle a <see cref="Solution"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="solution"></param>
        private void HandleSolution(object sender, Solution solution) {
            var cost = WorldState.EvaluateSolution(solution);

            Console.WriteLine($"Received solution with cost: {cost}");
            Console.WriteLine(solution);

            if (WorldState.TrySetNewSolution(solution)) {
                // notify dispatcher
                requestPipe.Put(-1);
            }

            EnableVirtualTime();
        }

        /// <summary>
        /// Switches to virtual time if it is allowed to
        /// </summary>
        private void EnableVirtualTime() {
            if (allowFastSimulation) {
                realTimeEnforcer--;

                // If there is no unfinished problem left
                if (realTimeEnforcer <= 0) {
                    env.SetVirtualtime();
                }
            }
        }

        /// <summary>
        /// Publish a problem
        /// </summary>
        /// <param name="env"></param>
        /// <param name="problem"></param>
        private void PublishProblem(PseudoRealtimeSimulation env, Problem problem) {
            if(allowFastSimulation) {
                realTimeEnforcer++;
                env.SetRealtime();
            }
            eventQueue.Publish(problem);
        }
    }
}
