using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RProxy
{
    public enum ReplyCode : byte
    {
        /// <summary>
        /// succeeded
        /// </summary>
        Ok = 0,
        /// <summary>
        /// general SOCKS server failure
        /// </summary>
        SocksFailure = 1,
        /// <summary>
        /// connection not allowed by ruleset
        /// </summary>
        ConnectionDisallowed = 2,
        /// <summary>
        ///  Network unreachable
        /// </summary>
        NetworkUnreachable = 3,
        /// <summary>
        /// Host unreachable
        /// </summary>
        HostUnreachable = 4,
        /// <summary>
        /// Connection refused
        /// </summary>
        ConnnectonRefused = 5,
        /// <summary>
        /// TTL 失效
        /// </summary>
        TtlExpires = 6,
        /// <summary>
        /// Command not supported
        /// </summary>
        UnsupportedCommand = 7,
        /// <summary>
        /// Address type not supported
        /// </summary>
        UnsupportedAddrType = 8,
        /// <summary>
        /// 9-255保留
        /// </summary>
        Reserved = 9,

        //socks4
        RequestGranted = 90,//: request granted
        /// <summary>
        /// request rejected or failed
        /// </summary>
        RequestRejected1 = 91,
        /// <summary>
        /// request rejected becasue SOCKS server cannot connect to identd on the client
        /// </summary>
        RequestRejected2 = 92,//: 
        /// <summary>
        /// request rejected because the client program and identd report different user-ids
        /// </summary>
        RequestRejected3 = 93 //
    }
}
