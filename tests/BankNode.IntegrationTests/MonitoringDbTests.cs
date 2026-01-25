using Xunit;
using MySql.Data.MySqlClient;

public class MonitoringDbTests
{
    private const string Conn =
        "server=localhost;database=bank;user=root;password=student;";

    [Fact]
    public void Snapshot_IsWrittenToDatabase()
    {
        using var c = new MySqlConnection(Conn);
        c.Open();

        using var cmd = c.CreateCommand();
        cmd.CommandText = """
            insert into snapshots(
                uptime_seconds,
                health_state,
                active_connections,
                total_commands,
                proxy_commands,
                error_count,
                persistence_strategy
            )
            values (10,'OK',1,5,1,0,'MYSQL')
        """;

        var rows = cmd.ExecuteNonQuery();
        Assert.Equal(1, rows);
    }

    [Fact]
    public void CommandMetrics_CanBeRead()
    {
        using var c = new MySqlConnection(Conn);
        c.Open();

        using var cmd = c.CreateCommand();
        cmd.CommandText = "select count(*) from command_metrics";

        var count = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.True(count >= 0);
    }
}
