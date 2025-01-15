using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace BankServer.Commands
{
    internal class WithdrawalCommand : Command
    {
        public WithdrawalCommand(int clientId, int clientSeqNumber, int seqNumber, int slot, double value) : base(clientId, clientSeqNumber, seqNumber, slot, value) { }

        public override List<double> Execute(Account account)
        {
            lock (account)
            {
                List<double> result = new();
                bool valid = account.Withdrawal(Value);
                if (valid)
                {
                    result.Add(Value);
                }
                else
                {
                    result.Add(0);
                }
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
                Type = CommandType.Withdraw,
                Value = Value
            };
        }
    }
}
