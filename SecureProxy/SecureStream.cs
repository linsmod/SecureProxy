using System;
using System.Collections.Generic;
using System.Text;

namespace SecureProxyServer
{
    class SecureStream
    {
        public static void Encrypt(byte[] buffer, int offset, int length)
        {
            for (int i = offset; i < offset + length; i++)
            {
                buffer[i] ^= (byte)(DateTime.UtcNow.Day * 8);
            }
        }

        public static void Decrypt(byte[] buffer, int offset, int length)
        {
            for (int i = offset; i < offset + length; i++)
            {
                buffer[i] ^= (byte)(DateTime.UtcNow.Day * 8);
            }
        }
    }
}
