# SecureProxy

CLIENT.RAW_DATA => SecureProxy.PORT_MAP.ENCRYPTED_DATA => GATEWAY/FIREWALL => SecureProxy.SOCK5_HANDLER.ENCRYPTED_DATA => SecureProxy.SOCK5_CLIENT.RAWDATA => INTERNET

# SecureProxy Client
runs on client as a proxy, actually it use port mapping transfer data with encryption and decryption methods,
simply encrypt data and send it to SecureProxy Server then recv data from SecureProxy Server and decrypt it for client


# SecureProxy Server

 runs on the other side of GATEWAY/FIREWALL , its close to the internet side that you want access.
 as a costomer defined sock5 proxy, it will decrypt or encypt data when recv from and send to GATEWAY/FIREWALL.

# Configuration

## application configuration
client:
` <listener type="RProxy.Proxy.PortMap.PortMapListener" value="host:0.0.0.0;int:12345;host:YOUR_SERVER;int:65530" /> `

server:
` <listener type="RProxy.Proxy.Socks.SocksListener" value="host:0.0.0.0;int:65530;authlist" /> `

this config is for mapping request from client 12345 port to  YOUR_SERVER's 65530 port.

## system proxy configuration
 config 127.0.0.1:12345 as system's sock5 proxy. if sock5 proxy is not supported by system, try firefox and select sock5 at it's network setting page.
 
# Install as service
## ubuntu 16.04
`wget -N https://raw.githubusercontent.com/kissstudio/SecureProxy/master/SecureProxy/install.sh && ./install.h

## windows
:only executable provided for now.

# How it's secrue
developer can re-define the encrypt/decrypt method by editing class SecureStream.
the request/response data stucture is invisible to GATEWAY/FIREWALL due to the encrypt/decrypt method.


