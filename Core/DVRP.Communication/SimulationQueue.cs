using DVRP.Domain;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace DVRP.Communication
{
    public class SimulationQueue
    {
        private PublisherSocket pubSocket;
        private SubscriberSocket subSocket;

        private string publishChannel = "event";
        private string subscribeChannel = "decision";
        private string scoreChannel = "score";

        public event EventHandler<EventArgs> OnEvent = delegate { };

        public SimulationQueue(string pubConnection, string subConnection) {
            pubSocket = new PublisherSocket(pubConnection);

            subSocket = new SubscriberSocket(subConnection);
            subSocket.Subscribe(subscribeChannel);
            subSocket.Subscribe(scoreChannel);
            HandleEventIn();
        }

        public void Publish(Problem problem) {
            Console.WriteLine(">>>>>>>>>");
            var json = JsonConvert.SerializeObject(problem);
            pubSocket.SendMoreFrame(publishChannel).SendFrame(json);
        }

        public void Publish(double score) {
            pubSocket.SendMoreFrame(scoreChannel).SendFrame(score.ToString());
        }

        private void HandleEventIn() {
            var task = new Task(() => {
                while(true) {
                    var topic = subSocket.ReceiveFrameString();
                    var message = subSocket.ReceiveFrameString();

                    if(OnEvent != null) {
                        Console.WriteLine("<<<<<<<<<");
                        OnEvent(this, new EventArgs(topic, message));
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
