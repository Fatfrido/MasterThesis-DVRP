using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Domain
{
    public class History
    {
        private List<Request>[] data;

        // key: request id, value: served by vehicle
        private Dictionary<int, int> served;
        private int vehicleCount;

        public int Count { get; private set; }

        public History(int vehicleCount) {
            data = new List<Request>[vehicleCount];

            for(int i = 0; i < data.Length; i++) {
                data[i] = new List<Request>();
            }

            served = new Dictionary<int, int>();
            this.vehicleCount = vehicleCount;
        }

        public void Add(int vehicle, Request request) {
            data[vehicle].Add(request);
            served.Add(request.Id, vehicle);
            Count++;
        }

        /// <summary>
        /// Returns true if given request has already been served, false otherwise
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public bool Contains(int requestId) {
            return served.ContainsKey(requestId);
        }

        /// <summary>
        /// Returns the request with given id
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        public Request GetRequest(int requestId) {
            var vehicle = served[requestId];
            return data[vehicle].Where(x => x.Id == requestId).First();
        }

        public Solution ToSolution() {
            var solution = new Solution(vehicleCount);

            for (int i = 0; i < vehicleCount; i++) {
                solution.AddRoute(i, data[i].Select(x => x.Id).ToArray());
            }

            return solution;
        }

        public List<Request> this[int key] {
            get => data[key];
        }
    }
}
