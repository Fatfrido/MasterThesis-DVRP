using NetMQ.Sockets;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DVRP.Domain;
using System.Text.Json;
using Newtonsoft.Json;

namespace DVRP.Communication
{
    public class OptimizerQueue : IOptimizerQueue
    {
        private PublisherSocket pubSocket;
        private SubscriberSocket subSocket;

        public event EventHandler<Problem> ProblemReceived = delegate { };
        public event EventHandler<SimulationResult> ResultsReceived = delegate { };

        public OptimizerQueue(string pubConnection, string subConnection)
        {
            pubSocket = new PublisherSocket(pubConnection);

            subSocket = new SubscriberSocket(subConnection);
            subSocket.Subscribe(Channel.Problem.ToString());
            subSocket.Subscribe(Channel.SimulationResult.ToString());

            HandleEventIn();
        }

        public void Publish(Solution solution)
        {
            //Console.WriteLine(">>>>>>>>>>>> solution");
            pubSocket.SendMoreFrame(Channel.Solution.ToString()).SendFrame(solution.Serialize());
        }

        public void PublishStart(StartSimulationMessage message)
        {
            //Console.WriteLine(">>>>>>>>>>>> start");
            pubSocket.SendMoreFrame(Channel.Start.ToString()).SendFrame(message.Serialize());
        }

        private void HandleEventIn()
        {
            var task = new Task(() =>
            {
                while (true)
                {
                    var topicStr = subSocket.ReceiveFrameString();
                    var topic = Enum.Parse(typeof(Channel), topicStr);
                    var message = subSocket.ReceiveFrameBytes();

                    switch (topic)
                    {
                        case Channel.Problem:
                            //Console.WriteLine("<<<<<<<<<<<<< problem");
                            ProblemReceived(this, message.Deserialize<Problem>());
                            break;
                        case Channel.SimulationResult:
                            Console.WriteLine("<<<<<<<<<<<<< simulation result");
                            ResultsReceived(this, message.Deserialize<SimulationResult>());
                            break;
                    }
                }
            });

            task.Start();
        }

        ~OptimizerQueue()
        {
            pubSocket.Dispose();
            subSocket.Dispose();
        }
    }
}
