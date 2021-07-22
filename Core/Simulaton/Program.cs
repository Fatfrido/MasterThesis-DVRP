using System;

namespace DVRP.Simulaton
{
    class Program
    {
        static void Main(string[] args) {
            Console.WriteLine("==| Starting simulation |==");
            var sim = new DVRP();
            sim.Simulate(pubConnectionStr: "tcp://*:12345", subConnectionString: "tcp://localhost:12346");
        }
    }
}
