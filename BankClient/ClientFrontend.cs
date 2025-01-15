using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankClient
{
    internal class ClientFrontend
    {
        private readonly List<GrpcChannel> bankServers = new();

        public ClientFrontend(List<string> bankServers)
        {
            foreach (var s in bankServers)
            {
                this.bankServers.Add(GrpcChannel.ForAddress(s));
            }
        }

        public void CloseChannels()
        {
            foreach (var channel in bankServers)
            {
                channel.ShutdownAsync().Wait();
            }
        }

        public void Deposit(int clientId, int seqNumber, double amount)
        {
            int responses = 0;

            foreach (var channel in bankServers)
            {
                var client = new BankClientService.BankClientServiceClient(channel);

                Thread thread = new(() =>
                {
                    DepositRequest request = new() { ClientId = clientId, ClientSeqNumber = seqNumber, Value = amount };
                    DepositReply reply = client.Deposit(request);

                    if (reply.Primary)
                    {
                        Console.WriteLine($"Received DepositReply from primary server: balance = {reply.Balance}.");
                    }
                    else
                    {
                        Console.WriteLine("Received DepositReply from backup server.");
                    }
                    responses++;
                });
                thread.Start();
            }

            while (responses < bankServers.Count) { }
        }

        public void ReadBalance(int clientId, int seqNumber)
        {
            int responses = 0;

            foreach (var channel in bankServers)
            {
                var client = new BankClientService.BankClientServiceClient(channel);

                Thread thread = new(() =>
                {
                    ReadBalanceRequest request = new() { ClientId = clientId, ClientSeqNumber = seqNumber };
                    ReadBalanceReply reply = client.ReadBalance(request);

                    if (reply.Primary)
                    {
                        Console.WriteLine($"Received ReadBalanceReply from primary server: balance = {reply.Balance}.");
                    }
                    else
                    {
                        Console.WriteLine("Received ReadBalanceReply from backup server.");
                    }
                    responses++;
                });
                thread.Start();
            }

            while (responses < bankServers.Count) { }
        }

        public void Withdraw(int clientId, int seqNumber, double amount)
        {
            int responses = 0;

            foreach (var channel in bankServers)
            {
                var client = new BankClientService.BankClientServiceClient(channel);

                Thread thread = new(() =>
                {
                    WithdrawalRequest request = new() { ClientId = clientId, ClientSeqNumber = seqNumber, Value = amount };
                    WithdrawalReply reply = client.Withdrawal(request);

                    if (reply.Primary)
                    {
                        Console.WriteLine($"Received WithdrawalReply from primary server: value = {reply.Value}, balance = {reply.Balance}.");
                    }
                    else
                    {
                        Console.WriteLine("Received WithdrawalReply from backup server.");
                    }
                    responses++;
                });
                thread.Start();
            }

            while (responses < bankServers.Count) { }
        }
    }
}
