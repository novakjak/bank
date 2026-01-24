using MySql.Data.MySqlClient;

public sealed class MySqlMonitoringStorage : IMonitoringStorage
{
    private readonly string _conn;

    public MySqlMonitoringStorage(string conn)
    {
        _conn = conn;
    }

    private MySqlConnection Open()
    {
        var c = new MySqlConnection(_conn);
        c.Open();
        return c;
    }

    public void SaveSnapshot(SnapshotRecord s)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();

        cmd.CommandText = @"
            insert into snapshots(
                uptime_seconds,
                health_state,
                active_connections,
                total_commands,
                proxy_commands,
                error_count,
                persistence_strategy
            )
            values(@u,@h,@a,@t,@p,@e,@ps)
        ";

        cmd.Parameters.AddWithValue("@u", s.UptimeSeconds);
        cmd.Parameters.AddWithValue("@h", s.HealthState.ToString());
        cmd.Parameters.AddWithValue("@a", s.ActiveConnections);
        cmd.Parameters.AddWithValue("@t", s.TotalCommands);
        cmd.Parameters.AddWithValue("@p", s.ProxyCommands);
        cmd.Parameters.AddWithValue("@e", s.ErrorCount);
        cmd.Parameters.AddWithValue("@ps", s.PersistenceStrategy.ToString());

        cmd.ExecuteNonQuery();
    }

    public void UpdateCommandMetric(CommandMetricRecord m)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();

        cmd.CommandText = @"
            insert into command_metrics(command_name, execution_count, error_count, avg_execution_ms)
            values(@n,@ec,@er,@avg)
            on duplicate key update
                execution_count = execution_count + @ec,
                error_count = error_count + @er,
                avg_execution_ms = 
                    (avg_execution_ms + @avg) / 2
        ";

        cmd.Parameters.AddWithValue("@n", m.CommandName);
        cmd.Parameters.AddWithValue("@ec", m.ExecutionCount);
        cmd.Parameters.AddWithValue("@er", m.ErrorCount);
        cmd.Parameters.AddWithValue("@avg", m.AvgExecutionMs);

        cmd.ExecuteNonQuery();
    }

    public IEnumerable<SnapshotRecord> GetSnapshots(int limit)
    {
        using var c = Open();
        using var cmd = c.CreateCommand();

        cmd.CommandText = @"
            select created_at, uptime_seconds, health_state,
                   active_connections, total_commands,
                   proxy_commands, error_count, persistence_strategy
            from snapshots
            order by created_at desc
            limit @l
        ";
        cmd.Parameters.AddWithValue("@l", limit);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            yield return new SnapshotRecord(
                r.GetDateTime(0),
                r.GetInt64(1),
                Enum.Parse<HealthState>(r.GetString(2)),
                r.GetInt32(3),
                r.GetInt64(4),
                r.GetInt64(5),
                r.GetInt64(6),
                Enum.Parse<PersistenceStrategy>(r.GetString(7))
            );
        }
    }

    public IEnumerable<CommandMetricRecord> GetCommandMetrics()
    {
        using var c = Open();
        using var cmd = c.CreateCommand();

        cmd.CommandText = @"
            select command_name, execution_count, error_count, avg_execution_ms
            from command_metrics
        ";

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            yield return new CommandMetricRecord(
                r.GetString(0),
                r.GetInt64(1),
                r.GetInt64(2),
                r.GetDouble(3)
            );
        }
    }
}
