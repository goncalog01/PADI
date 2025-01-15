using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankServer
{
    internal class Account
    {
        private double balance = 0;

        public double Balance()
        {
            return balance;
        }

        public void Deposit(double amount)
        {
            balance += amount;
        }

        public bool Withdrawal(double amount)
        {
            if (balance >= amount)
            {
                balance -= amount;
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
