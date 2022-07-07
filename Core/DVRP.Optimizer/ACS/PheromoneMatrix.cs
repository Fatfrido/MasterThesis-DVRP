using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer.ACS
{
    public class PheromoneMatrix
    {
        private double initialPheromoneValue;
        private double[,] pheromoneMatrix;
        private double evaporation;
        private double pheromoneConservation;

        public PheromoneMatrix(Problem problem, double initialPheromoneValue, double evaporation, double pheromoneConservation)
        {
            this.initialPheromoneValue = initialPheromoneValue;
            this.evaporation = evaporation;
            this.pheromoneConservation = pheromoneConservation;

            // Initialize matrix
            var length = problem.VehicleCount + problem.Requests.Length + 1; // depot
            pheromoneMatrix = new double[length, length];

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    pheromoneMatrix[i, j] = initialPheromoneValue;
                }
            }
        }

        /// <summary>
        /// Extends the original pheromone matrix if necessary
        /// </summary>
        /// <param name="problem"></param>
        public void Update(Problem problem)
        {
            var currentLength = pheromoneMatrix.GetLength(0);

            // Check if there is a new request (unknown id)
            var maxId = 0; // The highest id of all current requests

            for (int i = 0; i < problem.Requests.Length; i++)
            {
                if (problem.Requests[i].Id > maxId)
                {
                    maxId = problem.Requests[i].Id;
                }
            }

            // Transform maxId to the id it would have in the pheromone matrix
            var maxIdx = maxId + problem.VehicleCount;

            // The matrix is big enough for the highest id => no unhandled request
            if (currentLength > maxIdx)
            {
                return;
            }

            // The new length is the current length plus the difference to fit the highest id
            var newLength = currentLength + (maxIdx - (currentLength - 1));
            var matrix = new double[newLength, newLength];

            for (int i = 0; i < newLength; i++)
            {
                for (int j = 0; j < newLength; j++)
                {
                    if (i < currentLength && j < currentLength)
                    { // copy
                        matrix[i, j] = pheromoneMatrix[i, j];
                    }
                    else
                    {
                        matrix[i, j] = initialPheromoneValue;
                    }
                }
            }

            pheromoneMatrix = matrix;
        }

        /// <summary>
        /// Applies a global update that increases the pheromone value of given solution
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="problem"></param>
        public void GlobalUpdate(Solution solution, Problem problem)
        {
            for (int i = 0; i < solution.Route.Length - 1; i++)
            {
                var from = solution.Route[i] - 1; // exclude depot
                var to = solution.Route[i + 1] - 1;

                from = ToPheromoneIndex(from, problem);
                to = ToPheromoneIndex(to, problem);

                // update every edge between each node (customer)
                pheromoneMatrix[from, to] =
                    (1 - evaporation) * pheromoneMatrix[from, to] +
                    evaporation / solution.Cost;
            }
        }

        public void LocalUpdate(int from, int to, Problem problem)
        {
            var fromPheromone = ToPheromoneIndex(from, problem);
            var toPheromone = ToPheromoneIndex(to, problem);

            pheromoneMatrix[fromPheromone, toPheromone] = ((1 - evaporation) * pheromoneMatrix[fromPheromone, toPheromone]) +
                (evaporation * initialPheromoneValue);
        }

        /// <summary>
        /// Maps the index of a problem to the global pheromone matrix
        /// </summary>
        /// <param name="index"></param>
        /// <param name="problem"></param>
        /// <returns></returns>
        private int ToPheromoneIndex(int index, Problem problem)
        {
            if (index <= problem.VehicleCount)
            {
                return index;
            }
            else
            {
                // Get the id of the request at index and add vehicle count and depot to get the correct index on the pheromone matrix
                var res = problem.Mapping[index - problem.VehicleCount - 1] + problem.VehicleCount;
                return res;
            }
        }

        /// <summary>
        /// Applies the pheromone conservation strategy
        /// </summary>
        public void Conserve()
        {
            var length = pheromoneMatrix.GetLength(0);
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    pheromoneMatrix[i, j] = (1 - pheromoneConservation) * pheromoneMatrix[i, j]
                        + pheromoneConservation * initialPheromoneValue;
                }
            }
        }

        public double this[int x, int y]
        {
            get => pheromoneMatrix[x, y];
            set => pheromoneMatrix[x, y] = value;
        }
    }
}
