using DVRP.Communication;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DVRP.Simulaton
{
    class Program
    {
        private static DVRP sim;

        static void Main(string[] args) {
            Console.WriteLine("Initializing simulation...");

            // Read appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();

            var section = config.GetSection(nameof(SimulationConfig));
            var simulationConfig = section.Get<SimulationConfig>();

            ISimulationQueue queue = 
                new SimulationQueue(simulationConfig.PublishConnectionString, simulationConfig.SubscribeConnectionString);

            queue.StartSimulationReceived += (sender, allowFastSimulation) => {
                new Task(() => sim.Simulate(allowFastSimulation)).Start();
            };

            // Read problem instance
            var file = File.ReadAllText($"./{simulationConfig.ProblemInstanceName}.json");
            var dvrp = JsonConvert.DeserializeObject<ProblemInstance>(file);

            sim = new DVRP(queue, dvrp);

            Console.WriteLine("Simulation is ready");

            while (true) {
                Thread.Sleep(500);
            }
        }
    }
}
