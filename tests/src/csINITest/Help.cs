using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace csINITest
{
    class Help
    {
        public static void ShowError(string errorString)
        {
            Console.WriteLine("\r\n" + errorString);
            Standard();
        }

        public static void Standard()
        {
            Console.WriteLine("\r\ncsINI Test Utility");
            Console.WriteLine("Created by Kris Craig <kriscraig@php.net>");
            Console.WriteLine("------------------------------------------\r\n");
            Console.WriteLine("Usage:  csINITest.exe [OPTIONS]\r\n");
            Console.WriteLine("Options:");
            Console.WriteLine("         --help");
            Console.WriteLine("             Display this message.");
            Console.WriteLine("         --basedir <basedir>");
            Console.WriteLine("             Where to look for csLog.dll.");
            Console.WriteLine("             Default:  " + csINITest.basedir);

            Environment.Exit(0);
        }
    }
}
