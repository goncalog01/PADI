using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boney
{
    class PaxosServiceImpl : PaxosService.PaxosServiceBase
    {
        private class TimeStamps
        {
            public int writeTS { get; set; }
            public int readTS { get; set; }
            public int acceptedV { get; set; }
            public TimeStamps(int wt, int rt)
            {
                writeTS = wt;
                readTS = rt;
                acceptedV = -1;
            }
        }

        int id;
        PaxosState state;
        List<TimeStamps> timeStamps;


        //repliesReceived, quorum
        public PaxosServiceImpl(PaxosState pxState, int nrSlots)
        {
            state = pxState;
            timeStamps = new List<TimeStamps>();
            lock (state)
            {
                id = state.GetId();
            }

            for (int i = 0; i < nrSlots; i++)
            {
                timeStamps.Add(new TimeStamps(0, 0));
            }

        }

        //Acceptor code
        public override Task<Promise> Prepare(PrepareRequest request, ServerCallContext context)
        {
            Console.WriteLine("Acceptor -- Prepare Received {0}", request.ToString());
            return Task.FromResult(DoPrepare(request));
        }
        private Promise DoPrepare(PrepareRequest prepareRequest)
        {
            int tSlot = prepareRequest.Timeslot;
            int elected, accepted;
            Promise promise = new Promise();
            lock (state.GetSlot(tSlot))
            {
                elected = state.GetSlot(tSlot).GetElectedValue();
            }
            //No value has been elected
            if (elected == 0)
            {
                lock (timeStamps)
                {
                    accepted = timeStamps[tSlot].acceptedV;
                    //Promise with higher SeqNr has been seen
                    if (timeStamps[tSlot].readTS >= prepareRequest.SequenceNr)
                    {
                        promise.Value = -1;
                        promise.Timeslot = tSlot;
                        promise.Status = Status.NokTsLow;
                        promise.WriteTimeStamp = timeStamps[tSlot].writeTS;
                    }
                    // Normal case SeqNr higher than rTS
                    else
                    {
                        promise.Value = accepted;
                        promise.Timeslot = tSlot;
                        promise.Status = Status.Ok;
                        promise.WriteTimeStamp = timeStamps[tSlot].writeTS;
                        timeStamps[tSlot].readTS = prepareRequest.SequenceNr;
                    }
                }

            }
            else
            {
                promise.Value = elected;
                promise.Timeslot = tSlot;
                promise.Status = Status.NokElectionFinished;
                promise.WriteTimeStamp = -1; //Doesnt matter
            }


            return promise;
        }

        public override Task<AcceptReply> Accept(AcceptRequest accept, ServerCallContext context)
        {
            Console.WriteLine("Acceptor -- Accept Received {0}", accept.ToString());
            return Task.FromResult(DoAccept(accept));
        }

        private AcceptReply DoAccept(AcceptRequest accept)
        {
            int rTS;
            AcceptReply acceptReply = new AcceptReply();
            lock (timeStamps)
            {
                rTS = timeStamps[accept.Timeslot].readTS;
            }
            if (accept.SequenceNr != rTS)
            {
                acceptReply.Accepted = false;
                acceptReply.Timeslot = accept.Timeslot;
            }
            else
            {
                lock (timeStamps)
                {
                    timeStamps[accept.Timeslot].writeTS = accept.SequenceNr;
                    timeStamps[accept.Timeslot].acceptedV = accept.Value;
                }
                acceptReply.Accepted = true;
                acceptReply.Timeslot = accept.Timeslot;
            }

            return acceptReply;
        }

        //Listener
        public override Task<Empty> Listen(AcceptListenerRequest request, ServerCallContext context)
        {
            Console.WriteLine("Listener -- Listen Received {0}", request.ToString());
            return Task.FromResult(DoListen(request));
        }
        private Empty DoListen(AcceptListenerRequest listen)
        {
            lock (state)
            {
                state.GetSlot(listen.Timeslot).SetElectedValue(listen.ValueAccepted);
            }
            return new Empty { };
        }
    }
}
