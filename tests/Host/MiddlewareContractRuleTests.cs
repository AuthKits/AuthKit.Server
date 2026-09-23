using AuthKit.PluginContractValidator.Rules;
using AuthKit.Plugins.Abstractions;
using AuthKit.Plugins.Abstractions.Contracts.PluginContract;
using Microsoft.AspNetCore.Http;
using Xunit;
using ValidatorLoadedPlugin = AuthKit.PluginContractValidator.Core.LoadedPlugin;

namespace AuthKit.Host.Tests;

public sealed class MiddlewareContractRuleTests
{
    private readonly MiddlewareRule _rule = new();

    [Theory]
    [InlineData(typeof(ConventionMiddleware))]
    [InlineData(typeof(BaseMiddleware))]
    [InlineData(typeof(InterfaceMiddleware))]
    public async Task ValidMiddlewareModels_AreAccepted(Type middlewareType)
    {
        var errors = await ValidateAsync(middlewareType);

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ConventionMiddleware_WithoutRequestDelegateConstructor_IsRejected()
    {
        var errors = await ValidateAsync(typeof(MissingRequestDelegateMiddleware));

        Assert.Contains(errors, error =>
            error.Contains("TestPlugin", StringComparison.Ordinal)
            && error.Contains(nameof(MissingRequestDelegateMiddleware), StringComparison.Ordinal)
            && error.Contains("RequestDelegate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MiddlewareMatchingBothAuthKitModels_IsRejectedAsAmbiguous()
    {
        var errors = await ValidateAsync(typeof(AmbiguousMiddleware));

        Assert.Contains(errors, error =>
            error.Contains(nameof(AmbiguousMiddleware), StringComparison.Ordinal)
            && error.Contains("both AuthKitMiddlewareBase and IAuthKitMiddleware", StringComparison.Ordinal));
    }

    [Fact]
    public async Task StaticInvokeMethod_IsRejected()
    {
        var errors = await ValidateAsync(typeof(StaticInvokeMiddleware));

        Assert.Contains(errors, error =>
            error.Contains(nameof(StaticInvokeMiddleware), StringComparison.Ordinal)
            && error.Contains("static Invoke", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenericMiddlewareType_IsRejected()
    {
        var errors = await ValidateAsync(typeof(GenericMiddleware<>));

        Assert.Contains(errors, error =>
            error.Contains(nameof(GenericMiddleware<object>), StringComparison.Ordinal)
            && error.Contains("generic", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task VoidReturningInvokeMethod_IsRejected()
    {
        var errors = await ValidateAsync(typeof(VoidInvokeMiddleware));

        Assert.Contains(errors, error =>
            error.Contains(nameof(VoidInvokeMiddleware), StringComparison.Ordinal)
            && error.Contains("Task", StringComparison.Ordinal));
    }

    [Fact]
    public async Task MultipleRequestDelegateConstructors_AreRejected()
    {
        var errors = await ValidateAsync(typeof(MultiCtorMiddleware));

        Assert.Contains(errors, error =>
            error.Contains(nameof(MultiCtorMiddleware), StringComparison.Ordinal)
            && error.Contains("multiple public constructors", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AbstractMiddlewareType_IsRejected()
    {
        var errors = await ValidateAsync(typeof(AbstractMiddleware));

        Assert.Contains(errors, error =>
            error.Contains(nameof(AbstractMiddleware), StringComparison.Ordinal)
            && error.Contains("abstract", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<string>> ValidateAsync(Type middlewareType)
    {
        var plugin = new TestPlugin(middlewareType);
        var loadedPlugin = new ValidatorLoadedPlugin(plugin, typeof(MiddlewareContractRuleTests).Assembly);

        return await _rule.ValidateAsync(loadedPlugin);
    }

    private sealed class TestPlugin(Type middlewareType) : IAuthKitPlugin
    {
        public string Name => "TestPlugin";

        public IReadOnlyList<PluginMiddleware> Middlewares =>
        [
            new(middlewareType, PipelinePosition.BeforeAuthentication)
        ];
    }

    public sealed class ConventionMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public Task InvokeAsync(HttpContext context, RequestDelegate next) =>
            _next(context);
    }

    public sealed class BaseMiddleware : AuthKitMiddlewareBase
    {
        public override Task InvokeAsync(HttpContext context, RequestDelegate next) => next(context);
    }

    public sealed class InterfaceMiddleware : IAuthKitMiddleware
    {
        public Task InvokeAsync(HttpContext context, RequestDelegate next) => next(context);
    }

    public sealed class MissingRequestDelegateMiddleware
    {
        public Task InvokeAsync(HttpContext context, RequestDelegate next) => next(context);
    }

    public sealed class AmbiguousMiddleware : AuthKitMiddlewareBase, IAuthKitMiddleware
    {
        public override Task InvokeAsync(HttpContext context, RequestDelegate next) => next(context);
    }

    public sealed class StaticInvokeMiddleware
    {
        public StaticInvokeMiddleware(RequestDelegate next)
        {
            _ = next;
        }

        public static Task InvokeAsync(HttpContext context, RequestDelegate next) => next(context);
    }

    public sealed class GenericMiddleware<T>
    {
        public GenericMiddleware(RequestDelegate next)
        {
            _ = next;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next) => next(context);
    }

    public sealed class VoidInvokeMiddleware
    {
        public VoidInvokeMiddleware(RequestDelegate next)
        {
            _ = next;
        }

        public void InvokeAsync(HttpContext context, RequestDelegate next) => next(context).GetAwaiter().GetResult();
    }

    public sealed class MultiCtorMiddleware
    {
        public MultiCtorMiddleware(RequestDelegate next)
        {
            _ = next;
        }

        public MultiCtorMiddleware(RequestDelegate next, string name)
        {
            _ = next;
            _ = name;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next) => next(context);
    }

    public abstract class AbstractMiddleware
    {
        protected AbstractMiddleware(RequestDelegate next)
        {
            _ = next;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next) => next(context);
    }
}
