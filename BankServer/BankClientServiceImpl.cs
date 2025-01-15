using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankServer.Commands;

namespace BankServer
{
    internal class BankClientServiceImpl : BankClientService.BankClientServiceBase
    {
        private readonly BankState state;
        private readonly BankFrontend frontend;

        public BankClientServiceImpl(BankState state, BankFrontend frontend)
        {
            this.state = state;
            this.frontend = frontend;
        }

        public override Task<DepositReply> Deposit(DepositRequest request, ServerCallContext context)
        {
            if (state.Primary)
            {
                DepositCommand command = new(request.ClientId, request.ClientSeqNumber, state.LastSeqNumber + 1, state.Slot, request.Value);
                state.AddTentativeCommand(command);
                frontend.Commit(command, state.Id());
                List<double> result = state.ExecuteCommand(command);
                DepositReply reply = new() { Primary = true, Balance = result[0] };
                return Task.FromResult(reply);
            }
            else
            {
                DepositReply reply = new() { Primary = false };
                return Task.FromResult(reply);
            }
        }

        public override Task<WithdrawalReply> Withdrawal(WithdrawalRequest request, ServerCallContext context)
        {
            if (state.Primary)
            {
                WithdrawalCommand command = new(request.ClientId, request.ClientSeqNumber, state.LastSeqNumber + 1, state.Slot, request.Value);
                state.AddTentativeCommand(command);
                frontend.Commit(command, state.Id());
                List<double> result = state.ExecuteCommand(command);
                WithdrawalReply reply = new() { Primary = true, Value = result[0], Balance = result[1] };
                return Task.FromResult(reply);
            }
            else
            {
                WithdrawalReply reply = new() { Primary = false };
                return Task.FromResult(reply);
            }
        }

        public override Task<ReadBalanceReply> ReadBalance(ReadBalanceRequest request, ServerCallContext context)
        {
            if (state.Primary)
            {
                ReadBalanceCommand command = new(request.ClientId, request.ClientSeqNumber, state.LastSeqNumber + 1, state.Slot, -1);
                state.AddTentativeCommand(command);
                frontend.Commit(command, state.Id());
                List<double> result = state.ExecuteCommand(command);
                ReadBalanceReply reply = new() { Primary = true, Balance = result[0] };
                return Task.FromResult(reply);
            }
            else
            {
                ReadBalanceReply reply = new() { Primary = false };
                return Task.FromResult(reply);
            }
        }
    }
}
