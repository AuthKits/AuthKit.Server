using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuthKit.PluginContractValidator.Core;
using AuthKit.Plugins.Abstractions.Contracts;
using Microsoft.AspNetCore.Http;

namespace AuthKit.PluginContractValidator.Rules;

/// <summary>
/// Ensures a contributed middleware type follows the AuthKit middleware convention.
/// </summary>
/// <remarks>
/// The middleware type must expose a constructor accepting
/// <see cref="RequestDelegate"/> and a public <c>InvokeAsync</c> method
/// whose first parameter is <see cref="HttpContext"/> and whose return type is
/// <see cref="Task"/>.
/// </remarks>
public sealed class MiddlewareRule : IPluginContractRule
{
    /// <summary>Gets the rule name ("Middleware").</summary>
    public string Name => "Middleware";

    /// <summary>
    /// Validates the middleware type contributed by
    /// <see cref="IAuthKitPlugin.MiddlewareType"/>, if any.
    /// </summary>
    public Task<IReadOnlyList<string>> ValidateAsync(
        LoadedPlugin plugin,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var middlewareType = plugin.Instance.MiddlewareType;

        if (middlewareType is null)
            return Task.FromResult<IReadOnlyList<string>>(errors);

        if (!HasRequestDelegateConstructor(middlewareType))
        {
            errors.Add($"middleware: MiddlewareType '{middlewareType.Name}' " +
                "must have constructor accepting RequestDelegate.");
        }

        var invokeMethod = middlewareType.GetMethod(
            "InvokeAsync",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);

        if (invokeMethod is null)
        {
            errors.Add($"middleware: MiddlewareType '{middlewareType.Name}' " +
                "must expose public instance InvokeAsync method.");

            return Task.FromResult<IReadOnlyList<string>>(errors);
        }

        if (invokeMethod.ReturnType != typeof(Task))
        {
            errors.Add($"middleware: InvokeAsync on '{middlewareType.Name}' " + 
                "must return Task.");
        }

        var parameters = invokeMethod.GetParameters();

        if (parameters.Length == 0 ||
            parameters[0].ParameterType != typeof(HttpContext))
        {
            errors.Add($"middleware: InvokeAsync on '{middlewareType.Name}' "
                + "must take HttpContext as its first parameter.");
        }

        return Task.FromResult<IReadOnlyList<string>>(errors);
    }

    private static bool HasRequestDelegateConstructor(Type middlewareType)
    {
        return middlewareType
            .GetConstructors()
            .Any(constructor => constructor.GetParameters()
                .Any(parameter => parameter.ParameterType == typeof(RequestDelegate)));
    }
}