using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BankServer.Commands;

namespace BankServer
{
    internal class BankFrontend
    {
        private readonly List<GrpcChannel> boneyServers = new();
        private readonly List<GrpcChannel> bankServers = new();
        private readonly int quorum;

        public BankFrontend(List<string> boneyServers, List<string> bankServers)
        {
            foreach (var s in boneyServers)
            {
                this.boneyServers.Add(GrpcChannel.ForAddress(s));
            }

            foreach (var s in bankServers)
            {
                this.bankServers.Add(GrpcChannel.ForAddress(s));
            }

            quorum = (bankServers.Count / 2) + 1;
        }

        public void CloseChannels()
        {
            foreach (var channel in boneyServers)
            {
                channel.ShutdownAsync().Wait();
            }

            foreach (var channel in bankServers)
            {
                channel.ShutdownAsync().Wait();
            }
        }

        public Tuple<int, int> CompareAndSwap(int slot, int senderId, int proposeLeader)
        {
            Tuple<int, int> elected = new(0, 0);
            foreach (var channel in boneyServers)
            {
                var client = new CompareAndSwapService.CompareAndSwapServiceClient(channel);
                CompareAndSwapRequest request = new CompareAndSwapRequest { Timeslot = slot, BankServerId = senderId, ProposeLeader = proposeLeader };
                CompareAndSwapReply reply = client.CompareAndSwap(request);
                if (elected.Item1 == 0 && elected.Item2 == 0)
                {
                    elected = Tuple.Create(reply.Timeslot, reply.ElectedLeader);
                }
            }
            return elected;
        }

        public List<Command> ListPendingRequests(int lastSeqNumber, int senderId, int slot)
        {
            Dictionary<Tuple<int, int>, Command> commandsToCommit = new();
            int responses = 0;

            foreach (var channel in bankServers)
            {
                var client = new BankTwoPhaseCommitService.BankTwoPhaseCommitServiceClient(channel);

                Thread thread = new(() =>
                {
                    ListPendingRequestsRequest request = new() { LastKnownSequenceNumber = lastSeqNumber, Sender = senderId, Slot = slot };
                    ListPendingRequestsReply reply = client.ListPendingRequests(request);

                    if (reply.Ack)
                    {
                        foreach (var command in reply.Commands)
                        {
                            var commandId = new Tuple<int, int>(command.ClientId, command.ClientSeqNumber);
                            Command copy;

                            if (commandsToCommit.TryGetValue(commandId, out copy))
                            {
                                if (command.Slot > copy.Slot)
                                {
                                    commandsToCommit[commandId] = GRPCtoCommand(command);
                                }
                            }
                            else
                            {
                                commandsToCommit[commandId] = GRPCtoCommand(command);
                            }
                        }
                        responses++;
                    }
                });
                thread.Start();
            }

            while (responses < quorum) { }

            var commands = commandsToCommit.Values.ToList();
            return commands.OrderBy(c => c.Slot).ToList();
        }

        public void Commit(Command command, int senderId)
        {
            int responses = 0;

            foreach (var channel in bankServers)
            {
                var client = new BankTwoPhaseCommitService.BankTwoPhaseCommitServiceClient(channel);

                Thread thread = new(() =>
                {
                    ProposeRequest request = new ProposeRequest { Command = command.ToGRPC(), Sender = senderId };
                    ProposeReply reply = client.Propose(request);

                    if (reply.Ack)
                    {
                        responses++;
                    }
                });
                thread.Start();
            }

            while (responses < quorum) { }

            foreach (var channel in bankServers)
            {
                var client = new BankTwoPhaseCommitService.BankTwoPhaseCommitServiceClient(channel);
                CommitRequest request = new CommitRequest { Command = command.ToGRPC() };
                CommitReply reply = client.Commit(request);
            }
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
