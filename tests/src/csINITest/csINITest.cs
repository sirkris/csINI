using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace csINITest
{
    /*
     * Logging Note:
     * 
     * To test integration with the optional csLog library, simply place a copy of csLog.dll 
     * into the same directory as the test executable.  If a logs subdirectory appears after 
     * execution, it worked.
     * 
     * --Kris
     */
    class csINITest
    {
        public static string basedir = Environment.CurrentDirectory + @"\..\";

        protected static Assembly csINI;
        protected static Type csINIType;
        protected static object csINIInstance;

        static void Main(string[] args)
        {
            parseArgs(args);
            runTests();
        }

        static void parseArgs(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg.IndexOf("--") == 0)
                {
                    switch (arg.Substring(2).ToLower())
                    {
                        default:
                            Help.ShowError("Unrecognized switch '" + arg + "'!");
                            break;
                        case "help":
                            Help.Standard();
                            break;
                        case "basedir":
                            if (i >= args.Length - 1)
                            {
                                Help.ShowError("Syntax error:  Argument required for '" + arg + "'!");
                            }
                            else
                            {
                                i++;
                                basedir = args[i];
                            }
                            break;
                    }
                }
                else
                {
                    switch (arg.Trim().ToLower())
                    {
                        default:
                            Help.ShowError("Unrecognized argument '" + arg + "'!");
                            break;
                        case "":
                            break;
                    }
                }
            }
        }

        /* If you want to make a dependency optional, you can use a try/catch block.  --Kris */
        static void runTests()
        {
            /* The properties we'll be attempting to retrieve and display.  --Kris */
            Dictionary<string, Dictionary<string, string>> fields = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, string> fStr;

            fields.Add("string", new Dictionary<string, string>());
            fields["string"].Add("LibName", null);
            fields["string"].Add("Version", null);
            fields["string"].Add("Author", null);
            fields["string"].Add("Email", null);
            fields["string"].Add("Repo", null);

            // Repeat the same process for int and other types as-needed.  --Kris

            /* Load the DLL.  --Kris */
            csINI = Assembly.LoadFile(basedir + @"\csINI.dll");

            /* Retrieve the "INI" class definition.  --Kris */
            csINIType = csINI.GetType("csINI.INI");

            /* Instantiate the "INI" class.  --Kris */
            csINIInstance = Activator.CreateInstance(csINIType);

            /* Retrieve and display the string fields.  --Kris */
            fStr = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> field in fields["string"])
            {
                fStr.Add(field.Key, field.Value);  // Simply setting fStr to fields["string"] acts as a pointer for some reason and disrupts the foreach.  --Kris
            }

            foreach (KeyValuePair<string, string> field in fStr)
            {
                FieldInfo fieldInfo = csINIType.GetField(field.Key);
                fields["string"][field.Key] = (string)fieldInfo.GetValue(null);

                Console.WriteLine(field.Key + @":  " + fields["string"][field.Key]);
            }

            // Other types (int, etc) go here.
            // TODO - Streamline all types into a single loop?  The requisite casting might make it a bit tricky.

            /* Display all available methods.  --Kris */
            MethodInfo[] methodInfos = csINIType.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

            Console.WriteLine("\r\nAvailable Methods:\r\n");
            for (int i = 0; i < methodInfos.Count(); i++)
            {
                Console.WriteLine(methodInfos[i].ToString());
            }

            /*
             * Tests specific to INI functionality.  --Kris
             */
            Console.WriteLine("\r\nTesting INI Functions:\r\n");

            /* Create a new INI file.  --Kris */
            Dictionary<string, Dictionary<string, string>> newdirectives = new Dictionary<string, Dictionary<string, string>>();
            newdirectives.Add("Main Section", new Dictionary<string, string>());
            newdirectives["Main Section"].Add("PrimeDirective", "Make it so.");
            newdirectives["Main Section"].Add("SecondaryDirective", "Q!");
            newdirectives.Add("Another Section", new Dictionary<string, string>());
            newdirectives["Another Section"].Add("What does that mean?", "That boy needs therapy.");

            csINIType.InvokeMember("Create", BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public, null, csINIInstance,
                new object[] { Environment.CurrentDirectory + @"\test_gen.ini", "csINITest", "Auto-Generated INI File", newdirectives });

            /* Read from the different INI files.  --Kris */
            Dictionary<string, string> noheadersdirectives = new Dictionary<string, string>();
            Dictionary<string, Dictionary<string, string>> headerdirectives = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, Dictionary<string, string>> newdirectivesloaded = new Dictionary<string, Dictionary<string, string>>();

            noheadersdirectives = (Dictionary<string, string>)csINIType.InvokeMember("Load", 
                BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public | BindingFlags.OptionalParamBinding, null, csINIInstance,
                new object[] { Environment.CurrentDirectory + @"\test_noheaders.ini" });

            headerdirectives = (Dictionary<string, Dictionary<string, string>>)csINIType.InvokeMember("LoadWithHeaders",
                BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public | BindingFlags.OptionalParamBinding, null, csINIInstance,
                new object[] { Environment.CurrentDirectory + @"\test_headers.ini" });

            newdirectivesloaded = (Dictionary<string, Dictionary<string, string>>)csINIType.InvokeMember("LoadWithHeaders",
                BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public | BindingFlags.OptionalParamBinding, null, csINIInstance,
                new object[] { Environment.CurrentDirectory + @"\test_gen.ini" });

            /* Output the data from each set of directives.  --Kris */
            Console.WriteLine("\r\nFunctions test complete.  Displaying results:\r\n");

            Console.WriteLine(Environment.CurrentDirectory + @"\test_gen.ini:");
            foreach (KeyValuePair<string, Dictionary<string, string>> section in newdirectivesloaded)
            {
                Console.WriteLine(@"   [" + section.Key + @"]");

                if (newdirectives.ContainsKey(section.Key) == false)
                {
                    Console.WriteLine(@"   WARNING:  Loaded INI section not found in original autogen seed!");
                }

                foreach (KeyValuePair<string, string> directive in section.Value)
                {
                    Console.WriteLine(@"      " + directive.Key + @" = " + directive.Value);

                    if (newdirectives.ContainsKey(section.Key) == false
                        || newdirectives[section.Key].ContainsKey(directive.Key) == false
                        || newdirectivesloaded[section.Key][directive.Key] != newdirectives[section.Key][directive.Key])
                    {
                        Console.WriteLine(@"      WARNING:  Loaded INI directive not found in original autogen seed!");
                    }
                }
            }

            Console.WriteLine(Environment.CurrentDirectory + @"\test_headers.ini:");
            foreach (KeyValuePair<string, Dictionary<string, string>> section in headerdirectives)
            {
                Console.WriteLine(@"   [" + section.Key + @"]");

                foreach (KeyValuePair<string, string> directive in section.Value)
                {
                    Console.WriteLine(@"      " + directive.Key + @" = " + directive.Value);
                }
            }

            Console.WriteLine(Environment.CurrentDirectory + @"\test_noheaders.ini:");
            foreach (KeyValuePair<string, string> directive in noheadersdirectives)
            {
                Console.WriteLine(@"   " + directive.Key + @" = " + directive.Value);
            }

            Console.WriteLine("\r\nAll tests completed successfully!");
        }
    }
}
