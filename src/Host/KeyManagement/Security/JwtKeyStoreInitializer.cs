using System.Diagnostics;
using Core.KeyManagement.Interfaces;
using Spectre.Console;

namespace Host.KeyManagement.Security;

/// <summary>
/// Initializes and disposes the JWT key store as part of the application host lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// The initializer resolves <see cref="IJwtKeyStore"/> from a scoped service
/// provider and initializes its persisted key material before the application
/// begins serving requests.
/// </para>
/// <para>
/// Initialization progress is displayed using a Spectre.Console status spinner,
/// while the resulting initialization duration, active key identifier, and
/// number of available public keys are written to the application logger.
/// </para>
/// <para>
/// During application shutdown, the hosted service disposes the key store when
/// it implements <see cref="IAsyncDisposable"/>, allowing cryptographic resources
/// such as RSA instances to be released safely.
/// </para>
/// </remarks>
public sealed class JwtKeyStoreInitializer(
    IServiceProvider provider,
    ILogger<JwtKeyStoreInitializer> logger)
    : IHostedService, IAsyncDisposable
{
    private IJwtKeyStore? _store;

    /// <summary>
    /// Initializes the JWT key store during application startup.
    /// </summary>
    /// <param name="cancellationToken">Token that can be used to signal cancellation of the startup operation.</param>
    /// <remarks>
    /// <para>
    /// The <see cref="IJwtKeyStore"/> is registered as a singleton, so it is
    /// resolved directly from the provider and held for the lifetime of the
    /// initializer so its cryptographic resources can be released on shutdown.
    /// </para>
    /// <para>
    /// The initialization duration is measured using
    /// <see cref="Stopwatch"/> and reported after the key store has been
    /// successfully initialized.
    /// </para>
    /// </remarks>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var store = provider.GetRequiredService<IJwtKeyStore>();
        _store = store;

        var stopwatch = Stopwatch.StartNew();

        if (System.Console.IsOutputRedirected)
        {
            await store.InitializeAsync();
        }
        else
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("green"))
                .StartAsync(
                    "Initializing JWT KeyStore...",
                    async _ => await store.InitializeAsync());
        }

        stopwatch.Stop();

        var activeCredentials =
            store.GetActiveSigningCredentials();

        var activeKid =
            store.GetMetadata(activeCredentials.Key.KeyId)?.Kid ?? "N/A";

        var totalKeys =
            store.GetPublicJwks().Count();

        logger.LogInformation(
            "JWT Keystore initialized in {ElapsedMilliseconds} ms | Active KID: {ActiveKid} | Total Keys: {TotalKeys}",
            stopwatch.ElapsedMilliseconds,
            activeKid,
            totalKeys);
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
    
    public async ValueTask DisposeAsync()
    {
        if (_store is IAsyncDisposable asyncStore)
            await asyncStore.DisposeAsync();
    }
}