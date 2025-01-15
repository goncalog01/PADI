using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boney
{
    class CompareAndSwapServiceImpl : CompareAndSwapService.CompareAndSwapServiceBase
    {
        Dictionary<int, ServerEntry> bankServers;

        PaxosFrontend frontend;
        PaxosState state;

        public CompareAndSwapServiceImpl(Dictionary<int, ServerEntry> bankServerEntries, PaxosFrontend paxosFrontend, PaxosState stateP)//long ticks
        {
            bankServers = bankServerEntries;
            frontend = paxosFrontend;
            state = stateP;
            /*Task.Delay((int)((ticks - DateTime.Now.Ticks) / 10000) + 5000).ContinueWith(t => CompareAndSwap(new CompareAndSwapRequest
            {
                BankSeverId = 1,
                ProposeLeader = 12/state.GetId(),
                Timeslot = 1
            }
            )
            );  USED FOR SOME DEBUGGING*/
        }

        //This Service will mainly receive CompareAndSwap requests then through a method communicate the value to the paxos service

        public CompareAndSwapReply CompareAndSwap(CompareAndSwapRequest bRequest)
        {

            // TODO PERFECT CHANNEL INTEGRATION/IMPLEMENTATION
            CompareAndSwapRequest request;
            bool frozen;
            bool leader;
            lock (state)
            {
                frozen = state.AmFrozen();
            }
            //WHAT TO DO WHEN FROZEN??
            if (!frozen)
            {
                request = bRequest;
                int bankServerId = request.BankSeverId;
                int timeSlot = request.Timeslot;
                int proposedValue = request.ProposeLeader;

                int electedValue;

                //Check to see if leader
                lock (state)
                {
                    leader = state.AmLeader();
                }

                lock (state.GetSlot(timeSlot))
                {
                    electedValue = state.GetSlot(timeSlot).GetElectedValue();
                }


                /*  If election not yet done and process thinks its leader
                propose new value to Paxos  */
                if (electedValue == 0) //&& leader)
                {
                    electedValue = frontend.Propose(timeSlot, proposedValue);
                }
                // If election not yet done and process not leader 
                else if (electedValue == 0 && !leader)
                {

                    // Wait for elected value to be present
                    Monitor.Enter(state.GetSlot(timeSlot));
                    while (!state.GetSlot(timeSlot).IsElected())
                    {
                        Monitor.Wait(state.GetSlot(timeSlot));
                    }
                    electedValue = state.GetSlot(timeSlot).GetElectedValue();
                    Monitor.Exit(state.GetSlot(timeSlot));

                }

                // Returns elected value if leader
                return new CompareAndSwapReply()
                {
                    BoneySeverId = state.GetId(),
                    ElectedLeader = electedValue,
                    Timeslot = timeSlot
                };

            }
            else
            {
                //SOMETHING


                //TEMPORARY STAND IN FOR COMPILATION PURPOSES FOR CASE WHERE FROZEN
                return new CompareAndSwapReply()
                {
                    BoneySeverId = state.GetId(),
                    ElectedLeader = -1,
                    Timeslot = bRequest.Timeslot
                };
            }
        }
    }
}
