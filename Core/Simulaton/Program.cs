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
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing simulation...");

            // Read appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();

            var section = config.GetSection(nameof(SimulationConfig));
            var simulationConfig = section.Get<SimulationConfig>();

            ISimulationQueue queue =
                new SimulationQueue(simulationConfig.PublishConnectionString, simulationConfig.SubscribeConnectionString);

            var sim = new DVRP(queue);

            queue.StartSimulationReceived += (sender, startSimulationMessage) =>
            {
                new Task(() => sim.Simulate(startSimulationMessage)).Start();
            };

            Console.WriteLine("Simulation is ready");

            while (true)
            {
                Thread.Sleep(500);
            }
        }
    }
}
