using DVRP.Domain;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace DVRP.Communication
{
    public class SimulationQueue : ISimulationQueue
    {
        private PublisherSocket pubSocket;
        private SubscriberSocket subSocket;

        public event EventHandler<bool> StartSimulationReceived = delegate { };
        public event EventHandler<Solution> SolutionReceived = delegate { };

        public SimulationQueue(string pubConnection, string subConnection) {
            pubSocket = new PublisherSocket(pubConnection);

            subSocket = new SubscriberSocket(subConnection);
            subSocket.Subscribe(Channel.Solution);
            subSocket.Subscribe(Channel.Start);

            HandleEventIn();
        }

        public void Publish(Problem problem) {
            Console.WriteLine(">>>>>>>>> problem");
            var json = JsonConvert.SerializeObject(problem);
            pubSocket.SendMoreFrame(Channel.Problem).SendFrame(json);
        }

        public void Publish(SimulationResult result) {
            Console.WriteLine(">>>>>>>>> result");
            var json = JsonConvert.SerializeObject(result);
            pubSocket.SendMoreFrame(Channel.SimulationResult).SendFrame(json);
        }

        /// <summary>
        /// Handles incoming events on the queue
        /// </summary>
        private void HandleEventIn() {
            var task = new Task(() => {
                while(true) {
                    var topic = subSocket.ReceiveFrameString();
                    var message = subSocket.ReceiveFrameString();
                    Console.WriteLine($"received message: {topic}");

                    switch (topic) {
                        case Channel.Solution:
                            Console.WriteLine("<<<<<<<<<< solution");
                            var solution = JsonConvert.DeserializeObject<Solution>(message);
                            SolutionReceived(this, solution);
                            break;
                        case Channel.Start:
                            Console.WriteLine("<<<<<<<<<< start");
                            if (bool.TryParse(message, out var allowFastSimulation)) {
                                StartSimulationReceived(this, allowFastSimulation);
                            } else {
                                throw new ArgumentException($"'{message}' cannot be converted to bool");
                            }
                            break;
                    }
                }
            });

            task.Start();
        }

        ~SimulationQueue() {
            pubSocket.Dispose();
            subSocket.Dispose();
        }
    }
}
