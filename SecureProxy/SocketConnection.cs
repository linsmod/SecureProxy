using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SecureProxyServer
{
    public class SocketConnection : IDisposable
    {
        public SocketConnection(Socket socket, bool secure)
        {
            this.socket = socket;
            this.secure = secure;
            if (socket.Connected)
            {
                this.remoteEndPoint = socket.RemoteEndPoint;
                this.localEndPoint = socket.LocalEndPoint;
            }
        }
        public SocketConnection(AddressFamily af, SocketType st, ProtocolType pt, bool secure)
        {
            this.socket = new Socket(af, st, pt);
            this.secure = secure;
        }
        public bool secure { get; set; }
        public Socket socket { get; set; }
        public void Dispose()
        {
            socket.Dispose();
        }
        public void Close()
        {
            socket.Close();
        }
        EndPoint localEndPoint;
        public EndPoint LocalEndPoint
        {
            get { return localEndPoint ?? socket.LocalEndPoint; }
        }
        EndPoint remoteEndPoint;
        public EndPoint RemoteEndPoint
        {
            get { return remoteEndPoint ?? socket.RemoteEndPoint; }
        }

        public bool Connected { get { return socket.Connected; } }

        public async Task ReceiveAsync(byte[] buffer, Func<int, Task> callback)
        {
            await this.ReceiveAsync(buffer, 0, buffer.Length, callback);
        }

        public async Task ReceiveAsync(byte[] buffer, int offset, int len, Func<int, Task> callback)
        {
            try
            {
                SocketAsyncEventArgs sockArgs;

                if (!recvQueue.TryDequeue(out sockArgs))
                {
                    sockArgs = new SocketAsyncEventArgs();
                }
                sockArgs.SetBuffer(buffer, offset, len);
                sockArgs.Completed += SockArgsRecv_Completed;
                sockArgs.UserToken = callback;
                if (!socket.ReceiveAsync(sockArgs))
                {
                    SockArgsRecv_Completed(socket, sockArgs);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException ex)
            {
                Console.WriteLine("[WARN] ReceiveAsync | SocketError=" + ex.SocketErrorCode);
            }

        }


        public async Task SendAsync(byte[] buffer, int offset, int len, Action<int> callback)
        {
            if (secure)
                SecureStream.Encrypt(buffer, offset, len);
            int bytes = -1;
            try
            {
                SocketAsyncEventArgs sockArgs;
                if (!sendQueue.TryDequeue(out sockArgs))
                {
                    sockArgs = new SocketAsyncEventArgs();
                }
                sockArgs.SetBuffer(buffer, offset, len);
                sockArgs.Completed += SockArgsSend_Completed;
                sockArgs.UserToken = callback;
                if (!socket.SendAsync(sockArgs))
                {
                    SockArgsSend_Completed(socket, sockArgs);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (SocketException ex)
            {
                Console.WriteLine("[WARN] SendAsync | SocketError=" + ex.SocketErrorCode);
            }
        }
        ConcurrentQueue<SocketAsyncEventArgs> sendQueue = new ConcurrentQueue<SocketAsyncEventArgs>();
        ConcurrentQueue<SocketAsyncEventArgs> recvQueue = new ConcurrentQueue<SocketAsyncEventArgs>();
        private void SockArgsSend_Completed(object sender, SocketAsyncEventArgs e)
        {
            var callback = e.UserToken as Action<int>;
            e.Completed -= SockArgsSend_Completed;
            if (e.LastOperation == SocketAsyncOperation.Send)
            {
                sendQueue.Enqueue(e);
            }

            Console.WriteLine("{0} {1} =>     {2} {3}", "SEND", this.LocalEndPoint.ToString().PadRight(22), this.RemoteEndPoint.ToString().PadRight(22), e.BytesTransferred);
            if (secure && e.BytesTransferred > 0)
                SecureStream.Decrypt(e.Buffer, 0, e.BytesTransferred);
            callback(e.BytesTransferred);
        }

        private void SockArgsRecv_Completed(object sender, SocketAsyncEventArgs e)
        {
            var callback = e.UserToken as Func<int, Task>;
            e.Completed -= SockArgsRecv_Completed;
            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                recvQueue.Enqueue(e);
            }

            Console.WriteLine("{0} {1} <=     {2} {3}", "RECV", this.LocalEndPoint.ToString().PadRight(22), this.RemoteEndPoint.ToString().PadRight(22), e.BytesTransferred);
            if (secure && e.BytesTransferred > 0)
                SecureStream.Decrypt(e.Buffer, 0, e.BytesTransferred);
            callback(e.BytesTransferred).Wait();
        }

        public async Task SendAsync(byte[] buffer, Action<int> callback)
        {
            await SendAsync(buffer, 0, buffer.Length, callback);
        }

        public async Task AcceptAsync(Func<SocketConnection, Task> callback)
        {
            var newSocket = await socket.AcceptAsync();
            await callback(new SocketConnection(newSocket, this.secure));
        }

        internal void Shutdown(SocketShutdown both)
        {
            socket.Shutdown(both);
        }

        public async Task ConnectAsync(EndPoint remoteEp)
        {
            await socket.ConnectAsync(remoteEp);
            this.remoteEndPoint = socket.RemoteEndPoint;
            this.localEndPoint = socket.LocalEndPoint;
        }

        internal void Bind(IPEndPoint iPEndPoint)
        {
            socket.Bind(iPEndPoint);
        }

        internal void Listen(int v)
        {
            socket.Listen(v);
        }

        internal async Task<SocketConnection> AcceptAsync()
        {
            return new SocketConnection(await this.socket.AcceptAsync(), this.secure);
        }

        internal void SetSocketOption(SocketOptionLevel l, SocketOptionName keepAlive, int v)
        {
            socket.SetSocketOption(l, keepAlive, v);
        }
    }

    public class TcpSocket : SocketConnection
    {
        public TcpSocket(AddressFamily af, bool secure) : base(af, SocketType.Stream, ProtocolType.Tcp, secure)
        {
        }
    }
    public class UdpSocket : SocketConnection
    {
        public UdpSocket(AddressFamily af, bool secure) : base(af, SocketType.Dgram, ProtocolType.Udp, secure)
        {

        }
    }
}
