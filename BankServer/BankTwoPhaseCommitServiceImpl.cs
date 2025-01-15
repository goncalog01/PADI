using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankServer.Commands;

namespace BankServer
{
    internal class BankTwoPhaseCommitServiceImpl : BankTwoPhaseCommitService.BankTwoPhaseCommitServiceBase
    {
        private readonly BankState state;

        public BankTwoPhaseCommitServiceImpl(BankState state)
        {
            this.state = state;
        }

        public override Task<ListPendingRequestsReply> ListPendingRequests(ListPendingRequestsRequest request, ServerCallContext context)
        {
            if (state.Slot == request.Slot && state.PrimaryForSlot(request.Slot) == request.Sender)
            {
                ListPendingRequestsReply reply = new() { Ack = true };

                lock (state)
                {
                    foreach (var command in state.TentativeCommands())
                    {
                        if (command.SeqNumber > request.LastKnownSequenceNumber)
                        {
                            reply.Commands.Add(command.ToGRPC());
                        }
                    }
                }

                return Task.FromResult(reply);
            }
            else
            {
                ListPendingRequestsReply reply = new() { Ack = false };
                return Task.FromResult(reply);
            }
        }

        public override Task<ProposeReply> Propose(ProposeRequest request, ServerCallContext context)
        {
            if (state.PrimaryForSlot(request.Command.Slot) == request.Sender && state.AddTentativeCommand(GRPCtoCommand(request.Command)))
            {
                ProposeReply reply = new() { Ack = true };
                return Task.FromResult(reply);
            }
            else
            {
                ProposeReply reply = new() { Ack = false };
                return Task.FromResult(reply);
            }
        }

        public override Task<CommitReply> Commit(CommitRequest request, ServerCallContext context)
        {
            state.ExecuteCommand(GRPCtoCommand(request.Command));
            CommitReply reply = new();
            return Task.FromResult(reply);
        }

        private static Command GRPCtoCommand(CommandGRPC command)
        {
            switch (command.Type)
            {
                case CommandType.Deposit:
                    return new DepositCommand(command.ClientId, command.ClientSeqNumber, command.SeqNumber, command.Slot, command.Value);
                case CommandType.Read:
                    return new ReadBalanceCommand(command.ClientId, command.ClientSeqNumber, command.SeqNumber, command.Slot, -1);
                case CommandType.Withdraw:
                    return new WithdrawalCommand(command.ClientId, command.ClientSeqNumber, command.SeqNumber, command.Slot, command.Value);
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
