using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Transactions;
using Grpc.Core;

namespace Boney
{
    class Boney
    {
        public static void Main(string[] args)
        {
            //QUICK ARGS CHECK
            if (args.Length != 2)
            {
                Console.WriteLine("Wrong number of arguments, should start with two args. \n -> path to config file. \n->id between 1 and 3.");
                Environment.Exit(1);
            }
            int id = int.Parse(args[1]);
            if (id > 3 && id < 1)
            {
                Console.WriteLine("Id must be between 1 and 3");
                Environment.Exit(1);
            }

            //PROCESS INPUT CONFIG FILE

            DateTime dateTimeStart = DateTime.Now;
            int totalTimeSlots = 1; //Default 1
            int slotDuration = 1000;  //DURATION IN MS - Default 1s

            Dictionary<int, ServerEntry> boneyServerEntries = new Dictionary<int, ServerEntry>();
            Dictionary<int, ServerEntry> bankServerEntries = new Dictionary<int, ServerEntry>();
            List<List<Tuple<int, string, string>>> slotStates = new List<List<Tuple<int, string, string>>>();


            string[] lines = File.ReadAllLines(args[0]);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string[] parts = line.Split(new char[] { ' ', ',', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
                switch (line[0])
                {
                    case 'P':
                        Uri address;

                        if (parts[2].Equals("boney"))
                        {
                            address = new Uri(parts[3]);
                            boneyServerEntries.Add(int.Parse(parts[1]), new ServerEntry(address.Host, address.Port));
                        }
                        else if (parts[2].Equals("bank"))
                        {
                            address = new Uri(parts[3]);
                            bankServerEntries.Add(int.Parse(parts[1]), new ServerEntry(address.Host, address.Port));
                        }
                        break;
                    case 'S':
                        totalTimeSlots = int.Parse(parts[1]);
                        break;
                    case 'T':
                        DateTime dateTimeAux = DateTime.Now;
                        string[] time = parts[1].Split(':');
                        dateTimeStart = new DateTime(dateTimeAux.Year, dateTimeAux.Month, dateTimeAux.Day, int.Parse(time[0]), int.Parse(time[1]), int.Parse(time[2]));
                        break;
                    case 'D':
                        slotDuration = int.Parse(parts[1]);
                        break;
                    case 'F':
                        List<Tuple<int, string, string>> slot = new List<Tuple<int, string, string>>();
                        for (int k = 2; k < parts.Length; k += 3)
                        {
                            if (int.Parse(parts[k]) > 3) break;  //NO NEED TO KNOW STATE OF BANK SERVERS FOR BONEY SERVERS 

                            slot.Add(new Tuple<int, string, string>(int.Parse(parts[k]), parts[k + 1], parts[k + 2]));
                        }
                        slotStates.Add(slot);
                        break;
                    default:
                        Console.WriteLine("Config file not formated correctly.");
                        Environment.Exit(1);
                        break;
                }


            }



            ServerPort serverPort;

            serverPort = new ServerPort(boneyServerEntries[id].getHostName(), boneyServerEntries[id].getPortNum(), ServerCredentials.Insecure);
            Console.WriteLine("PreStarting Boney Server {0} with Hostname {1} at Port {2}", id, boneyServerEntries[id].getHostName(), boneyServerEntries[id].getPortNum());

            PaxosState state = new PaxosState(slotStates, id);
            PaxosFrontend paxosFrontend = new PaxosFrontend(boneyServerEntries, state);
            CompareAndSwapServiceImpl cmpAndSwapService = new CompareAndSwapServiceImpl(bankServerEntries, paxosFrontend, state);//dateTimeStart.Ticks
            PaxosServiceImpl paxosService = new PaxosServiceImpl(state, totalTimeSlots);

            Server server = new Server
            {
                Services = { PaxosService.BindService(paxosService),
                             CompareAndSwapService.BindService(cmpAndSwapService)
                           },
                Ports = { serverPort }
            };
            server.Start();
            Console.WriteLine("Finished Startup");

            // Timer setup for state update after every slotDuration
            System.Timers.Timer newTStampTimer = new System.Timers.Timer(slotDuration);

            newTStampTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            newTStampTimer.AutoReset = true;

            //Create Task to enable Timer as well as update state to slot 1
            //long ticksDifference;
            //DateTime dateTimeNow = DateTime.Now;
            //ticksDifference = dateTimeStart.Ticks - DateTime.Now.Ticks;


            Task.Delay((int)((dateTimeStart.Ticks - DateTime.Now.Ticks) / 10000)).ContinueWith(t =>
            {
                newTStampTimer.Enabled = true;
                lock (state)
                {
                    state.updateStatus();
                }
            });

            //THIS ALLOWS FOR SOME TIME FOR OTHER BONEY SERVERS TO STARTUP AND THEN CONNECTS TO THEM AS A CLIENT
            //Task.Delay((int)(ticksDifference / 20000)).ContinueWith(t => ConnectToOtherBoneys());


            void OnTimedEvent(Object? source, ElapsedEventArgs e)
            {
                lock (state)
                {
                    state.updateStatus();
                }
                if (state.GetTimeSlot() == totalTimeSlots)
                {
                    newTStampTimer.Stop();
                }
            }


            while (true) ;



        }
    }
}
