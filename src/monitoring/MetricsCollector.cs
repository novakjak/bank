public sealed class MetricsCollector
{
    private readonly DateTime _start = DateTime.UtcNow;

    private long _total;
    private long _proxy;
    private long _errors;

    private readonly Dictionary<string,(long count,long err,long time)> _cmds = new();

    public void OnCommandStart(string _) { }

    public void OnCommandSuccess(string cmd, long ms)
    {
        _total++;
        Update(cmd, ms, false);
    }

    public void OnCommandError(string cmd, long ms)
    {
        _total++;
        _errors++;
        Update(cmd, ms, true);
    }

    public void OnProxy() => _proxy++;

    private void Update(string cmd, long ms, bool err)
    {
        if (!_cmds.ContainsKey(cmd))
            _cmds[cmd] = (0,0,0);

        var v = _cmds[cmd];
        v.count++;
        if (err) v.err++;
        v.time += ms;
        _cmds[cmd] = v;
    }

    public SnapshotRecord BuildSnapshot(
        int activeConnections,
        PersistenceStrategy strategy
    )
    {
        var up = (long)(DateTime.UtcNow - _start).TotalSeconds;

        var health =
            _errors > 0 ? HealthState.DEGRADED : HealthState.OK;

        return new SnapshotRecord(
            DateTime.UtcNow,
            up,
            health,
            activeConnections,
            _total,
            _proxy,
            _errors,
            strategy
        );
    }

    public IEnumerable<CommandMetricRecord> BuildCommandMetricsAndReset()
    {
        foreach (var c in _cmds)
        {
            yield return new CommandMetricRecord(
                c.Key,
                c.Value.count,
                c.Value.err,
                c.Value.count == 0 ? 0 : (double)c.Value.time / c.Value.count
            );
        }

        _cmds.Clear();
    }

}
