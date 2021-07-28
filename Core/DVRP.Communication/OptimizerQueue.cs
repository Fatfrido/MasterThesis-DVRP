using NetMQ.Sockets;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DVRP.Domain;
using System.Text.Json;

namespace DVRP.Communication
{
    public class OptimizerQueue
    {
        private PublisherSocket pubSocket;
        private SubscriberSocket subSocket;

        private string publishChannel = "decision";
        private string requestScoreChannel = "score";
        private string subscribeChannel = "event";

        public event EventHandler<EventArgs> OnEvent = delegate { };

        public OptimizerQueue(string pubConnection, string subConnection) {
            pubSocket = new PublisherSocket(pubConnection);

            subSocket = new SubscriberSocket(subConnection);
            subSocket.Subscribe(subscribeChannel);
            subSocket.Subscribe(requestScoreChannel);
            HandleEventIn();
        }

        public void Publish(Solution solution) {
            Console.WriteLine(">>>>>>>>>>>>");
            var json = JsonSerializer.Serialize(solution);
            pubSocket.SendMoreFrame(publishChannel).SendFrame(json);
        }

        public void RequestScore(Solution solution) {
            var json = JsonSerializer.Serialize(solution);
            pubSocket.SendMoreFrame(requestScoreChannel).SendFrame(json);
        }

        private void HandleEventIn() {
            var task = new Task(() => {
                while(true) {
                    var topic = subSocket.ReceiveFrameString();
                    var message = subSocket.ReceiveFrameString();

                    if(OnEvent != null) {
                        Console.WriteLine("<<<<<<<<<<<<<");
                        OnEvent(this, new EventArgs(topic, message));
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
