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
            simulation.Start();
            Thread.Sleep(10000);
        }
    }
}
