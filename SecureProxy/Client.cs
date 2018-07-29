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

using SecureProxyServer;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace RProxy.Proxy
{

    /// <summary>References the callback method to be called when the <c>Client</c> object disconnects from the local client and the remote server.</summary>
    /// <param name="client">The <c>Client</c> that has closed its connections.</param>
    public delegate void DestroyDelegate(Client client);

    ///<summary>Specifies the basic methods and properties of a <c>Client</c> object. This is an abstract class and must be inherited.</summary>
    ///<remarks>The Client class provides an abstract base class that represents a connection to a local client and a remote server. Descendant classes further specify the protocol that is used between those two connections.</remarks>
    public abstract class Client : IDisposable
    {
        public bool Secure { get; set; }
        ///<summary>Initializes a new instance of the Client class.</summary>
        ///<param name="ClientSocket">The <see cref ="Socket">Socket</see> connection between this proxy server and the local client.</param>
        ///<param name="Destroyer">The callback method to be called when this Client object disconnects from the local client and the remote server.</param>
        public Client(SocketConnection ClientSocket, DestroyDelegate Destroyer)
        {
            this.ClientSocket = ClientSocket;
            this.Destroyer = Destroyer;
        }
        ///<summary>Initializes a new instance of the Client object.</summary>
        ///<remarks>Both the ClientSocket property and the DestroyDelegate are initialized to null.</remarks>
        public Client()
        {
            this.ClientSocket = null;
            this.Destroyer = null;
        }
        ///<summary>Gets or sets the Socket connection between the proxy server and the local client.</summary>
        ///<value>A Socket instance defining the connection between the proxy server and the local client.</value>
        ///<seealso cref ="DestinationSocket"/>
        internal SocketConnection ClientSocket
        {
            get
            {
                return m_ClientSocket;
            }
            set
            {
                if (m_ClientSocket != null)
                    m_ClientSocket.Dispose();
                m_ClientSocket = value;
            }
        }
        ///<summary>Gets or sets the Socket connection between the proxy server and the remote host.</summary>
        ///<value>A Socket instance defining the connection between the proxy server and the remote host.</value>
        ///<seealso cref ="ClientSocket"/>
        internal SocketConnection DestinationSocket
        {
            get
            {
                return m_DestinationSocket;
            }
            set
            {
                if (m_DestinationSocket != null)
                    m_DestinationSocket.Dispose();
                m_DestinationSocket = value;
            }
        }
        ///<summary>Gets the buffer to store all the incoming data from the local client.</summary>
        ///<value>An array of bytes that can be used to store all the incoming data from the local client.</value>
        ///<seealso cref ="RemoteBuffer"/>
        protected byte[] Buffer
        {
            get
            {
                return m_Buffer;
            }
        }
        ///<summary>Gets the buffer to store all the incoming data from the remote host.</summary>
        ///<value>An array of bytes that can be used to store all the incoming data from the remote host.</value>
        ///<seealso cref ="Buffer"/>
        protected byte[] RemoteBuffer
        {
            get
            {
                return m_RemoteBuffer;
            }
        }
        ///<summary>Disposes of the resources (other than memory) used by the Client.</summary>
        ///<remarks>Closes the connections with the local client and the remote host. Once <c>Dispose</c> has been called, this object should not be used anymore.</remarks>
        ///<seealso cref ="System.IDisposable"/>
        public void Dispose()
        {
            if (this.relayThread != null)
                this.relayThread.Dispose();
        }
        ///<summary>Returns text information about this Client object.</summary>
        ///<returns>A string representing this Client object.</returns>
        public override string ToString()
        {
            try
            {
                return "Incoming connection from " + ((IPEndPoint)DestinationSocket.RemoteEndPoint).Address.ToString();
            }
            catch
            {
                return "Client connection";
            }
        }
        RelayThread relayThread;
        ///<summary>Starts relaying data between the remote host and the local client.</summary>
        ///<remarks>This method should only be called after all protocol specific communication has been finished.</remarks>
        public async Task StartRelay()
        {

            try
            {
                if (ClientSocket.LocalEndPoint.Equals(DestinationSocket.RemoteEndPoint))
                {
                    Dispose();
                }
                else
                {
                    relayThread = new RelayThread(ClientSocket, DestinationSocket, this.Destroyer, this);
                    relayThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] " + ex.Message + "\r\n" + ex.StackTrace);
                Dispose();
            }
        }
        ///<summary>Starts communication with the local client.</summary>
        public abstract Task StartHandshake();
        // private variables
        /// <summary>Holds the address of the method to call when this client is ready to be destroyed.</summary>
        private DestroyDelegate Destroyer;
        /// <summary>Holds the value of the ClientSocket property.</summary>
        private SocketConnection m_ClientSocket;
        /// <summary>Holds the value of the DestinationSocket property.</summary>
        private SocketConnection m_DestinationSocket;
        /// <summary>Holds the value of the Buffer property.</summary>
        private byte[] m_Buffer = new byte[4096]; //0<->4095 = 4096
                                                  /// <summary>Holds the value of the RemoteBuffer property.</summary>
        private byte[] m_RemoteBuffer = new byte[1024];
    }

}