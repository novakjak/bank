using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

// Singleton managin incoming and outgoing connections
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
        Start();
        Console.WriteLine($"Listening on {_listener.LocalEndPoint.Address}:{_listener.LocalEndPoint.Port}");
        while (IsRunning)
        {
            var conn = await _listener.AcceptNetworkClientAsync(_tokenSource.Token);
            await _connectionLock.WaitAsync();
            var bankConn = new BankConnection(conn, _tokenSource);
            bankConn.Start();
            _connections.Add(bankConn);
            _connectionLock.Release();
        }
        Stop();
        IsRunning = false;
    }

    public async Task Remove(BankConnection conn)
    {
        await _connectionLock.WaitAsync();
        _connections.Remove(conn);
        _connectionLock.Release();
    }

    private void Start()
    {
        _listener.Start();
    }

    private void Stop()
    {
        _listener.Stop();
    }
}
