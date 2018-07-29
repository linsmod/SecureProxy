using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RProxy.Proxy
{
    public static class AsyncHelper
    {
        public static async Task ReceiveAsync(this Socket socket, byte[] buffer, Func<int, Task> callback)
        {
            var bytes = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None);
            await callback(bytes);
        }

        public static async Task ReceiveAsync(this Socket socket, byte[] buffer, int offset, int len, Func<int, Task> callback)
        {
            var bytes = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, len), SocketFlags.None);
            await callback(bytes);
        }

        public static async Task ReceiveAsync(this Socket socket, byte[] buffer, int offset, int len, Func<Task> callback)
        {
            var bytes = await socket.ReceiveAsync(new ArraySegment<byte>(buffer, offset, len), SocketFlags.None);
            await callback();
        }


        public static async Task SendAsync(this Socket socket, byte[] buffer, int offset, int len, Func<int, Task> callback)
        {
            var bytes = await socket.SendAsync(new ArraySegment<byte>(buffer, offset, len), SocketFlags.None);
            await callback(bytes);
        }

        public static async Task SendAsync(this Socket socket, byte[] buffer, Func<Task> callback)
        {
            var bytes = await socket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None);
            await callback();
        }

        public static async Task SendAsync(this Socket socket, byte[] buffer, Func<int, Task> callback)
        {
            var bytes = await socket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), SocketFlags.None);
            await callback(bytes);
        }

        public static async Task AcceptAsync(this Socket socket, Func<Socket, Task> callback)
        {
            var newSocket = await socket.AcceptAsync();
            await callback(newSocket);
        }
    }
}
