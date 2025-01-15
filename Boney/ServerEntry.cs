using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boney
{
    internal class ServerEntry
    {
        string hostname;
        int portNum;
        public ServerEntry(string host, int port)
        {
            hostname = host;
            portNum = port;
        }

        public string getHostName()
        {
            return hostname;
        }

        public int getPortNum()
        {
            return portNum;
        }
    }
}
