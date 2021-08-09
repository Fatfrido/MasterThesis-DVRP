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

        public OptimizerQueue(string pubConnection, string subConnection) {
            pubSocket = new PublisherSocket(pubConnection);

            subSocket = new SubscriberSocket(subConnection);
            subSocket.Subscribe(Channel.Problem);
            subSocket.Subscribe(Channel.SimulationResult);

            HandleEventIn();
        }

        public void Publish(Solution solution) {
            Console.WriteLine(">>>>>>>>>>>> solution");
            var json = JsonConvert.SerializeObject(solution);
            pubSocket.SendMoreFrame(Channel.Solution).SendFrame(json);
        }

        public void PublishStart(bool allowFastSimulation) {
            Console.WriteLine(">>>>>>>>>>>> start");
            pubSocket.SendMoreFrame(Channel.Start).SendFrame(allowFastSimulation.ToString());
        }

        private void HandleEventIn() {
            var task = new Task(() => {
                while(true) {
                    var topic = subSocket.ReceiveFrameString();
                    var message = subSocket.ReceiveFrameString();

                    switch (topic) {
                        case Channel.Problem:
                            Console.WriteLine("<<<<<<<<<<<<< problem");
                            var problem = JsonConvert.DeserializeObject<Problem>(message);
                            ProblemReceived(this, problem);
                            break;
                        case Channel.SimulationResult:
                            Console.WriteLine("<<<<<<<<<<<<< simulation result");
                            var result = JsonConvert.DeserializeObject<SimulationResult>(message);
                            ResultsReceived(this, result);
                            break;
                    }
                }
            });

            task.Start();
        }

        ~OptimizerQueue() {
            pubSocket.Dispose();
            subSocket.Dispose();
        }
    }
}
