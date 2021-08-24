using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Domain
{
    [ProtoContract]
    public class Tour
    {
        [ProtoMember(1)]
        public List<int> Data { get; set; } = new List<int>(); // contains IDs of requests

        public Tour() { }

        public Tour(List<int> data) {
            Data = data;
        }

        /// <summary>
        /// Calculates the cost of the Tour based on a given problem
        /// </summary>
        /// <param name="problem"></param>
        /// <returns></returns>
        public double GetCost(Problem problem) {
            var sum = 0.0;
            var prevRequest = 0;

            // Create reverse mapping to map requestId to its index in the problem
            var reverseMapping = problem.CreateReverseMapping();

            foreach(var request in Data) {
                sum += problem.GetCost(prevRequest, request);
                prevRequest = request;
            }

            // Drive back to depot
            sum += problem.GetCost(prevRequest, 0);

            return sum;
        }

        public void ApplyMapping(int[] mapping) {
            Data = Data.Select(x => mapping[x]).ToList();
        }

        public override string ToString() {
            var sb = new StringBuilder();
            sb.AppendJoin('-', Data);
            return sb.ToString();
        }
    }
}
