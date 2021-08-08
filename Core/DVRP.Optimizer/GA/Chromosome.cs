using DVRP.Domain;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Optimizer.GA
{
    public class Chromosome : IEnumerable<int>
    {
        private static Random random = new Random();

        public int[] Data { get; set; }
        public int Length { get => Data.Length; }

        /// <summary>
        /// Initializes the chromosome with a random permutation of integers starting with 1
        /// </summary>
        /// <param name="n">Length of the permutation</param>
        public Chromosome(int n) {
            Data = Enumerable.Range(1, n).ToArray().Shuffle(); // Requests start at 1 since 0 is the depot
        }

        /// <summary>
        /// Initializes the chromosome with given data
        /// </summary>
        /// <param name="data">Data of the chromosome</param>
        public Chromosome(int[] data) {
            Data = data;
        }

        /// <summary>
        /// Inverts a random sequence of the chromosome
        /// </summary>
        public void ApplyInversionMutation() {
            // Define two cut points
            var point1 = random.Next(0, Data.Length - 1);
            var point2 = random.Next(0, Data.Length - 1);

            var from = 0;
            var to = 1;

            if(point1 < point2) {
                from = point1;
                to += point2;
            } else {
                from = point2;
                to += point1;
            }

            // Invert sequence between the two cut points

            // Create new sequence
            var newSequence = new List<int>();
            for(int i = from; i < to; i++) {
                newSequence.Prepend(Data[i]);
            }

            // Apply new sequence
            foreach(var gene in newSequence) {
                Data[from] = gene;
                from++;
            }
        }

        public Chromosome PartiallyMappedCrossover(Chromosome other) {
            var childData = other.Data.ToArray(); // Copy genes from parent 2

            var done = new Dictionary<int, int>(Length); // key: inserted value, value: index the value was inserted at

            // Select random range of genes
            var point1 = random.Next(0, Data.Length - 1);
            var point2 = random.Next(0, Data.Length - 1);

            var from = 0;
            var to = 1;

            if (point1 < point2) {
                from = point1;
                to += point2;
            } else {
                from = point2;
                to += point1;
            }

            // Copy selection directly to child
            for(int i = from; i < to; i++) {
                childData[i] = Data[i];
                done[Data[i]] = i;
            }

            // Select each value in second parent in the range and check if it is already in the child
            for(int i = from; i < to; i++) {
                if (!done.TryGetValue(other.Data[i], out var b)) { // Not part of the child => add
                    var v = Data[i];
                    var index = Array.IndexOf(other.Data, v);

                    // Find index to insert value from second parent
                    while(from <= index && index <= to) {
                        v = Data[index];
                        index = Array.IndexOf(other.Data, v);
                    }

                    // Add value
                    childData[index] = other.Data[i];
                    done[other.Data[i]] = i;
                }
            }

            return new Chromosome(childData);
        }

        public IEnumerator<int> GetEnumerator() {
            return (IEnumerator<int>) Data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return Data.GetEnumerator();
        }

        public int this[int key] {
            get => Data[key];
            set => Data[key] = value;
        }
    }
}
