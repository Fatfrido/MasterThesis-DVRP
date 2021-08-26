using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Domain
{
    /// <summary>
    /// Makes dynamic requests easier to use
    /// </summary>
    public class DynamicRequestStore
    {
        private SortedDictionary<int, List<Request>> data = new SortedDictionary<int, List<Request>>();

        /// <summary>
        /// Returns all requests for a certain timestamp
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Tuple<int, IEnumerable<Request>>> GetRequests() {
            foreach(var entry in data) {
                yield return new Tuple<int, IEnumerable<Request>>(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Adds a new request to a given timestamp
        /// </summary>
        /// <param name="time"></param>
        /// <param name="request"></param>
        public void Add(int time, Request request) {
            if(!data.ContainsKey(time)) {
                data.Add(time, new List<Request>());
            }

            data[time].Add(request);
        }

        /// <summary>
        /// Applies ids to the requests
        /// </summary>
        /// <param name="startId">The id of the first dynamic request</param>
        public void UpdateIds(int startId) {
            var id = startId;

            foreach(var entry in data) {
                foreach(var request in entry.Value) {
                    request.Id = id;
                    id++;
                }
            }
        }
    }
}
