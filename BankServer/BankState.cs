using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankServer.Commands;

namespace BankServer
{
    internal class BankState
    {
        public bool Frozen { get; set; }
        public bool Primary { get; set; }
        public int LastSeqNumber { get; set; } = 0;
        public int Slot { get; set; } = 1;
        private readonly Dictionary<Tuple<int, int>, Command> tentativeCommands = new();
        private readonly Dictionary<int, int> slotsPrimary = new();
        private readonly Dictionary<int, List<int>> notSuspected;
        private readonly Dictionary<int, bool> frozenSlots;
        private readonly int pid;
        private readonly BankFrontend frontend;
        private readonly Account account;

        public BankState(int pid, Dictionary<int, List<int>> notSuspected, Dictionary<int, bool> frozenSlots, BankFrontend frontend, Account account)
        {
            this.pid = pid;
            this.notSuspected = notSuspected;
            this.frozenSlots = frozenSlots;
            this.frontend = frontend;
            this.account = account;
        }

        public int Id()
        {
            return this.pid;
        }

        public List<Command> TentativeCommands()
        {
            return tentativeCommands.Values.ToList();
        }

        public bool AddTentativeCommand(Command command)
        {
            var commandId = new Tuple<int, int>(command.ClientId, command.ClientSeqNumber);
            Command copy;

            lock (tentativeCommands)
            {
                if (tentativeCommands.TryGetValue(commandId, out copy))
                {
                    if (command.Slot > copy.Slot)
                    {
                        tentativeCommands[commandId] = command;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    tentativeCommands[commandId] = command;
                    return true;
                }
            }
        }

        public List<double> ExecuteCommand(Command command)
        {
            var commandId = new Tuple<int, int>(command.ClientId, command.ClientSeqNumber);

            lock (tentativeCommands)
            {
                tentativeCommands.Remove(commandId);

                if (this.LastSeqNumber < command.SeqNumber)
                {
                    this.LastSeqNumber = command.SeqNumber;
                }
            }

            return command.Execute(account);
        }

        public int PrimaryForSlot(int slot)
        {
            return slotsPrimary[slot];
        }

        public void CompareAndSwap()
        {
            this.Frozen = frozenSlots[this.Slot];
            Tuple<int, int> result = frontend.CompareAndSwap(this.Slot, pid, GuessLeader());
            slotsPrimary.Add(result.Item1, result.Item2);
            this.Primary = result.Item2 == pid;
            Cleanup();
        }

        private int GuessLeader()
        {
            int leader;

            if (this.Slot == 1)
            {
                leader = notSuspected[this.Slot].Min();
            }
            else
            {
                if (notSuspected[this.Slot].Contains(slotsPrimary[this.Slot - 1]))
                {
                    leader = slotsPrimary[this.Slot - 1];
                }
                else
                {
                    leader = notSuspected[this.Slot].Min();
                }
            }

            return leader;
        }

        private void Cleanup()
        {
            if (slotsPrimary[this.Slot - 1] != slotsPrimary[this.Slot] && slotsPrimary[this.Slot] == pid)
            {
                List<Command> commandsToCommit = frontend.ListPendingRequests(this.LastSeqNumber, pid, this.Slot);

                foreach (var command in commandsToCommit)
                {
                    Command copy = command;
                    copy.Slot = this.Slot;
                    frontend.Commit(command, pid);
                }
            }
        }
    }
}
