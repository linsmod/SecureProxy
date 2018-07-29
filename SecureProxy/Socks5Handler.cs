/*
    Copyright ?2002, The KPD-Team
    All rights reserved.
    http://www.mentalis.org/

  Redistribution and use in source and binary forms, with or without
  modification, are permitted provided that the following conditions
  are met:

    - Redistributions of source code must retain the above copyright
       notice, this list of conditions and the following disclaimer. 

    - Neither the name of the KPD-Team, nor the names of its contributors
       may be used to endorse or promote products derived from this
       software without specific prior written permission. 

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
  FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
  THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
  (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
  SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
  HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
  STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
  OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using RProxy.Proxy;
using RProxy.Proxy.Socks.Authentication;
using System.Threading.Tasks;
using System.Threading;
using SecureProxyServer;

namespace RProxy.Proxy.Socks
{

    ///<summary>Implements the SOCKS5 protocol.</summary>
    internal sealed class Socks5Handler : SocksHandler
    {
        private IPEndPoint UdpClientEndPoint;
        private IPEndPoint LocalBindEndPoint;
        ///<summary>Initializes a new instance of the Socks5Handler class.</summary>
        ///<param name="ClientConnection">The connection with the client.</param>
        ///<param name="Callback">The method to call when the SOCKS negotiation is complete.</param>
        ///<param name="AuthList">The authentication list to use when clients connect.</param>
        ///<exception cref="ArgumentNullException"><c>Callback</c> is null.</exception>
        ///<remarks>If the AuthList parameter is null, no authentication will be required when a client connects to the proxy server.</remarks>
        public Socks5Handler(SocketConnection ClientConnection, NegotiationCompleteDelegate Callback, AuthenticationList AuthList) : base(ClientConnection, Callback)
        {
            this.AuthList = AuthList;
        }
        ///<summary>Initializes a new instance of the Socks5Handler class.</summary>
        ///<param name="ClientConnection">The connection with the client.</param>
        ///<param name="Callback">The method to call when the SOCKS negotiation is complete.</param>
        ///<exception cref="ArgumentNullException"><c>Callback</c> is null.</exception>
        public Socks5Handler(SocketConnection ClientConnection, NegotiationCompleteDelegate Callback) : this(ClientConnection, Callback, null) { }
        ///<summary>Checks whether a specific request is a valid SOCKS request or not.</summary>
        ///<param name="Request">The request array to check.</param>
        ///<returns>True is the specified request is valid, false otherwise</returns>
        protected override async Task<bool> IsValidRequest(byte[] Request)
        {
            try
            {
                return (Request.Length == Request[0] + 1);
            }
            catch
            {
                return false;
            }
        }
        ///<summary>Processes a SOCKS request from a client and selects an authentication method.</summary>
        ///<param name="Request">The request to process.</param>
        protected override async Task ProcessRequest(byte[] Request)
        {
            try
            {
                //Console.WriteLine("AUTH: " + BitConverter.ToString(Request));
                byte Ret = 255;
                for (int Cnt = 1; Cnt < Request.Length; Cnt++)
                {
                    bool authRequired = AuthList != null && AuthList.Keys.Length > 0;
                    //Console.WriteLine("AUTH REQUIRED:" + (authRequired ? "YES" : "NO"));
                    if (Request[Cnt] == 0 && !authRequired)
                    {
                        //0 = No authentication
                        Ret = 0;
                        AuthMethod = new AuthNone();
                        break;
                    }
                    else if (Request[Cnt] == 2 && authRequired)
                    {
                        //2 = user/pass
                        Ret = 2;
                        AuthMethod = new AuthUserPass(AuthList);
                        if (AuthList != null)
                            break;
                    }
                }
                await Connection.SendAsync(new byte[] { 5, Ret }, this.OnAuthSent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] " + ex.Message + "\r\n" + ex.StackTrace);
                await Dispose(false);
            }
        }
        ///<summary>Called when client has been notified of the selected authentication method.</summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
        private void OnAuthSent(int bytes)
        {
            try
            {
                if (bytes <= 0 || AuthMethod == null)
                {
                    Dispose(false);
                    return;
                }
                AuthMethod.StartAuthentication(Connection, new AuthenticationCompleteDelegate(this.OnAuthenticationComplete));
            }
            catch
            {
                Dispose(false);
            }
        }
        ///<summary>Called when the authentication is complete.</summary>
        ///<param name="Success">Indicates whether the authentication was successful ot not.</param>
        private async void OnAuthenticationComplete(SocketConnection conn, bool Success)
        {
            try
            {
                if (Success)
                {
                    Bytes = null;
                    await Connection.ReceiveAsync(Buffer, 0, Buffer.Length, OnRecvRequest);
                }
                else
                {
                    await Dispose(false);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("[WARN] SocketError=" + e.SocketErrorCode);
                await Dispose(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] " + ex.Message + "\r\n" + ex.StackTrace);
                await Dispose(false);
            }
        }
        ///<summary>Called when we received the request of the client.</summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
        private async Task OnRecvRequest(int bytes)
        {
            try
            {
                //Console.WriteLine("RECV: bytes = " + bytes);
                if (bytes <= 0)
                {
                    await Dispose(false);
                    return;
                }
                AddBytes(Buffer, bytes);
                if (await IsValidQuery(Bytes))
                    await ProcessQuery(Bytes);
                else
                    await Connection.ReceiveAsync(Buffer, 0, Buffer.Length, OnRecvRequest);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("[WARN] SocketError=" + ex.SocketErrorCode);
                await Dispose(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] " + ex.Message + "\r\n" + ex.StackTrace);
                await Dispose(false);
            }
        }
        ///<summary>Checks whether a specified query is a valid query or not.</summary>
        ///<param name="Query">The query to check.</param>
        ///<returns>True if the query is valid, false otherwise.</returns>
        private async Task<bool> IsValidQuery(byte[] Query)
        {
            try
            {
                if (Query.Length < 3)
                    return false;
                switch (Query[3])
                {
                    case 1: //IPv4 address
                        return (Query.Length == 10);
                    case 3: //Domain name
                        return (Query.Length == Query[4] + 7);
                    case 4: //IPv6 address
                            //Not supported
                        await Dispose(ReplyCode.UnsupportedAddrType);
                        return false;
                    default:
                        await Dispose(false);
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] " + ex.Message + "\r\n" + ex.StackTrace);
                return false;
            }
        }
        ///<summary>Processes a received query.</summary>
        ///<param name="Query">The query to process.</param>
        private async Task ProcessQuery(byte[] Query)
        {
            try
            {
                Request request = null;
                Request.ParseRequest(Query, out request);
                switch (request.Command)
                {
                    case Command.Connect: //CONNECT
                        IPAddress RemoteIP = request.DstAddr;
                        int RemotePort = 0;
                        if (Query[3] == 1)
                        {
                            RemoteIP = IPAddress.Parse(Query[4].ToString() + "." + Query[5].ToString() + "." + Query[6].ToString() + "." + Query[7].ToString());
                            RemotePort = Query[8] * 256 + Query[9];
                        }
                        else if (Query[3] == 3)
                        {
                            if (RemoteIP == null)
                            {
                                RemoteIP = Dns.GetHostAddressesAsync(Encoding.ASCII.GetString(Query, 5, Query[4])).Result[0];
                            }
                            RemotePort = Query[4] + 5;
                            RemotePort = Query[RemotePort] * 256 + Query[RemotePort + 1];
                        }
                        RemoteConnection = new TcpSocket(RemoteIP.AddressFamily, false);
                        try
                        {
                            await RemoteConnection.ConnectAsync(new IPEndPoint(RemoteIP, RemotePort));
                            await OnConnected(null);
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine("[WARN] SocketError=" + ex.SocketErrorCode + "\r\n");
                            await OnConnected(ex);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[WARN] " + ex.Message + "\r\n" + ex.StackTrace);
                            await OnConnected(ex);
                        }
                        break;
                    case Command.Bind: //BIND
                        byte[] Reply = new byte[10];
                        long LocalIP = BitConverter.ToInt64(Listener.GetLocalExternalIP().Result.GetAddressBytes(), 0);
                        AcceptSocket = new TcpSocket(IPAddress.Any.AddressFamily, false);
                        AcceptSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                        AcceptSocket.Listen(50);
                        Reply[0] = 5;  //Version 5
                        Reply[1] = 0;  //Everything is ok :)
                        Reply[2] = 0;  //Reserved
                        Reply[3] = 1;  //We're going to send a IPv4 address
                        Reply[4] = (byte)((LocalIP % 256));  //IP Address/1
                        Reply[5] = (byte)((LocalIP % 65536) / 256);  //IP Address/2
                        Reply[6] = (byte)((LocalIP % 16777216) / 65536);  //IP Address/3
                        Reply[7] = (byte)(Math.Floor(LocalIP / 16777216.0));  //IP Address/4
                        Reply[8] = (byte)(Math.Floor(((IPEndPoint)AcceptSocket.LocalEndPoint).Port / 256.0));  //Port/1
                        Reply[9] = (byte)(((IPEndPoint)AcceptSocket.LocalEndPoint).Port % 256);  //Port/2
                        await Connection.SendAsync(Reply, async (int x) => { await this.OnStartAccept(); });
                        break;
                    case Command.UdpAssociate: //UDP ASSOCIATE

                        if (request.DstAddr.Equals(IPAddress.Any) && request.DstPort == 0)
                        {
                            request.DstPort = FreePort.FindNextAvailableUDPPort(4200);
                            if (request.DstPort == 0)
                            {
                                await Dispose(ReplyCode.SocksFailure);
                                return;
                            }
                        }
                        if (request.DstAddr.Equals(IPAddress.Any))
                        {
                            request.DstAddr = ((IPEndPoint)Connection.LocalEndPoint).Address;
                        }
                        UdpClientEndPoint = new IPEndPoint(request.DstAddr, request.DstPort);
                        LocalBindEndPoint = new IPEndPoint(((IPEndPoint)(Connection.LocalEndPoint)).Address, request.DstPort);
                        var reply = request.CreateReply(LocalBindEndPoint.Address, LocalBindEndPoint.Port);
                        await Connection.SendAsync(reply.ToBytes(), async (int x) => { await this.StartUdpReceive(); });
                        break;
                    default:
                        await Dispose(ReplyCode.UnsupportedCommand);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] " + ex.Message + "\r\n" + ex.StackTrace);
                await Dispose(ReplyCode.SocksFailure);
            }
        }

        /// <summary>
        /// when udp associate command ok, udpconnection to client is created.
        /// </summary>
        Socket UdpConnection;

        /// <summary>
        /// start receive to relay the udp datagram between client and remote host.
        /// </summary>
        /// <returns></returns>
        private async Task StartUdpReceive()
        {
            UdpConnection = new Socket(UdpClientEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            await UdpConnection.ReceiveAsync(Buffer, OnUdpReceive);
        }

        private async Task OnUdpReceive(int bytes)
        {

        }


        ///<summary>Called when we're successfully connected to the remote host.</summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
        private async Task OnConnected(Exception ex)
        {
            if (ex == null)
            {
                await Dispose(ReplyCode.Ok);
            }
            else
            {
                var socketException = (ex as SocketException);
                switch (socketException.SocketErrorCode)
                {
                    case SocketError.TimedOut:
                        await Dispose(ReplyCode.SocksFailure);
                        break;
                    default:
                        await Dispose(ReplyCode.SocksFailure);
                        break;
                }
            }
        }
        ///<summary>Called when there's an incoming connection in the AcceptSocket queue.</summary>
        ///<param name="ar">The result of the asynchronous operation.</param>
        protected override async Task OnAccept(SocketConnection ar)
        {
            AcceptSocket.Dispose();
            AcceptSocket = null;
            await Dispose(ReplyCode.Ok);
        }
        protected override Task OnAcceptError(Exception ex)
        {
            return Dispose(ReplyCode.SocksFailure);
        }
        ///<summary>Sends a reply to the client connection and disposes it afterwards.</summary>
        ///<param name="Value">A byte that contains the reply code to send to the client.</param>
        protected override async Task Dispose(ReplyCode Value)
        {
            byte[] ToSend;
            try
            {
                if (Value != ReplyCode.Ok)
                {
                    ToSend = new byte[] { 5, (byte)Value, 0, 1, 0, 0, 0, 0, 0, 0 };
                }
                else
                {
                    var bytes = ((IPEndPoint)RemoteConnection.LocalEndPoint).Address.GetAddressBytes();
                    int addr = BitConverter.ToInt32(bytes, 0);
                    ToSend = new byte[10];
                    ToSend[0] = 5;
                    ToSend[1] = (byte)Value;
                    ToSend[2] = 0;
                    ToSend[3] = 1;  //We're going to send a IPv4 address
                    ToSend[4] = (byte)((addr % 256));  //IP Address/1
                    ToSend[5] = (byte)((addr % 65536) / 256);  //IP Address/2
                    ToSend[6] = (byte)((addr % 16777216) / 65536);  //IP Address/3
                    ToSend[7] = (byte)(Math.Floor(addr / 16777216.0));  //IP Address/4
                    ToSend[8] = (byte)(Math.Floor(((IPEndPoint)RemoteConnection.LocalEndPoint).Port / 256.0));  //Port/1
                    ToSend[9] = (byte)(((IPEndPoint)RemoteConnection.LocalEndPoint).Port % 256);  //Port/2}
                }
            }
            catch (Exception ex)
            {
                ToSend = new byte[] { 5, 1, 0, 1, 0, 0, 0, 0, 0, 0 };
            }
            try
            {
                await Connection.SendAsync(ToSend, ToSend[1] == 0 ? OnDisposeGood : (Action<int>)OnDisposeBad);
            }
            catch
            {
                await Dispose(false);
            }
        }
        ///<summary>Gets or sets the the AuthBase object to use when trying to authenticate the SOCKS client.</summary>
        ///<value>The AuthBase object to use when trying to authenticate the SOCKS client.</value>
        ///<exception cref="ArgumentNullException">The specified value is null.</exception>
        private AuthBase AuthMethod
        {
            get
            {
                return m_AuthMethod;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                m_AuthMethod = value;
            }
        }
        ///<summary>Gets or sets the AuthenticationList object to use when trying to authenticate the SOCKS client.</summary>
        ///<value>The AuthenticationList object to use when trying to authenticate the SOCKS client.</value>
        private AuthenticationList AuthList
        {
            get
            {
                return m_AuthList;
            }
            set
            {
                m_AuthList = value;
            }
        }
        // private variables
        /// <summary>Holds the value of the AuthList property.</summary>
        private AuthenticationList m_AuthList;
        /// <summary>Holds the value of the AuthMethod property.</summary>
        private AuthBase m_AuthMethod;
    }

}
