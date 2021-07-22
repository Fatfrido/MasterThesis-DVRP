using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Communication
{
    public class EventArgs
    {
        public string Topic { get; set; }
        public string Message { get; set; }

        public EventArgs() { }
        public EventArgs(string topic, string message) {
            Topic = topic;
            Message = message;
        }
    }
}
