// The structure of this class is inspired by PeerConnection in my bittorrent project
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class BankConnection
{
    public static string Code { get; } = NetworkListener.GetLocalAddr().ToString();

    public IPAddress? BankIp { get; set; }
    public IPAddress RealIp { get; private set; }
    public int Port { get; private set; }

    public bool Started { get; set; } = false;

    private int _timeout;
    public int TimeoutMs
    {
        get => _timeout;
        set {
            if (value < 0)
                return;
            _timeout = value;
            _client.SendTimeout = value;
            _client.ReceiveTimeout = value;
            _client.GetStream().ReadTimeout = value;
            _client.GetStream().WriteTimeout = value;
        }
    }

    private INetworkClient _client;
    private StreamWriter? _writer;
    private BankStorage _bank = BankStorage.Get();
    private CancellationTokenSource _tokenSource;
    private Task? _connectionTask;
    private int[] _expectedResponses = new int[Enum.GetValues<MsgType>().Count()];

    
    public BankConnection(INetworkClient client, CancellationTokenSource tokenSource)
    {
        _client = client;
        TimeoutMs = Config.Get().Timeout * 1000;
        _tokenSource = CancellationTokenSource
            .CreateLinkedTokenSource(tokenSource.Token);
        RealIp = _client.IPEndPoint.Address;
        Port = _client.RemoteEndPoint.Port;
    }
    public BankConnection(IPAddress addr, int port, CancellationTokenSource tokenSource)
    {
        _client = new NetworkClient();
        TimeoutMs = Config.Get().Timeout * 1000;
        _tokenSource = CancellationTokenSource
            .CreateLinkedTokenSource(tokenSource.Token);
        RealIp = addr;
        Port = port;
    }
    public void Start()
    {
        _tokenSource.TryReset();
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
                var timeout = CancellationTokenSource
                    .CreateLinkedTokenSource(_tokenSource.Token);
                timeout.CancelAfter(TimeoutMs);
                line = (await reader.ReadLineAsync(timeout.Token))!.Trim();
            }
            catch (OperationCanceledException)
            {
                Logger.Info($"Timeout with {BankIp ?? RealIp} expired");
                Started = false;
                continue;
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
            if (wasResponse)
            {
                try
                {
                    if (msg!.GetMsgType() == MsgType.ER)
                        continue;
                    msg.Handle(this);
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                }
                continue;
            }
            try
            {
                msg = line.MsgFromString();
                Logger.Info($"Received message from {BankIp ?? RealIp} - {msg}");

                var resp = msg.Handle(this);
                if (resp is not null)
                    await SendMessage(resp);
            }
            catch (Exception e)
            {
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
        _tokenSource.Cancel();
        _connectionTask = null;
        _writer?.Close();
        _client.Close();
        Logger.Info($"Terminated connection with {BankIp ?? RealIp}");
    }
}
