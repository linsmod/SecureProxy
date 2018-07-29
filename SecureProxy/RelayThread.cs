using RProxy.Proxy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SecureProxyServer
{
    public class RelayThread : IDisposable
    {
        bool _disconnected;
        public byte[] ClientBuffer = new byte[8192 * 4];
        public byte[] RemoteBuffer = new byte[8192 * 4];
        public SocketConnection ClientConnection;
        public SocketConnection RemoteConnection;
        private DestroyDelegate Destroyer;
        Client client;
        public RelayThread(SocketConnection ClientConnection, SocketConnection RemoteConnection, DestroyDelegate Destroyer, Client client)
        {
            this.ClientConnection = ClientConnection;
            this.RemoteConnection = RemoteConnection;
            this.Destroyer = Destroyer;
            this.client = client;
        }
        public void Start()
        {
            ClientConnection.ReceiveAsync(ClientBuffer, OnClientReceive);
            RemoteConnection.ReceiveAsync(RemoteBuffer, OnRemoteReceive);
        }
        //public async Task OnRemoteFirstReceive(int bytesReceived)
        //{
        //    if (bytesReceived > 0)
        //    {
        //        await OnRemoteReceive(bytesReceived);
        //    }
        //    if (bytesReceived < 0)
        //    {
        //        this.Dispose();
        //    }
        //}
        //public async Task OnClientFirstReceive(int bytesReceived)
        //{
        //    if (bytesReceived > 0)
        //    {
        //        await OnClientReceive(bytesReceived);
        //    }
        //    if (bytesReceived < 0)
        //    {
        //        this.Dispose();
        //    }
        //}

        public async Task OnClientReceive(int bytesReceived)
        {
            if (bytesReceived > 0)
            {
                await RemoteConnection.SendAsync(ClientBuffer, 0, bytesReceived, OnRemoteSent);
            }
            if (bytesReceived == 0)
            {
                this.Dispose();
                //await RemoteConnection.ReceiveAsync(RemoteBuffer, OnRemoteReceive);
            }
            if (bytesReceived < 0)
            {
                this.Dispose();
            }
        }

        public async Task OnRemoteReceive(int bytesReceived)
        {
            if (bytesReceived > 0)
            {
                await ClientConnection.SendAsync(RemoteBuffer, 0, bytesReceived, OnClientSent);
            }
            if (bytesReceived == 0)
            {
                this.Dispose();
                //await ClientConnection.ReceiveAsync(ClientBuffer, OnClientReceive);
            }
            if (bytesReceived < 0)
            {
                this.Dispose();
            }
        }

        public void OnClientSent(int bytesSent)
        {
            if (bytesSent > 0)
            {
                RemoteConnection.ReceiveAsync(RemoteBuffer, OnRemoteReceive);
            }
        }

        public void OnRemoteSent(int bytesSent)
        {
            if (bytesSent > 0)
            {
                ClientConnection.ReceiveAsync(ClientBuffer, OnClientReceive);
            }
        }

        bool _disposeCalled;
        public void Dispose()
        {
            if (!_disposeCalled)
            {
                _disposeCalled = true;
                _disconnected = true;
                if (this.ClientConnection != null)
                {
                    this.ClientConnection.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                    this.ClientConnection.Close();
                }
                if (this.RemoteConnection != null)
                {
                    this.RemoteConnection.Shutdown(System.Net.Sockets.SocketShutdown.Both);
                    this.RemoteConnection.Close();
                }

                if (this.Destroyer != null)
                    this.Destroyer(client);
            }
        }
    }
}
