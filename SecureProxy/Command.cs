using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RProxy
{
    public class RequestParseException : Exception
    {
        private ReplyCode unsupportedAddrType;

        public RequestParseException(ReplyCode unsupportedAddrType)
        {
            this.unsupportedAddrType = unsupportedAddrType;
        }

        public Reply ErrorCode { get; set; }
    }
    public class Request
    {
        public byte Version { get; set; }
        public Command Command { get; set; }
        public byte Reserved { get; set; }
        public AddressType AddrType { get; set; }
        /// <summary>
        /// desired destination address
        /// </summary>
        public IPAddress DstAddr { get; set; }

        /// <summary>
        /// desired destination port in network octet order
        /// </summary>
        public int DstPort { get; set; }
        public static void ParseRequest(byte[] buffer, out Request request)
        {
            request = new Request();
            request.Version = buffer[0];
            request.Command = (Command)buffer[1];
            request.Reserved = buffer[2];
            request.AddrType = (AddressType)buffer[3];
            if (request.AddrType == AddressType.IpV4)
            {
                var addr = new byte[4];
                Array.Copy(buffer, 4, addr, 0, 4);
                request.DstAddr = new IPAddress(addr);
                request.DstPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(new byte[] { 0, 0, buffer[4 + 4], buffer[4 + 5] }, 0));
            }
            else if (request.AddrType == AddressType.DomainName)
            {
                var domainLen = buffer[4];
                var domain = new byte[domainLen];
                Array.Copy(buffer, 5, domain, 0, domainLen);
                var domainStr = Encoding.UTF8.GetString(domain);
                try
                {
                    request.DstAddr = Dns.GetHostAddressesAsync(domainStr).Result[0];
                    Console.WriteLine("domain:" + domainStr + ", ip=" + request.DstAddr);
                }
                catch
                {
                    Console.WriteLine("domain:" + domainStr + ", ip=" + request.DstAddr);
                    throw new RequestParseException(ReplyCode.HostUnreachable);
                }
                request.DstPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(new byte[] { 0, 0, buffer[4 + domainLen], buffer[4 + domainLen + 1] }, 0));
            }
            else if (request.AddrType == AddressType.IpV6)
            {
                var addr = new byte[16];
                Array.Copy(buffer, 4, addr, 0, 16);
                request.DstAddr = new IPAddress(addr);
                request.DstPort = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(new byte[] { 0, 0, buffer[4 + 16], buffer[4 + 17] }, 0));
            }
            else
            {
                throw new RequestParseException(ReplyCode.UnsupportedAddrType);
            }
        }
        public Reply CreateReply(IPAddress bindAddr, int bindPort)
        {
            var reply = new Reply();
            reply.Version = this.Version;
            reply.ReplyCode = ReplyCode.Ok;
            reply.Reserved = 0;
            reply.AddrType = AddrType;
            reply.BindAddr = bindAddr;
            reply.BindPort = bindPort;
            return reply;
        }
    }
    public class Reply
    {
        public byte Version { get; set; }
        public ReplyCode ReplyCode { get; set; }
        /// <summary>
        /// RESERVED
        /// </summary>
        public byte Reserved { get; set; }
        /// <summary>
        /// address type of following address
        /// </summary>
        public AddressType AddrType { get; set; }

        public IPAddress BindAddr { get; set; }
        public int BindPort { get; set; }
        public byte[] ToBytes()
        {
            var buffer = new List<byte>();
            buffer.Add(Version);
            buffer.Add((byte)ReplyCode);
            buffer.Add(Reserved);
            buffer.Add((byte)AddrType);
            buffer.AddRange(BindAddr.GetAddressBytes());
            buffer.AddRange(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(BindPort)).Skip(2));
            return buffer.ToArray();
        }
    }

    public enum Command
    {
        Unknown = 0,
        Connect = 1,
        Bind = 2,
        UdpAssociate = 3
    }
    public enum AddressType
    {
        Unknown = 0,
        IpV4 = 1,
        DomainName = 3,
        IpV6 = 4
    }
}
