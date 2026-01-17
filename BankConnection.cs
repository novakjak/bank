using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class BankConnection
{
    public IPAddress? BankIp { get; private set; }
    public IPAddress RealIp { get; private set; }
    public int Port { get; private set; }

    public int TimeoutMs { get; set; }

    private INetworkClient _client;
    private BankStorage _bank = BankStorage.Get();
    private CancellationTokenSource _tokenSource;
    private Task? _connectionTask;

    
    public BankConnection(INetworkClient client, CancellationTokenSource tokenSource)
    {
        _client = client;
        _tokenSource = tokenSource;
        RealIp = _client.IPEndPoint.Address;
        Port = _client.IPEndPoint.Port;
    }
    public BankConnection(IPAddress addr, int port, CancellationTokenSource tokenSource)
    {
        _client = new NetworkClient();
        _tokenSource = tokenSource;
        RealIp = addr;
        Port = port;
    }
    public void Start()
    {
        _connectionTask ??= Task.Run(Run);
    }

    public async Task Run()
    {
        if (!_client.Connected)
            await _client.ConnectAsync(RealIp, Port, _tokenSource.Token);
    }
    
}
