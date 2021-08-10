﻿using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Simulaton
{
    public class ProblemInstance
    {
        /// <summary>
        /// Vehicle types available in this problem instance
        /// </summary>
        public VehicleType[] VehicleTypes { get; set; }

        /// <summary>
        /// X coordinate of all locations including depot (at 0)
        /// </summary>
        public int[] XLocations { get; set; }

        /// <summary>
        /// Y coordinate of all locations including depot (at 0)
        /// </summary>
        public int[] YLocations { get; set; }

        /// <summary>
        /// Demand for every request
        /// </summary>
        public int[] Demands { get; set; }

        /// <summary>
        /// Time in seconds after which the request is available. 0 means it is available right at the beginning
        /// </summary>
        public int[] Available { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initialRequests"></param>
        /// <param name="dynamicRequests"></param>
        public void GetRequests(out Request[] initialRequests, out Dictionary<int, Request> dynamicRequests) {
            var initialRequestsList = new List<Request>();
            dynamicRequests = new Dictionary<int, Request>();

            for(int i = 0; i < Available.Length; i++) {
                var request = new Request(
                    XLocations[i + 1],
                    YLocations[i + 1],
                    Demands[i],
                    i + 1
                    );

                if (Available[i] < 1) { // available at the beginning
                    initialRequestsList.Add(request);
                } else { // appears dynamically
                    dynamicRequests.Add(Available[i], request);
                }
            }

            initialRequests = initialRequestsList.ToArray();
        }
    }
}