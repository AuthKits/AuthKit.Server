using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AuthKit.PluginContractValidator.Core;
using AuthKit.Plugins.Abstractions.Contracts;
using AuthKit.Plugins.Abstractions.Contracts.PluginContract;
using AuthKit.Plugins.Abstractions.Pipeline;
using Microsoft.AspNetCore.Http;

namespace AuthKit.PluginContractValidator.Rules;

/// <summary>
/// Ensures contributed middleware types follow one of the supported AuthKit
/// middleware models.
/// </summary>
/// <remarks>
/// The validator distinguishes conventional ASP.NET Core middleware,
/// <see cref="AuthKitMiddlewareBase"/>, and <see cref="IAuthKitMiddleware"/>.
/// </remarks>
public sealed class MiddlewareRule : IPluginContractRule
{
    /// <summary>Gets the rule name ("Middleware").</summary>
    public string Name => "Middleware";

    /// <summary>
    /// Validates every legacy and declarative middleware type contributed by
    /// the plugin.
    /// </summary>
    public Task<IReadOnlyList<string>> ValidateAsync(
        LoadedPlugin plugin,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var pluginName = plugin.Instance.Name;

        if (plugin.Instance.MiddlewareType is { } legacyMiddlewareType)
            ValidateMiddlewareType(pluginName, legacyMiddlewareType, errors);

        foreach (var middleware in plugin.Instance.Middlewares ?? [])
        {
            if (middleware.MiddlewareType is null)
            {
                errors.Add($"middleware: Plugin '{pluginName}' declares PluginMiddleware with a null MiddlewareType.");
                continue;
            }

            if (middleware.Transport == AuthKitTransport.Grpc)
                ValidateGrpcInterceptorType(pluginName, middleware.MiddlewareType, errors);
            else
                ValidateMiddlewareType(pluginName, middleware.MiddlewareType, errors);
        }

        return Task.FromResult<IReadOnlyList<string>>(errors);
    }

    private static void ValidateGrpcInterceptorType(string pluginName, Type middlewareType, List<string> errors)
    {
        var typeErrors = new List<string>();

        ValidateCommonShape(middlewareType, typeErrors);

        if (!typeof(Grpc.Core.Interceptors.Interceptor).IsAssignableFrom(middlewareType))
        {
            typeErrors.Add(
                "targets the gRPC transport but is not a Grpc.Core.Interceptors.Interceptor subclass. " +
                "HTTP middleware (IAuthKitMiddleware, AuthKitMiddlewareBase, convention middleware) " +
                "cannot run on gRPC; declare Transport = Http or provide an Interceptor instead. " +
                "No automatic HttpContext bridge is provided.");
        }

        errors.AddRange(typeErrors.Select(error =>
            $"middleware: Plugin '{pluginName}' gRPC interceptor '{FormatTypeName(middlewareType)}' is invalid: {error}"));
    }

    private static void ValidateMiddlewareType(string pluginName, Type middlewareType, List<string> errors)
    {
        var typeErrors = new List<string>();

        ValidateCommonShape(middlewareType, typeErrors);

        var isBaseClassModel = typeof(AuthKitMiddlewareBase).IsAssignableFrom(middlewareType);
        var isInterfaceModel = typeof(IAuthKitMiddleware).IsAssignableFrom(middlewareType);

        if (isBaseClassModel && isInterfaceModel)
        {
            typeErrors.Add("matches both AuthKitMiddlewareBase and IAuthKitMiddleware models; choose one model.");
        }
        else if (isBaseClassModel)
        {
            ValidateDiActivatedMiddleware(middlewareType, nameof(AuthKitMiddlewareBase), typeErrors);
            ValidateAuthKitBaseMiddleware(middlewareType, typeErrors);
        }
        else if (isInterfaceModel)
        {
            ValidateDiActivatedMiddleware(middlewareType, nameof(IAuthKitMiddleware), typeErrors);
            ValidateInterfaceMiddleware(middlewareType, typeErrors);
        }
        else
        {
            ValidateConventionMiddleware(middlewareType, typeErrors);
        }

        errors.AddRange(typeErrors.Select(error =>
            $"middleware: Plugin '{pluginName}' middleware '{FormatTypeName(middlewareType)}' is invalid: {error}"));
    }

    private static void ValidateCommonShape(Type middlewareType, List<string> errors)
    {
        if (!IsPubliclyVisible(middlewareType))
            errors.Add("type must be public.");

        if (!middlewareType.IsClass)
            errors.Add("type must be a concrete class.");

        if (middlewareType.IsAbstract)
            errors.Add("type must not be abstract.");

        if (middlewareType.ContainsGenericParameters || middlewareType.IsGenericType)
            errors.Add("generic middleware types are not supported.");
    }

    private static void ValidateConventionMiddleware(Type middlewareType, List<string> errors)
    {
        var activationConstructors = middlewareType.GetConstructors()
            .Where(constructor => ConstructorStartsWith<RequestDelegate>(constructor))
            .ToArray();

        if (activationConstructors.Length == 0)
        {
            errors.Add("does not implement AuthKitMiddlewareBase or IAuthKitMiddleware, " +
                "and its constructor does not take RequestDelegate as the first parameter.");
        }
        else if (activationConstructors.Length > 1)
        {
            errors.Add("declares multiple public constructors with RequestDelegate as the first parameter.");
        }

        ValidateInvokeMethod(
            middlewareType,
            static method => method.Name is "InvokeAsync" or "Invoke",
            IsValidConventionInvokeMethod,
            "must expose public instance InvokeAsync(HttpContext, ...dependencies) or Invoke(HttpContext, ...dependencies) returning Task.",
            errors);
    }

    private static void ValidateDiActivatedMiddleware(Type middlewareType, string modelName, List<string> errors)
    {
        if (middlewareType.GetConstructors().Length == 0)
            errors.Add($"{modelName} middleware must expose at least one public constructor for DI activation.");
    }

    private static void ValidateAuthKitBaseMiddleware(Type middlewareType, List<string> errors)
    {
        var invokeAsync = middlewareType.GetMethod(
            "InvokeAsync",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: [typeof(HttpContext), typeof(RequestDelegate)],
            modifiers: null);

        if (invokeAsync is null || invokeAsync.DeclaringType == typeof(AuthKitMiddlewareBase))
        {
            errors.Add("must override InvokeAsync(HttpContext, RequestDelegate).");
            return;
        }

        ValidateInvokeMethod(
            middlewareType,
            static method => method.Name == "InvokeAsync",
            IsValidAuthKitInvokeMethod,
            "must override InvokeAsync(HttpContext, RequestDelegate) returning Task.",
            errors);
    }

    private static void ValidateInterfaceMiddleware(Type middlewareType, List<string> errors) =>
        ValidateInvokeMethod(
            middlewareType,
            static method => method.Name == "InvokeAsync",
            IsValidAuthKitInvokeMethod,
            "must provide public instance InvokeAsync(HttpContext, RequestDelegate) returning Task.",
            errors);

    private static void ValidateInvokeMethod(
        Type middlewareType,
        Func<MethodInfo, bool> namePredicate,
        Func<MethodInfo, bool> signaturePredicate,
        string missingMessage,
        List<string> errors)
    {
        var allCandidates = middlewareType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(namePredicate)
            .ToArray();

        if (allCandidates.Any(method => method.IsStatic))
            errors.Add("static Invoke or InvokeAsync methods are not valid middleware entry points.");

        var validMethod = allCandidates.Any(method => !method.IsStatic && signaturePredicate(method));

        if (!validMethod)
            errors.Add(missingMessage);
    }

    private static bool ConstructorStartsWith<TParameter>(ConstructorInfo constructor)
    {
        var parameters = constructor.GetParameters();
        return parameters.Length > 0 && parameters[0].ParameterType == typeof(TParameter);
    }

    private static bool HasExactParameters(MethodInfo method, params Type[] expected) =>
        method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(expected);

    private static bool IsValidConventionInvokeMethod(MethodInfo method)
    {
        var parameters = method.GetParameters();

        return method.ReturnType == typeof(Task)
            && parameters.Length > 0
            && parameters[0].ParameterType == typeof(HttpContext);
    }

    private static bool IsValidAuthKitInvokeMethod(MethodInfo method) =>
        method.ReturnType == typeof(Task)
        && HasExactParameters(method, typeof(HttpContext), typeof(RequestDelegate));

    private static bool IsPubliclyVisible(Type type)
    {
        if (type.IsPublic)
            return true;

        if (!type.IsNestedPublic)
            return false;

        return type.DeclaringType is not null && IsPubliclyVisible(type.DeclaringType);
    }

    private static string FormatTypeName(Type type) => type.FullName ?? type.Name;
}
