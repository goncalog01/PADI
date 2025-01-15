using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace BankServer.Commands
{
    internal class DepositCommand : Command
    {
        public DepositCommand(int clientId, int clientSeqNumber, int seqNumber, int slot, double value) : base(clientId, clientSeqNumber, seqNumber, slot, value) { }

        public override List<double> Execute(Account account)
        {
            lock (account)
            {
                List<double> result = new();
                account.Deposit(Value);
                result.Add(account.Balance());
                return result;
            }
        }

        public override CommandGRPC ToGRPC()
        {
            return new()
            {
                ClientId = ClientId,
                ClientSeqNumber = ClientSeqNumber,
                SeqNumber = SeqNumber,
                Slot = Slot,
                Type = CommandType.Deposit,
                Value = Value
            };
        }
    }
}
