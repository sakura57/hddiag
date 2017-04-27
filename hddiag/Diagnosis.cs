using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;

namespace hddiag
{
    class Diagnosis
    {
        /*
         * Acquire the NetworkInterface object representing a given adapter.
         * 
         * Parameter may be "Wi-Fi" or "Ethernet" for instance.
         * 
         * Returns null if the adapter wasn't found.
         */
        public static NetworkInterface GetAdapter(string name)
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in interfaces)
            {
                if(adapter.Name.Equals(name))
                {
                    return adapter;
                }
            }

            return null;
        }

        /*
         * This function gets our local IP address
         * by opening a dummy socket and noting the
         * local endpoint's address.
         */
        public static IPAddress GetLocalIP()
        {
            IPAddress localIP;

            using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                //connect to some nonexistant server
                sock.Connect("1.1.1.1", 1);
                IPEndPoint endPoint = sock.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address;
                sock.Close();
            }

            return localIP;
        }

        /*
         * Returns true if DNS servers are not set to be
         * automatically acquired by DHCP.
         */
        public static bool DiagnoseIllegalDNSServers(NetworkInterface adapter)
        {
            string adapterParamsKey = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\" + adapter.Id;

            //get the NameServer value from the wi-fi adapter's
            //registry key. this will be nonexistant or blank
            //if DNS servers are set to be automatically
            //acquired
            string nameServer = (string)Registry.GetValue(adapterParamsKey, "NameServer", null);

            return !String.IsNullOrEmpty(nameServer);
        }
    }
}
