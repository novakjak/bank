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
    public const int MIN_PORT = 65525;
    public const int MAX_PORT = 65535;
    public static string Code { get; } = NetworkListener.GetLocalAddr().ToString();

    public event EventHandler<(IPAddress addr, int port, IBankMsg msg)>? ReceivedProxyResponse;
    public event EventHandler<(IPAddress fromBank, IPAddress toBank, IBankMsg msg)>? ProxyRequest;
    public event EventHandler? ConnectionTerminated;

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
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private BankStorage _bank = BankStorage.Get();
    private CancellationTokenSource _tokenSource;
    private Task? _connectionTask;
    private SemaphoreSlim _respLock = new(1);
    private int[] _expectedResponses = new int[Enum.GetValues<MsgType>().Count()];
    private List<(IPAddress addr, int port, MsgType type)> _expectedProxyResponses = new();

    
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

    public void Stop()
    {
        ConnectionTerminated?.Invoke(this, EventArgs.Empty);
        Started = false;
        _tokenSource.Cancel();
        _connectionTask = null;
        _writer?.Close();
        _reader?.Close();
        _client.Close();
        Logger.Info($"Terminated connection with {BankIp ?? RealIp}");
    }

    private async Task Run()
    {
        if (!_client.Connected)
            await _client.ConnectAsync(RealIp, Port, _tokenSource.Token);
        Logger.Info($"Started communication with {BankIp ?? RealIp}");
        var stream = _client.GetStream();
        _reader = new StreamReader(stream);
        _writer = new StreamWriter(stream);
        await SendMessage(new BankCode());

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
                line = (await _reader!.ReadLineAsync(timeout.Token))!.Trim();
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
                var t = (MsgType)i;
                if (_expectedResponses[i] == 0
                    && _expectedProxyResponses.Count(r => r.type == t) == 0
                    && i != (int)MsgType.ER)
                {
                    continue;
                }
                try
                {
                    msg = t.RespFromString(line);
                    if (await TryProxyResponseBack(t, msg))
                        continue;
                    await _respLock.WaitAsync();
                    _expectedResponses[i] -= 1;
                    _respLock.Release();
                    wasResponse = true;
                    Logger.Info($"Received response from {BankIp ?? RealIp} - {msg}");
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
                    await SendResponse(resp);
            }
            catch (Exception e)
            {
                await SendResponse($"ER {e.Message}");
                Logger.Error(e.Message);
                continue;
            }
        }
        Stop();
    }

    public async Task<bool> TryProxyResponseBack(MsgType type, IBankMsg msg)
    {
        // When receiving a response it's not possible to accurately
        // judge whether the response was for a message sent by this
        // node  or if it was proxied and if it should be sent back.
        // Thus if a response to a proxied message is expected, it
        // is sent back even though it might've been a response
        // sent by to a message from this node. Errors are even worse.
        // Since error messages are not labeled in any way, it's
        // impossible to tell for which message the error is meant.
        // Errors are sent to whoever is waiting on more responses.
        var i = (int)type;

        await _respLock.WaitAsync();
        try
        {
            var expectedProxy = _expectedProxyResponses.Count();
            var expectedProxyOfType = _expectedProxyResponses.Count(r => r.type == type);
            var expected = _expectedResponses.Sum();
            var msgDiff = expectedProxy - expected;
            if (expectedProxyOfType > 0 || (type == MsgType.ER && msgDiff > 0))
            {
                var idx = _expectedProxyResponses.FindIndex(r => r.type == type);
                var addr = _expectedProxyResponses[idx].addr;
                var port = _expectedProxyResponses[idx].port;
                _expectedProxyResponses.RemoveAt(idx);
                ReceivedProxyResponse?.Invoke(this, (addr, port, msg));
                Logger.Info($"Received response from {BankIp ?? RealIp} to proxied message from {addr} - {msg}");
                return true;
            }
        }
        finally
        {
            _respLock.Release();
        }
        return false;
    }

    public void RaiseProxyRequest(IPAddress to, IBankMsg msg)
        => ProxyRequest?.Invoke(this, (NetworkListener.LocalAddr, to, msg));

    public async Task Proxy(IPAddress fromAddr, IBankMsg msg)
    {
        await _respLock.WaitAsync();
        _expectedProxyResponses.Add((fromAddr, Port, msg.GetMsgType()));
        _respLock.Release();
        await SendMessage(msg!.ToString());
    }
    public async Task ProxyResponse(IBankMsg resp)
        => await SendResponse(resp!.ToString());

    private async Task SendMessage(IBankMsg msg)
    {
        await _respLock.WaitAsync();
        _expectedResponses[(int)msg.GetMsgType()] += 1;
        _respLock.Release();
        await SendMessage(msg!.ToString());
    }
    private async Task SendMessage(string msg)
    {
        await _writer!.WriteLineAsync(msg.AsMemory(), _tokenSource.Token);
        _writer?.Flush();
        Logger.Info($"Sent message to {BankIp ?? RealIp} - {msg}");
    }
    private async Task SendResponse(IBankMsg msg)
        => await SendResponse(msg!.ToString());
    private async Task SendResponse(string resp)
    {
        await _writer!.WriteLineAsync(resp.AsMemory(), _tokenSource.Token);
        _writer?.Flush();
        Logger.Info($"Sent response to {BankIp ?? RealIp} - {resp}");
    }
}
