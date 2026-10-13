using ArenaStorm.Server;
using ArenaStorm.Shared.Constants;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    int port = NetworkConstants.GameServerPort;
    if (args.Length > 0 && int.TryParse(args[0], out int parsedPort))
    {
        port = parsedPort;
    }
    else if (int.TryParse(Environment.GetEnvironmentVariable("GAME_SERVER_PORT"), out int envPort))
    {
        port = envPort;
    }
    else if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out int altPort))
    {
        port = altPort;
    }

    Log.Information("ArenaStorm authoritative server starting on port {Port}...", port);

    var server = new GameServer();

    Console.CancelKeyPress += (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        Log.Information("Shutdown requested via Console.CancelKeyPress. Stopping server...");
        server.Stop();
    };

    server.Start(port);
    server.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ArenaStorm server terminated unexpectedly.");
}
finally
{
    Log.Information("ArenaStorm server shutdown finished.");
    Log.CloseAndFlush();
}
