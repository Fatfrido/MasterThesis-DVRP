using DVRP.Domain;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace DynamicInstanceGenerator
{
    class Program
    {
        private static Random random = new Random();

        static void Main(string[] args) {
            var json = File.ReadAllText(args[0]);
            var problem = JsonConvert.DeserializeObject<ProblemInstance>(json);

            ModifyAndExport(problem, 0.2, 1, 360, $"{problem.Name}-low-balanced");
            ModifyAndExport(problem, 0.5, 1, 360, $"{problem.Name}-medium-balanced");
            ModifyAndExport(problem, 0.9, 1, 360, $"{problem.Name}-high-balanced");

            ModifyAndExport(problem, 0.5, 1, 21, $"{problem.Name}-medium-clustered-soon");
            ModifyAndExport(problem, 0.5, 170, 190, $"{problem.Name}-medium-clustered-medium");
            ModifyAndExport(problem, 0.5, 340, 360, $"{problem.Name}-medium-clustered-late");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="problem"></param>
        /// <param name="dod"></param>
        /// <param name="from">must be greater than 0</param>
        /// <param name="to"></param>
        /// <param name="name"></param>
        private static void ModifyAndExport(ProblemInstance problem, double dod, int from, int to, string name) {
            var dynamicRequestCount = (int) Math.Ceiling(problem.Available.Length * dod);

            var clone = problem.Clone();

            clone.Name = name;

            // https://stackoverflow.com/questions/35065764/select-n-records-at-random-from-a-set-of-n
            for (int i = 0; i < clone.Available.Length; i++) {
                var p = (double) dynamicRequestCount / (clone.Available.Length - i);

                if(random.NextDouble() <= p) {
                    // Select a random availability value between 'from' and 'to'
                    clone.Available[i] = random.Next(from, to);
                    dynamicRequestCount--;
                }
            }

            Export(clone, name);
        }

        private static void Export(ProblemInstance problem, string name) {
            File.WriteAllText($"{name}.json", JsonConvert.SerializeObject(problem));
        }
    }
}
