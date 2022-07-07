using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Domain
{
    [ProtoContract]
    public class Matrix<T>
    {
        [ProtoMember(1)]
        public T[] Data { get; set; }

        [ProtoMember(2)]
        public int Dimension { get; private set; }

        public Matrix() { }

        public Matrix(T[] data, int dimension)
        {
            Data = data;
            Dimension = dimension;
        }

        public Matrix(T[,] data)
        {
            Data = new T[data.Length];
            Dimension = data.GetLength(0);

            var index = 0;
            for (var i = 0; i < Dimension; i++)
            {
                for (var j = 0; j < Dimension; j++)
                {
                    Data[index] = data[i, j];
                    index++;
                }
            }
        }

        public T this[int row, int col]
        {
            get => Data[row * Dimension + col];
            set => Data[row * Dimension + col] = value;
        }
    }
}
