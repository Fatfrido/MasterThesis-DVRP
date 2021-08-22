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
        public List<int> Data { get; set; } = new List<int>();

        public Tour() { }

        public Tour(List<int> data) {
            Data = data;
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
