using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Domain
{
    [ProtoContract]
    public class Solution
    {
        [ProtoMember(1)]
        public Tour[] Data { get; set; }

        public Solution() { }

        public Solution(int vehicleNumber) {
            Data = new Tour[vehicleNumber];
            for(int i = 0; i < vehicleNumber; i++) {
                Data[i] = new Tour();
            }
        }

        public void AddRoute(int vehicle, int[] route) {
            Data[vehicle] = new Tour(route.ToList()); // TODO use list as param??
        }

        public int[] GetRoute(int vehicle) {
            return Data[vehicle].Data.ToArray();
        }

        public void ApplyMapping(int[] mapping) {
            for(var i = 0; i < Data.Length; i++) {
                Data[i].ApplyMapping(mapping);
            }
        }

        public double Evaluate(Problem problem) {
            var sum = 0.0;

            foreach(var tour in Data) {
                sum += tour.GetCost(problem);
            }

            return sum;
        }

        public override string ToString() {
            var sb = new StringBuilder();

            sb.Append(">>> SOLUTION").AppendLine();

            for(int i = 0; i < Data.GetLength(0); i++) { // vehicles
                if(Data[i] != null) {
                    sb.Append($"[vehicle {i}] ")
                        .Append(Data[i])
                        .AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
