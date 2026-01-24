using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

string conn =
    builder.Configuration.GetConnectionString("mysql")
    ?? throw new Exception("Missing mysql connection string");

app.MapGet("/api/status", () =>
{
    using var c = new MySqlConnection(conn);
    c.Open();

    using var cmd = c.CreateCommand();
    cmd.CommandText = """
        select s.created_at, s.health_state, s.active_connections,
               s.total_commands, s.proxy_commands, s.error_count,
               s.persistence_strategy,
               nc.shutdown_requested
        from snapshots s
        join node_control nc on nc.id = 1
        order by s.created_at desc
        limit 1
    """;

    using var r = cmd.ExecuteReader();
    if (!r.Read())
        return Results.Ok(new { alive = false });

    var createdAt = r.GetDateTime(0);
    var shutdownRequested = r.GetBoolean(7);

    var alive =
        !shutdownRequested &&
        (DateTime.UtcNow - createdAt).TotalSeconds < 15;

    return Results.Ok(new
    {
        alive,
        createdAt,
        health = r.GetString(1),
        connections = r.GetInt32(2),
        total = r.GetInt64(3),
        proxy = r.GetInt64(4),
        errors = r.GetInt64(5),
        persistence = r.GetString(6)
    });
});



app.MapGet("/api/commands", () =>
{
    using var c = new MySqlConnection(conn);
    c.Open();

    using var cmd = c.CreateCommand();
    cmd.CommandText = """
        select command_name, execution_count, error_count, avg_execution_ms
        from command_metrics
    """;

    using var r = cmd.ExecuteReader();
    var list = new List<object>();

    while (r.Read())
    {
        list.Add(new
        {
            name = r.GetString(0),
            count = r.GetInt64(1),
            errors = r.GetInt64(2),
            avg = r.GetDouble(3)
        });
    }

    return Results.Ok(list);
});

app.MapPost("/api/reset-commands", () =>
{
    using var c = new MySqlConnection(conn);
    c.Open();

    using var cmd = c.CreateCommand();
    cmd.CommandText = "delete from command_metrics";
    cmd.ExecuteNonQuery();

    return Results.Ok();
});

app.MapPost("/api/shutdown", () =>
{
    using var c = new MySqlConnection(conn);
    c.Open();

    using var cmd = c.CreateCommand();
    cmd.CommandText = """
        update node_control
        set shutdown_requested = true
        where id = 1
    """;

    cmd.ExecuteNonQuery();
    return Results.Ok();
});

app.Run();
