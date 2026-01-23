// The structure of this class is heavily inspired by TorrentTask in my bittorrent project

using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// Singleton managing incoming and outgoing connections
public class ConnectionManager
{
    private static ConnectionManager? _instance;

    public bool IsRunning { get; private set; } = false;

    private INetworkListener _listener;
    private CancellationTokenSource _tokenSource;
    private List<BankConnection> _connections = new();
    // Using SemaphoreSlim instead of Lock because SemaphoreSlim has async methods
    private SemaphoreSlim _connectionLock = new(1); 

    private ConnectionManager(INetworkListener listener, CancellationTokenSource tokenSource)
    {
        _listener = listener;
        _tokenSource = tokenSource;
    }

    public static ConnectionManager Get()
    {
        if (_instance is null)
            throw new InvalidOperationException("Connection manager has not yet been initialized");
        return _instance;
    }
    public static void Init(INetworkListener listener, CancellationTokenSource tokenSource)
    {
        if (_instance is not null)
            throw new InvalidOperationException("Connection manager has already been initialized");
        _instance = new ConnectionManager(listener, tokenSource);
    }

    public async Task Run()
    {
        IsRunning = true;
        _listener.Start();
        Logger.Info($"Listening on {_listener.LocalEndPoint.Address}:{_listener.LocalEndPoint.Port}");
        while (IsRunning)
        {
            var conn = await _listener.AcceptNetworkClientAsync(_tokenSource.Token);
            Logger.Info($"Accepted connection from {conn.IPEndPoint.Address}");
            var bankConn = new BankConnection(conn, _tokenSource);
            await AddConnection(bankConn);
        }
        _listener.Stop();
    }

    private async Task AddConnection(BankConnection conn)
    {
        conn.ConnectionTerminated += async (_, _) => await RemoveConnection(conn);
        conn.ProxyRequest += async (_, msg) => await Proxy(msg.fromBank, msg.toBank, msg.msg);
        conn.ReceivedProxyResponse += async (_, resp) => await ProxyResponse(resp.addr, resp.msg);
        conn.Start();
        await _connectionLock.WaitAsync();
        _connections.Add(conn);
        _connectionLock.Release();
    }
    private async Task RemoveConnection(BankConnection conn)
    {
        await _connectionLock.WaitAsync();
        _connections.Remove(conn);
        _connectionLock.Release();
    }

    /// <summary>
    /// Get a connected node or try to connect to one if none is found
    /// </summary>
    /// <returns>
    /// Returns a list of connections. It can return
    /// multiple remote hosts that all accepted a connection in the port range
    /// of 65525 - 65535. Some of these connections might not be
    /// for a bank node but it's impossible to tell because if we were to send
    /// a BC message and then wait for a response the other side might terminate
    /// the connection without us even sending the proxied message.
    /// </returns>
    private async Task<List<BankConnection>> GetOrConnectTo(IPAddress addr)
    {
        await _connectionLock.WaitAsync();
        try
        {
            var conns = _connections.Where(c => c?.BankIp == addr);
            if (conns.Count() > 0)
                return conns.ToList();
        }
        finally
        {
            _connectionLock.Release();
        }
        var foundConns = new List<BankConnection>();
        for (int port = BankConnection.MIN_PORT; port < BankConnection.MAX_PORT; port++)
        {
            try
            {
                var client = new NetworkClient();
                await client.ConnectAsync(addr, port, _tokenSource.Token);
                var conn = new BankConnection(client, _tokenSource);
                await AddConnection(conn);
                foundConns.Add(conn);
            }
            catch
            {
                continue;
            }
        }
        return foundConns;
    }

    private async Task Proxy(IPAddress fromBank, IPAddress toBank, IBankMsg msg)
    {
        var conns = await GetOrConnectTo(toBank);
        foreach (var conn in conns)
            await conn.Proxy(fromBank, msg);
    }

    private async Task ProxyResponse(IPAddress toBank, IBankMsg msg)
    {
        var conns = await GetOrConnectTo(toBank);
        foreach (var conn in conns)
            await conn.ProxyResponse(msg);
    }

    ~ConnectionManager()
    {
        IsRunning = false;
        _tokenSource.Cancel();
        _listener.Dispose();
    }
}
