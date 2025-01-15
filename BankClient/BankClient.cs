using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankClient
{
    class BankClient
    {
        private static int pid;
        private static int seqNumber = 1;
        private static List<string> bankServers = new();
        private static string script;

        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Incorrect number of arguments, should be: [configFilePath] [processID]");
                return;
            }

            string[] commands = File.ReadAllLines(args[0]);
            pid = int.Parse(args[1]);

            for (int i = 0; i < commands.Length; i++)
            {
                var arguments = commands[i].Split(' ');

                if (arguments[0] == "P")
                {
                    if (arguments[2] == "client")
                    {
                        int id = int.Parse(arguments[1]);
                        if (id == pid)
                        {
                            script = arguments[3];
                        }
                    }
                    else if (arguments[2] == "bank")
                    {
                        bankServers.Add(arguments[3]);
                    }
                }
            }

            ClientFrontend frontend = new(bankServers);

            Thread thread = new(() =>
            {
                Console.WriteLine("Press any key to stop the client...");
                Console.ReadKey();
                frontend.CloseChannels();
                Environment.Exit(0);
            });
            thread.Start();

            commands = File.ReadAllLines(script);
            int ix = 0;

            while (true)
            {
                var arguments = commands[ix].Split(' ');

                if (arguments[0] == "D")
                {
                    if (arguments.Length != 2)
                    {
                        Console.WriteLine($"Line {ix + 1}: wrong number of arguments.");
                        continue;
                    }
                    double amount = double.Parse(arguments[1], CultureInfo.InvariantCulture);
                    if (amount <= 0)
                    {
                        Console.WriteLine($"Line {ix + 1}: deposit amount must be more than 0.");
                        continue;
                    }
                    frontend.Deposit(pid, seqNumber, amount);
                    seqNumber++;
                }
                else if (arguments[0] == "W")
                {
                    if (arguments.Length != 2)
                    {
                        Console.WriteLine($"Line {ix + 1}: wrong number of arguments.");
                        continue;
                    }
                    double amount = double.Parse(arguments[1], CultureInfo.InvariantCulture);
                    if (amount <= 0)
                    {
                        Console.WriteLine($"Line {ix + 1}: withdrawal amount must be more than 0.");
                        continue;
                    }
                    frontend.Withdraw(pid, seqNumber, amount);
                    seqNumber++;
                }
                else if (arguments[0] == "R")
                {
                    if (arguments.Length != 1)
                    {
                        Console.WriteLine($"Line {ix + 1}: wrong number of arguments.");
                        continue;
                    }
                    frontend.ReadBalance(pid, seqNumber);
                    seqNumber++;
                }
                else if (arguments[0] == "S")
                {
                    if (arguments.Length != 2)
                    {
                        Console.WriteLine($"Line {ix + 1}: wrong number of arguments.");
                        continue;
                    }
                    int time = int.Parse(arguments[1]);
                    if (time < 0)
                    {
                        Console.WriteLine($"Line {ix + 1}: wait time must be more than or equal to 0.");
                        continue;
                    }
                    Thread.Sleep(time);
                }
                else
                {
                    Console.WriteLine($"Line {ix + 1}: unknown command.");
                }
                ix++;
                if (ix == commands.Length)
                {
                    ix = 0;
                }
            }
        }
    }
}
