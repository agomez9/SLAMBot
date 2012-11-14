using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace SLAMBotClasses
{
    /// <summary>
    /// Contains general / random methods that are useful
    /// </summary>
    public class Common
    {
        /// <summary>
        /// Gets the internal IP on the computer.
        /// </summary>
        /// <returns>IP address as a string</returns>
        public static string GetIP()
        {
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                }
            }
            return localIP;
        }
    }
}
