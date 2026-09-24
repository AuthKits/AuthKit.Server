using AuthKit.PluginContractValidator.Rules;
using AuthKit.Plugins.Abstractions.Contracts.PluginContract;
using AuthKit.Plugins.Abstractions.Pipeline;
using Microsoft.AspNetCore.Http;
using Xunit;
using ValidatorLoadedPlugin = AuthKit.PluginContractValidator.Core.LoadedPlugin;

namespace AuthKit.Host.Tests;

public sealed class MiddlewareContractRuleTests
{
    private readonly MiddlewareRule _rule = new();

    [Fact]
    public void RuleName_IsMiddleware() => Assert.Equal("Middleware", _rule.Name);

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

    private async Task<IReadOnlyList<string>> ValidateGrpcAsync(Type middlewareType)
    {
        var plugin = new GrpcTestPlugin(middlewareType);
        var loadedPlugin = new ValidatorLoadedPlugin(plugin, typeof(MiddlewareContractRuleTests).Assembly);

        return await _rule.ValidateAsync(loadedPlugin);
    }

    [Fact]
    public async Task ValidGrpcInterceptor_IsAccepted()
    {
        var errors = await ValidateGrpcAsync(typeof(ValidGrpcInterceptor));

        Assert.Empty(errors);
    }

    [Fact]
    public async Task NonInterceptorGrpcType_IsRejected()
    {
        var errors = await ValidateGrpcAsync(typeof(ConventionMiddleware));

        Assert.Contains(errors, error =>
            error.Contains(nameof(ConventionMiddleware), StringComparison.Ordinal)
            && error.Contains("Interceptor", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AbstractGrpcInterceptor_IsRejected()
    {
        var errors = await ValidateGrpcAsync(typeof(AbstractGrpcInterceptor));

        Assert.Contains(errors, error =>
            error.Contains(nameof(AbstractGrpcInterceptor), StringComparison.Ordinal)
            && error.Contains("abstract", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class GrpcTestPlugin(Type middlewareType) : IAuthKitPlugin
    {
        public string Name => "TestPlugin";

        public IReadOnlyList<PluginMiddleware> Middlewares =>
        [
            new(middlewareType, PipelinePosition.BeforeAuthentication, Transport: AuthKitTransport.Grpc)
        ];
    }

    public sealed class ValidGrpcInterceptor : Grpc.Core.Interceptors.Interceptor
    {
    }

    public abstract class AbstractGrpcInterceptor : Grpc.Core.Interceptors.Interceptor
    {
    }

    [Fact]
    public async Task NullMiddlewareTypeEntry_IsRejected()
    {
        var plugin = new RawListPlugin([new PluginMiddleware(null!, PipelinePosition.BeforeAuthentication)]);
        var loadedPlugin = new ValidatorLoadedPlugin(plugin, typeof(MiddlewareContractRuleTests).Assembly);

        var errors = await _rule.ValidateAsync(loadedPlugin);

        Assert.Contains(errors, error => error.Contains("null MiddlewareType", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UndefinedTransport_IsRejected()
    {
        var plugin = new RawListPlugin([new PluginMiddleware(typeof(ConventionMiddleware), PipelinePosition.BeforeAuthentication, Transport: (AuthKitTransport)99)]);
        var loadedPlugin = new ValidatorLoadedPlugin(plugin, typeof(MiddlewareContractRuleTests).Assembly);

        var errors = await _rule.ValidateAsync(loadedPlugin);

        Assert.Contains(errors, error => error.Contains("undefined transport", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UndefinedPosition_IsRejected()
    {
        var plugin = new RawListPlugin([new PluginMiddleware(typeof(ConventionMiddleware), (PipelinePosition)99)]);
        var loadedPlugin = new ValidatorLoadedPlugin(plugin, typeof(MiddlewareContractRuleTests).Assembly);

        var errors = await _rule.ValidateAsync(loadedPlugin);

        Assert.Contains(errors, error => error.Contains("undefined position", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task NonPublicMiddlewareType_IsRejected()
    {
        var errors = await ValidateAsync(typeof(PrivateMiddleware));

        Assert.Contains(errors, error =>
            error.Contains("must be public", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NonClassMiddlewareType_IsRejected()
    {
        var errors = await ValidateAsync(typeof(StructMiddleware));

        Assert.Contains(errors, error =>
            error.Contains("concrete class", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BaseMiddlewareWithoutPublicConstructor_IsRejected()
    {
        var errors = await ValidateAsync(typeof(PrivateCtorBaseMiddleware));

        Assert.Contains(errors, error =>
            error.Contains("at least one public constructor", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AbstractBaseMiddlewareWithoutOverride_IsRejected()
    {
        var errors = await ValidateAsync(typeof(NoOverrideBaseMiddleware));

        Assert.Contains(errors, error =>
            error.Contains("must override InvokeAsync", StringComparison.Ordinal));
    }

    private sealed class RawListPlugin(IReadOnlyList<PluginMiddleware> middlewares) : IAuthKitPlugin
    {
        public string Name => "TestPlugin";

        public IReadOnlyList<PluginMiddleware> Middlewares => middlewares;
    }

    private sealed class PrivateMiddleware
    {
        public PrivateMiddleware(RequestDelegate next)
        {
            _ = next;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next) => next(context);
    }

    public struct StructMiddleware
    {
        public Task InvokeAsync(HttpContext context, RequestDelegate next) => next(context);
    }

    public sealed class PrivateCtorBaseMiddleware : AuthKitMiddlewareBase
    {
        private PrivateCtorBaseMiddleware()
        {
        }

        public override Task InvokeAsync(HttpContext context, RequestDelegate next) => next(context);
    }

    public abstract class NoOverrideBaseMiddleware : AuthKitMiddlewareBase
    {
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

        public Task InvokeAsync(HttpContext context) =>
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
