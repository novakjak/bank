using System;
using System.Collections.Generic;
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
    private StreamWriter? _writer;
    private BankStorage _bank = BankStorage.Get();
    private CancellationTokenSource _tokenSource;
    private Task? _connectionTask;
    private int[] _expectedResponses = new int[Enum.GetValues<MsgType>().Count()];

    
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
        Logger.Info($"Started communication with {BankIp ?? RealIp}");
        using var stream = _client.GetStream();
        using var reader = new StreamReader(stream);
        _writer = new StreamWriter(stream);
        var bankCode = new BankCode();
        await SendMessage(bankCode);

        while (Started)
        {
            string line;
            IBankMsg? msg = null;
            bool wasResponse = false;
            try
            {
                line = (await reader.ReadLineAsync(_tokenSource.Token))!.Trim();
            }
            catch (IOException e)
            {
                Logger.Error(e.Message);
                Started = false;
                continue;
            }
            if (line.Length == 0)
                continue;
            for (int i = 0; i < _expectedResponses.Count(); i++)
            {
                if (_expectedResponses[i] == 0 && i != (int)MsgType.ER)
                    continue;
                try
                {
                    var t = (MsgType)i;
                    msg = t.RespFromString(line);
                    _expectedResponses[i] -= 1;
                    wasResponse = true;
                    Logger.Info($"Received response fom {BankIp ?? RealIp} - {msg}");
                    break;
                }
                catch
                {
                    continue;
                }
            }
            try
            {
                if (!wasResponse)
                    msg = line.MsgFromString();
                Logger.Info($"Received message from {BankIp ?? RealIp} - {msg}");

                if (msg!.GetMsgType() == MsgType.ER)
                    continue;
                var resp = msg!.Handle(this);
                await SendMessage(resp);
            }
            catch (Exception e)
            {
                // Do not send error messages on responses
                if (!wasResponse)
                    await SendMessage($"ER {e.Message}");
                Logger.Error(e.Message);
                continue;
            }
        }
        Stop();
    }

    public async Task SendMessage(IBankMsg msg)
    {
        _expectedResponses[(int)msg.GetMsgType()] += 1;
        await SendMessage(msg.ToString()!);
    }
    public async Task SendMessage(string msg)
    {
        var task = _writer?.WriteLineAsync(msg.AsMemory(), _tokenSource.Token);
        if (task is not null)
            await task;
        _writer?.Flush();
        Logger.Info($"Sent message to {BankIp ?? RealIp} - {msg}");
    }

    public void Stop()
    {
        Started = false;
        _connectionTask = null;
        _writer?.Close();
        _client.Close();
        Logger.Info($"Terminated connection with {BankIp ?? RealIp}");
    }
}
