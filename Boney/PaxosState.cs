using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boney
{
    internal class PaxosState
    {
        /*         
         *  List that contais multiple lists - one for each timeslot - each
         *  having the status info about each boney process - Tuple (pId, F/N, S/NS)       
         */
        List<List<Tuple<int, string, string>>> slotStates;

        ElectedSlot[] elected;

        bool amLeader;
        bool amFrozen;

        int predictedLeader;
        int currentTimeSlot = 0;
        int id;



        public PaxosState(List<List<Tuple<int, string, string>>> slotStates, int id)
        {
            this.slotStates = slotStates;
            this.id = id;
            elected = new ElectedSlot[slotStates.Count];

            for (int i = 0; i < slotStates.Count; i++)
            {
                elected[i] = new ElectedSlot();
            }
        }

        /*  public void SetElected(int timeSlot, int value)
          {
              elected[timeSlot - 1] = value;
          }

          public int GetElected(int timeSlot)
          {
              return elected[timeSlot - 1];
          }*/

        public ElectedSlot GetSlot(int slot)
        {
            return elected[slot - 1];
        }

        public int GetTimeSlot()
        {
            return currentTimeSlot;
        }

        public int GetId()
        {
            return id;
        }

        public bool AmFrozen()
        {
            return amFrozen;
        }

        public bool AmLeader()
        {
            return amLeader;
        }
        public void udpateTimeSlot()
        {
            this.currentTimeSlot++;
        }

        /* Updates relevant state fields for boney process with id id */
        public void updateStatus()
        {

            //Use last time slot as it will match the array position of new time slot
            int timeSlotOffseted = this.currentTimeSlot;
            int prediction = int.MaxValue;
            DateTime aux = DateTime.UtcNow;

            //TimeSlot Update
            this.currentTimeSlot++;

            //Leader Predection
            foreach (Tuple<int, string, string> tup in this.slotStates[timeSlotOffseted])
            {

                if ((tup.Item1 < prediction && tup.Item1 == this.id) && tup.Item2.Equals("N"))
                {
                    prediction = this.id;
                    this.amLeader = true;
                    break; //optimization as the first id that passes will be predicted as it is the lowest
                }
                else if ((tup.Item1 < prediction && tup.Item1 != this.id) && tup.Item3.Equals("NS"))
                {
                    prediction = tup.Item1;
                    this.amLeader = false;
                    break;
                }
            }
            predictedLeader = prediction;

            //Frozen Update
            this.amFrozen = this.slotStates[timeSlotOffseted][this.id - 1].Item2.Equals("F");

            Console.WriteLine("{0} - Boney Process updated its status - Leader: {1}" +
                "TS: {2}", aux.ToString("dd-MM-yyyy HH:mm:ss:fff"), prediction, this.currentTimeSlot);
        }
    }

    internal class ElectedSlot
    {

        int elected = 0;

        public ElectedSlot() { }

        public bool IsElected()
        {
            return elected != 0;
        }

        public int GetElectedValue()
        {
            return elected;
        }

        public void SetElectedValue(int val)
        {
            elected = val;
        }
    }
}
