﻿using System;
using DiseaseCore;

namespace coursework
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            World simulation = new World(95, 5, 1.0f, false);
            Console.WriteLine("Starting!");
            simulation.Start();
            var res = simulation.GetCurrentState();
            Console.WriteLine($"Result: {res}");
            Console.WriteLine("Read again!");
            res = simulation.GetCurrentState();
            Console.WriteLine($"Result: {res}");
            Console.WriteLine("Read again!");
            res = simulation.GetCurrentState();
            Console.WriteLine($"Result: {res}");
            Console.WriteLine("Read again!");
            res = simulation.GetCurrentState();
            Console.WriteLine($"Result: {res}");

            Console.WriteLine("Stopping!");

            simulation.Stop();
        }
    }
}
