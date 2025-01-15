using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boney
{
    internal class PaxosFrontend
    {
        Dictionary<int, ServerEntry> boneyServers;
        int NR_SERVERS, QUORUM;
        int ballotNr;
        PaxosState state;

        private readonly GrpcChannel[] channel;
        private readonly PaxosService.PaxosServiceClient[] client;



        public PaxosFrontend(Dictionary<int, ServerEntry> bnyServers, PaxosState pState)
        {
            boneyServers = bnyServers;
            state = pState;
            NR_SERVERS = boneyServers.Count;
            QUORUM = (NR_SERVERS / 2) + 1;
            ballotNr = state.GetId();
            channel = new GrpcChannel[NR_SERVERS];
            client = new PaxosService.PaxosServiceClient[NR_SERVERS];


            for (int i = 0; i < NR_SERVERS; i++)
            {
                ServerEntry server = boneyServers[i + 1];
                AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

                channel[i] = GrpcChannel.ForAddress("http://" + server.getHostName() + ":" + server.getPortNum().ToString());
                client[i] = new PaxosService.PaxosServiceClient(channel[i]);
            }

        }

        //This will be the Proposer Role && all its code
        public int Propose(int tSlot, int pValue)
        {
            //FROZEN AND NON FROZEN CASES TO IMPLEMENT
            int electedValue;
            lock (state.GetSlot(tSlot))
            {
                electedValue = state.GetSlot(tSlot).GetElectedValue();
            }

            //This check is already done on C&Swap Impl however it is done again here
            // as an abundance of caution
            if (electedValue != 0) return electedValue;


            //Normal functioning proposer

            int valueToPropose = pValue;
            bool reset;

            while (true)
            {

                reset = false;
                int highestWT = 0;
                int acceptResponses = 0, promiseResponses = 0;

                //PREPARE REQUESTS
                List<Task<Promise>> promises = new List<Task<Promise>>();
                foreach (var client in this.client)
                {
                    Console.WriteLine("Proposer -- Prepare sent with SeqNr {0}", ballotNr);
                    promises.Add(client.PrepareAsync(new PrepareRequest
                    {
                        Timeslot = tSlot,
                        SequenceNr = ballotNr
                    }).ResponseAsync);
                }

                //WAIT FOR QUORUM OF PROMISES
                while (promiseResponses < QUORUM)
                {
                    Promise promiseReceived = new Promise(); // HACK
                    if (promises.Count > 0)
                    {
                        Task<Promise> promiseTaskReceived = Task.WhenAny(promises).Result;
                        promiseReceived = promiseTaskReceived.Result;

                        promiseResponses++;
                        promises.Remove(promiseTaskReceived);
                    }
                    Console.WriteLine("Proposer -- Promise received {0}", promiseReceived.ToString());
                    //If nok immediatly reset as we only have 3 processes not big deal
                    if (promiseReceived.Status == Status.NokTsLow)
                    {
                        ballotNr += NR_SERVERS;
                        reset = true;
                        break; // JUMP TO BEGGINING AKA TRY AGAIN WITH HIGHER BALLOT NR
                    }


                    //OK
                    if (promiseReceived.Status == Status.Ok)
                    {
                        //Checks if WT on reply is highest and adopt value if so (highest = 0 at start)
                        if (promiseReceived.WriteTimeStamp > highestWT)
                        {

                            valueToPropose = promiseReceived.Value;
                            highestWT = promiseReceived.WriteTimeStamp;

                        }

                    }

                    //Election was already finished
                    if (promiseReceived.Status == Status.NokElectionFinished)
                    {
                        return promiseReceived.Value;
                    }
                }

                //Reset from Nok inside prepare phase
                if (reset) continue;


                //ACCEPT REQUESTS
                List<Task<AcceptReply>> accepted = new List<Task<AcceptReply>>();
                foreach (var client in this.client)
                {
                    Console.WriteLine("Proposer -- Accept sent with SeqNr {0} and Value {1}",
                         ballotNr, valueToPropose);

                    accepted.Add(client.AcceptAsync(new AcceptRequest
                    {
                        Timeslot = tSlot,
                        SequenceNr = ballotNr,
                        Value = valueToPropose
                    }).ResponseAsync); ;
                }
                //ACCEPT PHASE 
                while (acceptResponses < QUORUM)
                {
                    AcceptReply acceptReplyReceived = new AcceptReply(); // HACK
                    if (accepted.Count > 0)
                    {
                        Task<AcceptReply> acceptTaskReceived = Task.WhenAny(accepted).Result;
                        acceptReplyReceived = acceptTaskReceived.Result;

                        Console.WriteLine("Proposer -- AcceptReply received {0}", acceptReplyReceived.ToString());

                        //ACCEPTED COUNTS TOWARDS QUORUM
                        if (acceptReplyReceived.Accepted && acceptReplyReceived.Timeslot == tSlot)
                        {
                            acceptResponses++;
                        }

                        accepted.Remove(acceptTaskReceived);
                    }

                    //IF RECEIVED ALL ANSWERS BUT NO QUORUM START FROM BEGINING
                    if (accepted.Count == 0 && acceptResponses < QUORUM)
                    {
                        ballotNr += NR_SERVERS;
                        reset = true;
                        break;
                    }
                }
                //Couldnt get quorum, try again with higher ballotNr
                if (reset) continue;

                //Send accepted value to listerners and then return without waiting for response
                foreach (var client in this.client)
                {
                    Console.WriteLine("Proposer -- Listen sent with SeqNr {0} and Value {1}",
                        ballotNr, valueToPropose);

                    client.ListenAsync(new AcceptListenerRequest
                    {
                        Timeslot = tSlot,
                        SequenceNr = ballotNr,
                        ValueAccepted = valueToPropose
                    });
                }

                Console.WriteLine("Proposer -- Value sent to C&Swap{0}", valueToPropose);

                return valueToPropose;
            }

        }
    }

}