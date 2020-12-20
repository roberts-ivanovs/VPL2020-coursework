using System;
using System.Threading;
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
            Thread.Sleep(1000);
            Console.WriteLine("Stopping!");
            simulation.Stop();
            Thread.Sleep(1000);
        }
    }
}
