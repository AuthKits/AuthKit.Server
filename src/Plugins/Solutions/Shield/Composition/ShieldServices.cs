using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shield.Detection;
using Shield.Detection.Sensors;
using Shield.Options;
using Shield.State;

namespace Shield.Composition;

/// <summary>
/// Composition root for Shield services: validated options, shared state,
/// and detector strategies in evaluation order.
/// </summary>
public static class ShieldServices
{
    /// <summary>
    /// Registers Shield into the host DI container.
    /// </summary>
    public static IServiceCollection AddShield(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ShieldOptions>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IShieldState, ShieldState>();
        services.AddSingleton<IAttackDetector, AttackSignatureDetector>();
        services.AddSingleton<IAttackDetector, SuspiciousUserAgentDetector>();
        services.AddSingleton<IAttackDetector, OversizedRequestDetector>();
        services.AddSingleton<IAttackDetector, RateFloodDetector>();
        services.AddSingleton<IAttackDetector, AuthBruteForceDetector>();
        services.AddSingleton<IAttackDetector, EndpointScannerDetector>();

        return services;
    }
}
