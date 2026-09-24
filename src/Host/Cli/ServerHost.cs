using Host.Plugins.Loading;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace Host.Cli;

/// <summary>
/// Background service responsible for displaying AuthKit server startup information
/// and maintaining the server host lifetime.
/// </summary>
/// <remarks>
/// <para>
/// Displays the configured environment, listening endpoint, ports, issuer,
/// and loaded plugin information when the server starts.
/// The service remains active until the host requests shutdown through the
/// provided <see cref="CancellationToken"/>.
/// </para>
/// </remarks>
public sealed class ServerHost(
    IOptions<AuthKitServerOptions> opts,
    IHostEnvironment env,
    IReadOnlyList<LoadedPlugin> plugins) : BackgroundService
{
    private readonly AuthKitServerOptions _opts = opts.Value;

    /// <summary>
    /// Executes the server host background service.
    /// </summary>
    /// <param name="stoppingToken">Token that is signaled when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        AnsiConsole.Write(new FigletText("AuthKit")
            .Color(Color.Green)
            .Centered());

        var restPort = Environment.GetEnvironmentVariable("DEV_CERT_PORT_REST") ?? "5000";
        var grpcPort = Environment.GetEnvironmentVariable("DEV_CERT_PORT_GRPC") ?? "5001";

        var info = new Panel(
            $"""
             [bold blue]Environment:[/] {env.EnvironmentName}
             [bold blue]Listening on:[/] {_opts.Host}
             [bold blue]REST port:[/] {restPort}   [bold blue]gRPC port:[/] {grpcPort}
             [bold yellow]Issuer:[/] {_opts.Issuer}
             [grey]Started at {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}[/]
             """)
        {
            Border = BoxBorder.Double,
            Padding = new Padding(1, 1),
            Header = new PanelHeader("Server Started")
        };
        AnsiConsole.Write(info);

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[green]Plugin[/]");
        table.AddColumn("[yellow]Version[/]");
        table.AddColumn("[grey]Description[/]");
        table.AddColumn("[cyan]Middleware[/]");
        table.AddColumn("[magenta]Security schemes[/]");

        if (plugins.Count == 0)
        {
            table.AddRow("[grey]none loaded[/]", "-", "-", "-", "-");
        }
        else
        {
            foreach (var lp in plugins)
            {
                var description = lp.Plugin.Description ?? "-";
                var middlewares = lp.Plugin.Middlewares ?? [];
                var middleware = middlewares.Count > 0
                    ? string.Join(", ", middlewares.Select(m => m.Name ?? m.MiddlewareType?.Name ?? "?"))
                    : "-";
                var schemes = lp.Plugin.GetSecuritySchemes();
                var schemeNames = schemes.Count > 0 ? string.Join(", ", schemes.Keys) : "-";

                table.AddRow(
                    $"[bold]{lp.Plugin.Name}[/]",
                    $"v{lp.Plugin.Version}",
                    description,
                    middleware,
                    schemeNames);
            }
        }

        AnsiConsole.Write(new Panel(table)
        {
            Border = BoxBorder.Rounded,
            Header = new PanelHeader($"Plugins ({plugins.Count})")
        });

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }

        AnsiConsole.MarkupLine("[red]Server shutting down...[/]");
    }
}
