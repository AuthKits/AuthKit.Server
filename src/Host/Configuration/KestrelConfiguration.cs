using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Host.Configuration;

/// <summary>
/// Provides configuration helpers for the Kestrel web server.
/// </summary>
/// <remarks>
/// <para>
/// Configures HTTPS listeners for the REST and gRPC endpoints using a
/// certificate and connection settings supplied through environment variables.
/// </para>
/// <para>REST supports both HTTP/1.1 and HTTP/2, while the gRPC endpoint is restrictedto HTTP/2.</para>
/// <para>When configuration environment variables are not provided, development defaults are used.</para>
/// </remarks>
public static class KestrelConfiguration
{
    /// <summary>
    /// Configures Kestrel with HTTPS listeners for the REST and gRPC endpoints.
    /// </summary>
    /// <param name="webHost">The web host builder used to configure Kestrel.</param>
    /// <remarks>
    /// Reads the certificate path, certificate password, and REST and gRPC ports
    /// from the corresponding environment variables. Missing values fall back to
    /// development defaults.
    /// </remarks>
    public static void ConfigureKestrelServer(this IWebHostBuilder webHost)
    {
        var (certPath, certPassword, portRest, portGrpc) = LoadSettings();
        ValidateCertificate(certPath, certPassword);
        ConfigureListeners(webHost, certPath, certPassword, portRest, portGrpc);

        Console.ForegroundColor = ConsoleColor.Green;
        var protocol = File.Exists(certPath) ? "HTTPS" : "HTTP";
        Console.WriteLine($"✅ Kestrel configured ({protocol}): REST={portRest}, gRPC={portGrpc}, Cert={certPath}");
        Console.ResetColor();
    }

    #region Helper Methods

    /// <summary>
    /// Loads Kestrel certificate and endpoint settings from environment variables.
    /// </summary>
    /// <returns>
    /// A tuple containing the certificate path, certificate password,
    /// REST port, and gRPC port.
    /// </returns>
    private static (string certPath, string certPassword, int portRest, int portGrpc) LoadSettings()
    {
        var certPath = Environment.GetEnvironmentVariable("DEV_CERT_PATH") ?? "/root/certs/devcert.pfx";
        var certPassword = Environment.GetEnvironmentVariable("DEV_CERT_PASSWORD") ?? string.Empty;
        var portRest = int.TryParse(Environment.GetEnvironmentVariable("DEV_CERT_PORT_REST"), out var p1) ? p1 : 5000;
        var portGrpc = int.TryParse(Environment.GetEnvironmentVariable("DEV_CERT_PORT_GRPC"), out var p2) ? p2 : 5001;

        return (certPath, certPassword, portRest, portGrpc);
    }

    /// <summary>
    /// Validates the configured HTTPS certificate settings and writes warnings
    /// for missing certificate files or passwords.
    /// </summary>
    /// <param name="certPath">Path to the HTTPS certificate file.</param>
    /// <param name="certPassword">Password used to load the certificate.</param>
    private static void ValidateCertificate(string certPath, string certPassword)
    {
        if (!File.Exists(certPath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"⚠️  Certificate not found at: {certPath}. Falling back to HTTP.");
            Console.ResetColor();
        }

        if (string.IsNullOrWhiteSpace(certPassword))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  DEV_CERT_PASSWORD not set — using empty password for certificate.");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Configures the Kestrel listeners for REST and gRPC traffic.
    /// </summary>
    /// <param name="webHost">The web host builder used to configure Kestrel.</param>
    /// <param name="certPath">Path to the HTTPS certificate file.</param>
    /// <param name="certPassword">Password used to load the HTTPS certificate.</param>
    /// <param name="portRest">Port used by the REST endpoint.</param>
    /// <param name="portGrpc">Port used by the gRPC endpoint.</param>
    private static void ConfigureListeners(
        IWebHostBuilder webHost,
        string certPath,
        string certPassword,
        int portRest,
        int portGrpc)
    {
        bool useHttps = File.Exists(certPath);

        webHost.ConfigureKestrel(options =>
        {
            // REST (HTTP/1 + HTTP/2)
            options.ListenAnyIP(portRest, listenOptions =>
            {
                if (useHttps)
                {
                    listenOptions.UseHttps(certPath, certPassword);
                }
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            });

            // gRPC (HTTP/2 only)
            options.ListenAnyIP(portGrpc, listenOptions =>
            {
                if (useHttps)
                {
                    listenOptions.UseHttps(certPath, certPassword);
                }
                else
                {
                    // gRPC requires HTTP/2, which on some platforms requires HTTPS or specific configuration
                    // for ClearText.
                }
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        });
    }

    #endregion
}
