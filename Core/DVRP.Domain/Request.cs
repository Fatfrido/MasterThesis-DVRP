using ProtoBuf;
using System;

namespace DVRP.Domain
{
    [ProtoContract]
    public class Request
    {
        public int X { get; set; }

        public int Y { get; set; }

        [ProtoMember(1)]
        public int Amount { get; set; }

        [ProtoMember(2)]
        public int Id { get; set; }

        public int Vehicle { get; set; }

        public Request(int x, int y, int amount, int id) {
            X = x;
            Y = y;
            Amount = amount;
            Id = id;
        }

        public Request() {}

        public double Distance(Request request) {
            var sqr1 = (X - request.X) * (X - request.X);
            var sqr2 = (Y - request.Y) * (Y - request.Y);
            return Math.Sqrt(sqr1 + sqr2);
        }
    }
}
