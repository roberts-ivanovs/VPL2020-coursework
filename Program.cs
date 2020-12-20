using System;
using DiseaseCore;

namespace coursework
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            World simulation = new World(95, 5, 1.0f);
            Console.WriteLine("Starting!");
            simulation.Start();
            var res = simulation.GetCurrentState();
            Console.WriteLine($"Result: {res.Count}");
            Console.WriteLine("Stopping!");
            simulation.Stop();
        }
    }
}
