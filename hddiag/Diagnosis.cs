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
using System.Runtime.InteropServices;

namespace hddiag
{
    [ComVisible(false), StructLayout(LayoutKind.Sequential)]
    internal struct IPForwardTable
    {
        public uint Size;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public IPFORWARDROW[] Table;
    };

    [ComVisible(false), StructLayout(LayoutKind.Sequential)]
    internal struct IPFORWARDROW
    {
        internal uint /*DWORD*/ dwForwardDest;
        internal uint /*DWORD*/ dwForwardMask;
        internal uint /*DWORD*/ dwForwardPolicy;
        internal uint /*DWORD*/ dwForwardNextHop;
        internal uint /*DWORD*/ dwForwardIfIndex;
        internal uint /*DWORD*/ dwForwardType;
        internal uint /*DWORD*/ dwForwardProto;
        internal uint /*DWORD*/ dwForwardAge;
        internal uint /*DWORD*/ dwForwardNextHopAS;
        internal uint /*DWORD*/ dwForwardMetric1;
        internal uint /*DWORD*/ dwForwardMetric2;
        internal uint /*DWORD*/ dwForwardMetric3;
        internal uint /*DWORD*/ dwForwardMetric4;
        internal uint /*DWORD*/ dwForwardMetric5;
    };

    public static class NativeMethods
    {
        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        public extern static int GetIpForwardTable(IntPtr /*PMIB_IPFORWARDTABLE*/ pIpForwardTable, ref int /*PULONG*/ pdwSize, bool bOrder);
    }

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
                try
                {
                    sock.Connect("1.1.1.1", 1);
                }
                catch(SocketException se)
                {
                    return null;
                }
                IPEndPoint endPoint = sock.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address;
                sock.Close();
            }

            return localIP;
        }

        /*
         * Given a ptr to the routing table,
         * return an IPForwardTable struct.
         */
        private static IPForwardTable ReadIPForwardTable(IntPtr tablePtr)
        {
            var result = (IPForwardTable)Marshal.PtrToStructure(tablePtr, typeof(IPForwardTable));

            IPFORWARDROW[] table = new IPFORWARDROW[result.Size];
            IntPtr p = new IntPtr(tablePtr.ToInt64() + Marshal.SizeOf(result.Size));
            for (int i = 0; i < result.Size; ++i)
            {
                table[i] = (IPFORWARDROW)Marshal.PtrToStructure(p, typeof(IPFORWARDROW));
                p = new IntPtr(p.ToInt64() + Marshal.SizeOf(typeof(IPFORWARDROW)));
            }
            result.Table = table;

            return result;
        }

        /*
         * Returns an IPForwardTable struct containing
         * the IPv4 routing table.
         * 
         * Invokes unmanaged function GetIpForwardTable.
         */
        public static IPForwardTable GetIPForwardTable()
        {
            var fwdTable = IntPtr.Zero;
            int size = 0;
            var result = NativeMethods.GetIpForwardTable(fwdTable, ref size, true);
            fwdTable = Marshal.AllocHGlobal(size);

            result = NativeMethods.GetIpForwardTable(fwdTable, ref size, true);

            var forwardTable = ReadIPForwardTable(fwdTable);

            Marshal.FreeHGlobal(fwdTable);

            return forwardTable;
        }

        /*
         * Returns the number of default gateways
         * in the routing table. Default gateways
         * have destination IPs of 0.0.0.0.
         */
        public static int DiagnoseMultipleDefaultGateways(IPForwardTable routingTable)
        {
            int defaultGateways = 0;

            foreach(IPFORWARDROW forwardRow in routingTable.Table)
            {
                if(forwardRow.dwForwardDest == 0)
                {
                    ++defaultGateways;
                }
            }

            return defaultGateways;
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

        /*
         * Returns true if DHCP is disabled. (user is requesting
         * a static local IP)
         */
        public static bool DiagnoseDHCPDisabled(NetworkInterface adapter)
        {
            string adapterParamsKey = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Services\\Tcpip\\Parameters\\Interfaces\\" + adapter.Id;

            //get the EnableDHCP value from the wi-fi adapter's
            //registry key. this will be nonexistant or zero
            //if DHCP is disabled
            uint dhcpEnabled = System.Convert.ToUInt32(Registry.GetValue(adapterParamsKey, "EnableDHCP", 0));

            return dhcpEnabled == 0;
        }

        /*
         * Returns true if NetBIOS over TCP/IP is enabled,
         * or if the NetBIOS setting is configured to be obtained
         * via DHCP
         */
        public static bool DiagnoseNetBIOSOverTcpip(NetworkInterface adapter)
        {
            string netBIOSKey = "HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\services\\NetBT\\Parameters\\Interfaces\\Tcpip_" + adapter.Id;

            //get the NetbiosOptions value for this
            //adapter
            uint netBIOSSetting = System.Convert.ToUInt32(Registry.GetValue(netBIOSKey, "NetbiosOptions", 0));

            //0 = acquire setting from DHCP (acceptable)
            //1 = NetBIOS over tcp/ip is enabled
            //2 = disabled
            return netBIOSSetting == 1;
        }

        /*
         * Returns true if the local IP is in an unrecognized range.
         * 
         * Valid IPv4 ranges should be:
         * 128.113.0.0/16
         * 128.213.0.0/16
         * 129.5.0.0/16
         * 129.161.0.0/16
         * 
         * Valid IPv6 ranges:
         * 2620:0:2820:
         * 
         * For now, the function only tests IPv4 address.
         */
        public static bool DiagnoseLocalIpUnrecognizedRange(IPAddress localIP)
        {
            byte[] ip = localIP.GetAddressBytes();
            bool localIpInValidRange = false;

            //the array of valid ranges which are acceptable.
            //to add a new valid range, simply add a new element
            //to this array
            byte[][] validRanges = new byte[][]
            {
                new byte[] { 128, 113 },
                new byte[] { 128, 213 },
                new byte[] { 129, 5 },
                new byte[] { 129, 161 }
            };

            foreach(byte[] validRange in validRanges)
            {
                bool rangeMatched = true;

                for(int i = 0; i < validRange.Length; ++i)
                {
                    if(validRange[i] != ip[i])
                    {
                        rangeMatched = false;
                    }
                }

                if(rangeMatched)
                {
                    localIpInValidRange = true;
                    break;
                }
            }

            return !localIpInValidRange;
        }
    }
}