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

        /// <summary>
        /// Emitted C02 per distance unit
        /// </summary>
        public int Emissions { get; set; }

        public VehicleType() { }

        public VehicleType(int capacity, int vehicleCount, int emissions) {
            Capacity = capacity;
            VehicleCount = vehicleCount;
            Emissions = emissions;
        }
    }
}
