// This file is taken and adapted from my bittorrent client.
// Its intended use is to be an abstraction over TcpListener to allow for
// straightforward unit testing through the use of a mock INetworkListener.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;

public interface INetworkListener : IDisposable
{
    public IPEndPoint LocalEndPoint { get; }
    static abstract INetworkListener Create(int port);
    Task<INetworkClient> AcceptNetworkClientAsync(CancellationToken token);
    void Start();
    void Stop();
}

public class NetworkListener : INetworkListener, IDisposable
{
    public static IPAddress LocalAddr { get; } = NetworkListener.GetLocalAddr();
    private TcpListener _listener;

    public IPEndPoint LocalEndPoint { get => (_listener.LocalEndpoint as IPEndPoint)!; }
    
    private NetworkListener(TcpListener listener) => _listener = listener;

    public static INetworkListener Create(int port)
    {
        var listener = new TcpListener(NetworkListener.LocalAddr, port);
        return new NetworkListener(listener);
    }

    public static INetworkListener CreateWithinRange(int min_port, int max_port)
    {
        if (min_port > max_port)
            throw new ArgumentException("min_port is greater that max_port");
        INetworkListener? listener = null;
        ExceptionDispatchInfo? lastEx = null;
        for (int port = min_port; port <= max_port; port++)
        {
            try
            {
                listener = NetworkListener.Create(port);
                listener.Start();
                break;
            }
            catch (SocketException e)
            {
                lastEx = ExceptionDispatchInfo.Capture(e);
                Logger.Debug($"Port {port} is already in use; skipping");
                continue;
            }
            finally
            {
                listener?.Dispose();
            }
        }
        if (listener is null && lastEx is not null)
            lastEx.Throw();

        return listener!;
    }

    public async Task<INetworkClient> AcceptNetworkClientAsync(CancellationToken token)
        => new NetworkClient(await _listener.AcceptTcpClientAsync(token));

    public void Start() => _listener.Start();
    public void Stop() => _listener.Stop();
    public void Dispose() => _listener.Dispose();

    public static IPAddress GetLocalAddr()
    {
        using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Unspecified);
        // 240.0.0.1 is reserved and unreachable, this was chosen by design.
        // We just need to create a socket on an available interface.
        sock.Connect("240.0.0.1", 1);
        return (sock.LocalEndPoint as IPEndPoint)!.Address;
    }
}
