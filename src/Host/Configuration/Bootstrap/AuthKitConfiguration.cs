using Core.KeyManagement.Interfaces;
using Core.KeyManagement.Services;
using Core.TokenKeyBindings.Interfaces;
using Core.TokenKeyBindings.Services;
using Host.KeyManagement.Repositories;
using Host.KeyManagement.Security;
using Host.Restful.Middleware.Exceptions;
using Host.TokenKeyBindings.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Host.Configuration.Bootstrap;

/// <summary>
/// Provides dependency injection configuration for AuthKit core services.
/// </summary>
/// <remarks>
/// <para>
/// Registers plugin-independent services used by AuthKit for JWT signing key
/// management and token-to-signing-key bindings.
/// </para>
/// <para>
/// These services can be shared by multiple authentication plugins that need
/// to issue, sign, validate, or associate tokens with cryptographic keys.
/// </para>
/// <para>
/// The configuration also registers a custom authorization middleware result
/// handler responsible for producing consistent authorization responses.
/// </para>
/// </remarks>
public static class AuthKitConfiguration
{
    /// <summary>
    /// Registers AuthKit core services with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection used to register AuthKit dependencies.</param>
    /// <remarks>
    /// <para>
    /// Registers the HTTP context accessor, authorization result handler, JWT
    /// signing key infrastructure, encrypted key store services, and token key
    /// binding services.
    /// </para>
    /// <para>
    /// The AES master key used to protect persisted key material is read from the
    /// <c>Encryption:AES_MASTER_KEY</c> configuration value.
    /// </para>
    /// </remarks>
    public static void AddAuthKitCore(
        this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddSingleton<
            IAuthorizationMiddlewareResultHandler,
            CustomAuthorizationMiddlewareResultHandler>();

        services.AddSingleton<IKeyEncryptor>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();

            var key = configuration["Encryption:AES_MASTER_KEY"]
                ?? throw new InvalidOperationException(
                    "Missing AES_MASTER_KEY in configuration.");

            return new AesKeyEncryptor(key);
        });

        services.AddSingleton<IKeyGenerator, RsaKeyGenerator>();
        services.AddSingleton<IKeyStoreRepository, KeyStoreRepository>();
        services.AddSingleton<IJwtKeyStore, JwtKeyStore>();

        services.AddHostedService<JwtKeyStoreInitializer>();
        services.AddSingleton<IKeyBindingRepository, KeyBindingRepository>();
        services.AddSingleton<IKeyBindingService,KeyBindingService>();
    }
}
