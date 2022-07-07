using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Domain
{
    public class VehicleType
    {
        /// <summary>
        /// Capacity of vehicles of this type
        /// </summary>
        public int Capacity { get; set; }

        /// <summary>
        /// Number of vehicles that are of this type
        /// </summary>
        public int VehicleCount { get; set; }

        public VehicleType() { }

        public VehicleType(int capacity, int vehicleCount)
        {
            Capacity = capacity;
            VehicleCount = vehicleCount;
        }
    }
}
