using DVRP.Domain;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Domain
{
    [ProtoContract]
    public class ProblemInstance
    {
        /// <summary>
        /// Capacity of each vehicle type
        /// </summary>
        [ProtoMember(1)]
        public int[] VehicleTypeCapacity { get; set; }

        /// <summary>
        /// Count of available vehicles of each type
        /// </summary>
        [ProtoMember(2)]
        public int[] VehicleTypeCount { get; set; }

        /// <summary>
        /// X coordinate of all locations including depot (at 0)
        /// </summary>
        [ProtoMember(3)]
        public int[] XLocations { get; set; }

        /// <summary>
        /// Y coordinate of all locations including depot (at 0)
        /// </summary>
        [ProtoMember(4)]
        public int[] YLocations { get; set; }

        /// <summary>
        /// Demand for every request
        /// </summary>
        [ProtoMember(5)]
        public int[] Demands { get; set; }

        /// <summary>
        /// Time in seconds after which the request is available. 0 means it is available right at the beginning
        /// </summary>
        [ProtoMember(6)]
        public int[] Available { get; set; }

        /// <summary>
        /// Time needed to service a customer
        /// </summary>
        [ProtoMember(7)]
        public double ServiceTime { get; set; }

        /// <summary>
        /// Seconds per distance unit
        /// </summary>
        [ProtoMember(8)]
        public double Speed { get; set; }

        /// <summary>
        /// Name of the instance
        /// </summary>
        [ProtoMember(9)]
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initialRequests"></param>
        /// <param name="dynamicRequests"></param>
        public void GetRequests(out Request[] initialRequests, out DynamicRequestStore dynamicRequests)
        {
            var initialRequestsList = new List<Request>();
            dynamicRequests = new DynamicRequestStore();

            var id = 1;
            for (int i = 0; i < Available.Length; i++)
            {
                var request = new Request(
                    XLocations[i + 1],
                    YLocations[i + 1],
                    Demands[i],
                    -1 // the ids must be ordered by release time of a request
                    );

                if (Available[i] < 1)
                { // available at the beginning
                    request.Id = id;
                    initialRequestsList.Add(request);
                    id++;
                }
                else
                { // appears dynamically
                    // ids of dynamic requests will be set later
                    dynamicRequests.Add(Available[i], request);
                }
            }

            // Sort dynamic requests by the time they will be available
            dynamicRequests.UpdateIds(id);

            initialRequests = initialRequestsList.ToArray();
        }

        /// <summary>
        /// Returns the <see cref="VehicleType"/>s available in this problem
        /// </summary>
        /// <returns></returns>
        public VehicleType[] GetVehicleTypes()
        {
            var vehicleTypes = new VehicleType[VehicleTypeCapacity.Length];

            for (int i = 0; i < VehicleTypeCapacity.Length; i++)
            {
                vehicleTypes[i] = new VehicleType(VehicleTypeCapacity[i], VehicleTypeCount[i]);
            }

            return vehicleTypes;
        }

        /// <summary>
        /// Deep clones the problem instance
        /// </summary>
        /// <returns></returns>
        public ProblemInstance Clone()
        {
            var clone = new ProblemInstance();

            // Deep clone
            clone.Available = Available.ToArray();
            clone.Demands = Demands.ToArray();
            clone.ServiceTime = ServiceTime;
            clone.Speed = Speed;
            clone.VehicleTypeCapacity = VehicleTypeCapacity.ToArray();
            clone.VehicleTypeCount = VehicleTypeCount.ToArray();
            clone.XLocations = XLocations.ToArray();
            clone.YLocations = YLocations.ToArray();
            clone.Name = new string(Name);

            return clone;
        }
    }
}
