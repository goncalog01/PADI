using System.Diagnostics;

namespace PuppetMaster
{
    class PuppetMaster
    {
        private static readonly List<Process> processes = new();
        private static readonly List<int> processIds = new();

        public static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Wrong number of arguments: [configFile] [boneyExePath] [bankExePath] [clientExePath].");
                Environment.Exit(0);
            }

            ParseConfigFile(args[0], args[1], args[2], args[3]);
            StartProcesses();
            CloseProcesses();
        }

        private static void ParseConfigFile(string configFile, string boneyExePath, string bankExePath, string clientExePath)
        {
            string[] commands = File.ReadAllLines(configFile);

            for (int i = 0; i < commands.Length; i++)
            {
                var arguments = commands[i].Split(' ');

                if (arguments[0] == "P")
                {
                    if (arguments.Length != 4)
                    {
                        Console.WriteLine($"Line {i + 1}: wrong number of arguments.");
                        continue;
                    }
                    int id = int.Parse(arguments[1]);
                    if (processIds.Contains(id))
                    {
                        Console.WriteLine($"Line {i + 1}: there is already a process with the same id.");
                        continue;
                    }
                    else
                    {
                        processIds.Add(id);
                    }
                    if (arguments[2] == "boney")
                    {
                        Process p = new();
                        p.StartInfo.FileName = boneyExePath;
                        p.StartInfo.ArgumentList.Add(configFile);
                        p.StartInfo.ArgumentList.Add(id.ToString());
                        processes.Add(p);
                    }
                    else if (arguments[2] == "bank")
                    {
                        Process p = new();
                        p.StartInfo.FileName = bankExePath;
                        p.StartInfo.ArgumentList.Add(configFile);
                        p.StartInfo.ArgumentList.Add(id.ToString());
                        processes.Add(p);
                    }
                    else if (arguments[2] == "client")
                    {
                        Process p = new();
                        p.StartInfo.FileName = clientExePath;
                        p.StartInfo.ArgumentList.Add(configFile);
                        p.StartInfo.ArgumentList.Add(id.ToString());
                        processes.Add(p);
                    }
                }
                else if (arguments[0] == "S" || arguments[0] == "T" || arguments[0] == "D")
                {
                    if (arguments.Length != 2)
                    {
                        Console.WriteLine($"Line {i + 1}: wrong number of arguments.");
                        Environment.Exit(0);
                    }
                }
                else
                {
                    Console.WriteLine($"Line {i + 1}: unknown command.");
                }
            }
        }

        public static void StartProcesses()
        {
            foreach (Process p in processes)
            {
                p.Start();
            }
        }

        public static void CloseProcesses()
        {
            Console.WriteLine("Press any key to close all processes...");
            Console.ReadKey();

            foreach (Process p in processes)
            {
                p.CloseMainWindow();
                p.Close();
            }
        }
    }
}