using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Text;
using System.Transactions;
using Grpc.Core;
using Timer = System.Timers.Timer;

namespace BankServer
{
    class BankServer
    {
        private static readonly Dictionary<int, string> bankServers = new();
        private static readonly List<string> boneyServers = new();
        private static readonly Dictionary<int, List<int>> notSuspected = new();
        private static readonly Dictionary<int, bool> frozenSlots = new();
        private static int totalSlots;
        private static int slotsDuration;

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine("Incorrect number of arguments, should be: [configFilePath] [processID]");
                return;
            }

            string[] commands = File.ReadAllLines(args[0]);
            int pid = int.Parse(args[1]);

            for (int i = 0; i < commands.Length; i++)
            {
                var arguments = commands[i].Split(' ');

                if (arguments[0] == "P")
                {
                    if (arguments[2] == "boney")
                    {
                        boneyServers.Add(arguments[3]);
                    }
                    else if (arguments[2] == "bank")
                    {
                        int id = int.Parse(arguments[1]);
                        bankServers.Add(id, arguments[3]);
                    }
                }
                else if (arguments[0] == "S")
                {
                    totalSlots = int.Parse(arguments[1]);
                }
                else if (arguments[0] == "D")
                {
                    slotsDuration = int.Parse(arguments[1]);
                }
                else if (arguments[0] == "T")
                {
                    continue;
                }
                else if (arguments[0] == "F")
                {
                    char[] delim = { '(', ')', ',', ' ' };
                    var argumentsF = commands[i].Split(delim);
                    int slotNumber = int.Parse(argumentsF[1]);

                    for (int j = 2; j < argumentsF.Length; j += 3)
                    {
                        int id = int.Parse(argumentsF[j]);
                        if (!bankServers.ContainsKey(id))
                        {
                            continue;
                        }
                        if (!notSuspected.ContainsKey(slotNumber))
                        {
                            notSuspected.Add(slotNumber, new());
                        }
                        if (id == pid)
                        {
                            bool isFrozen = argumentsF[j + 1] == "F";
                            frozenSlots.Add(slotNumber, isFrozen);
                            if (!isFrozen)
                            {
                                notSuspected[slotNumber].Add(pid);
                            }
                        }
                        else if (argumentsF[j + 2] == "NS")
                        {
                            notSuspected[slotNumber].Add(id);
                        }
                    }
                }
                else
                {
                    Console.Error.WriteLine("Unknown command");
                }
            }

            string address = bankServers[pid];
            Uri uri = new(address);
            Account account = new();
            BankFrontend frontend = new(boneyServers, bankServers.Values.ToList());
            BankState state = new(pid, notSuspected, frozenSlots, frontend, account);

            Server server = new()
            {
                Services = { BankClientService.BindService(new BankClientServiceImpl(state, frontend)),
                             BankTwoPhaseCommitService.BindService(new BankTwoPhaseCommitServiceImpl(state))
                           },
                Ports = { new ServerPort(uri.Host, uri.Port, ServerCredentials.Insecure) }
            };
            server.Start();

            Timer timer = new(slotsDuration);
            timer.Elapsed += (sender, e) => HandleTimer();
            timer.Start();
            state.CompareAndSwap();
            Console.WriteLine("Press any key to shutdown the server...");
            Console.ReadKey();
            frontend.CloseChannels();
            server.ShutdownAsync().Wait();
            Environment.Exit(0);

            void HandleTimer()
            {
                state.Slot += 1;
                if (totalSlots < state.Slot)
                {
                    frontend.CloseChannels();
                    server.ShutdownAsync().Wait();
                    Environment.Exit(0);
                }
                state.CompareAndSwap();
            }
        }
    }
}