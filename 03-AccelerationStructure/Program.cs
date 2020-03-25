﻿using System;

namespace RayTracingTutorial03
{
    class Program
    {
        private class RTXApplication : Application
        {
            public RTXApplication()
            {
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            using (var app = new RTXApplication())
            {
                app.Run();
            }
        }
    }
}