using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class BankConnection
{
    public static string Code { get; } = NetworkListener.GetLocalAddr().ToString();

    public IPAddress? BankIp { get; private set; }
    public IPAddress RealIp { get; private set; }
    public int Port { get; private set; }

    public bool Started { get; set; } = false;

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
        Started = true;
        _connectionTask ??= Task.Run(Run, _tokenSource.Token);
    }

    public async Task Run()
    {
        if (!_client.Connected)
            await _client.ConnectAsync(RealIp, Port, _tokenSource.Token);
        using var stream = _client.GetStream();
        using var reader = new StreamReader(stream);
        using var writer = new StreamWriter(stream);
        while (Started)
        {
            string line;
            IBankMsg msg;
            try
            {
                line = (await reader.ReadLineAsync(_tokenSource.Token))!.Trim();
            }
            catch (IOException e)
            {
                Console.Error.WriteLine(e.Message);
                Started = false;
                continue;
            }
            if (line.Length == 0)
                continue;
            try
            {
                msg = line.MsgFromString();
                Console.WriteLine(msg);
                Console.WriteLine(msg.GetType().Name);
                var resp = msg.Handle(this);
                await writer.WriteLineAsync(resp.ToString().AsMemory(), _tokenSource.Token);
                writer.Flush();
            }
            catch (Exception e)
            {
                await writer.WriteLineAsync($"ER {e.Message}".AsMemory(), _tokenSource.Token);
                writer.Flush();
                Console.Error.WriteLine(e.StackTrace);
                continue;
            }
        }
        Stop();
    }

    public void Stop()
    {
        Started = false;
        _connectionTask = null;
        _client.Close();
    }
}
