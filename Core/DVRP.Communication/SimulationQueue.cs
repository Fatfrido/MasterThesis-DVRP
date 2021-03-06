using DVRP.Domain;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Threading.Tasks;

namespace DVRP.Communication
{
    public class SimulationQueue : ISimulationQueue
    {
        private PublisherSocket pubSocket;
        private SubscriberSocket subSocket;

        public event EventHandler<StartSimulationMessage> StartSimulationReceived = delegate { };
        public event EventHandler<Solution> SolutionReceived = delegate { };

        public SimulationQueue(string pubConnection, string subConnection)
        {
            pubSocket = new PublisherSocket(pubConnection);

            subSocket = new SubscriberSocket(subConnection);
            subSocket.Subscribe(Channel.Solution.ToString());
            subSocket.Subscribe(Channel.Start.ToString());

            HandleEventIn();
        }

        public void Publish(Problem problem)
        {
            Console.WriteLine(">>>>>>>>> problem");
            pubSocket.SendMoreFrame(Channel.Problem.ToString()).SendFrame(problem.Serialize());
        }

        public void Publish(SimulationResult result)
        {
            Console.WriteLine(">>>>>>>>> result");
            pubSocket.SendMoreFrame(Channel.SimulationResult.ToString()).SendFrame(result.Serialize());
        }

        /// <summary>
        /// Handles incoming events on the queue
        /// </summary>
        private void HandleEventIn()
        {
            var task = new Task(() =>
            {
                while (true)
                {
                    var topicStr = subSocket.ReceiveFrameString();
                    var topic = Enum.Parse(typeof(Channel), topicStr);
                    var message = subSocket.ReceiveFrameBytes();
                    Console.WriteLine($"received message: {topic}");

                    switch (topic)
                    {
                        case Channel.Solution:
                            Console.WriteLine("<<<<<<<<<< solution");
                            SolutionReceived(this, message.Deserialize<Solution>());
                            break;
                        case Channel.Start:
                            Console.WriteLine("<<<<<<<<<< start");
                            StartSimulationReceived(this, message.Deserialize<StartSimulationMessage>());
                            break;
                    }
                }
            });

            task.Start();
        }

        ~SimulationQueue()
        {
            pubSocket.Dispose();
            subSocket.Dispose();
        }
    }
}
