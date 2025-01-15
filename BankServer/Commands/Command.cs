using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankServer.Commands
{
    internal abstract class Command
    {
        public int ClientId { get; }
        public int ClientSeqNumber { get; }
        public int SeqNumber { get; set; }
        public int Slot { get; set; }
        public double Value { get; }

        public Command(int clientId, int clientSeqNumber, int seqNumber, int slot, double value)
        {
            this.ClientId = clientId;
            this.ClientSeqNumber = clientSeqNumber;
            this.SeqNumber = seqNumber;
            this.Slot = slot;
            this.Value = value;
        }

        abstract public List<double> Execute(Account account);

        abstract public CommandGRPC ToGRPC();
    }
}
