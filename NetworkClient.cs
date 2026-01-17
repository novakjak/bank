// This file is taken and adapted from my bittorrent client.
// Its intended use is to be an abstraction over TcpClient to allow for
// straightforward unit testing through the use of a mock INetworkClient.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public interface INetworkClient : IDisposable
{
    IPEndPoint IPEndPoint { get; }
    bool Connected { get; }
    ValueTask ConnectAsync(IPAddress address, int port, CancellationToken token);
    Stream GetStream();
    void Close();
}

public class NetworkClient : INetworkClient
{
    private TcpClient _client;

    public IPEndPoint IPEndPoint { get => (IPEndPoint)_client.Client.LocalEndPoint!; }
    public bool Connected { get => _client.Connected; }
    public NetworkClient() => _client = new TcpClient();
    public NetworkClient(TcpClient client) => _client = client;

    public async ValueTask ConnectAsync(IPAddress address, int port, CancellationToken token)
        => await _client.ConnectAsync(address, port, token);

    public Stream GetStream() => _client.GetStream();
    public void Close() => _client.Close();
    public void Dispose() => _client.Dispose();
}
