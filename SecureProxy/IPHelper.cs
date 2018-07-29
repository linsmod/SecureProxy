using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RProxy
{
    public class IPHelper
    {
        public static long Endpoint2Int(EndPoint ep)
        {
            return BitConverter.ToInt64((ep as IPEndPoint).Address.GetAddressBytes(), 0);
        }

        public static long IP2Int(IPAddress addr)
        {
            return BitConverter.ToInt64(addr.GetAddressBytes(), 0);
        }
    }
}
