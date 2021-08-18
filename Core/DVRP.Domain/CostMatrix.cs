using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Domain
{
    [ProtoContract]
    public class CostMatrix
    {
        [ProtoMember(1)]
        public long[] Data { get; set; }

        [ProtoMember(2)]
        public int Dimension { get; private set; }

        public CostMatrix() { }
        
        public CostMatrix(long[] data) {
            Data = data;
        }

        public CostMatrix(long[,] data) {
            Data = new long[data.Length];
            Dimension = data.GetLength(0);

            var index = 0;
            for (var i = 0; i < Dimension; i++) {
                for(var j = 0; j < Dimension; j++) {
                    Data[index] = data[i, j];
                    index++;
                }
            }
        }

        public long this[int row, int col] {
            get => Data[row * Dimension + col];
            set => Data[row * Dimension + col] = value;
        }
    }
}
