using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Shield.Composition;
using Shield.Detection;
using Shield.State;
using Xunit;

namespace Shield.Tests;

public sealed class ShieldReviewTests
{
    [Fact]
    public void ClientIdResolver_IgnoresClientSuppliedLeftmostAddress()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.1, 203.0.113.5, 192.0.2.1";

        Assert.Equal("192.0.2.1", ClientIdResolver.Resolve(context, true, 1));
        Assert.Equal("203.0.113.5", ClientIdResolver.Resolve(context, true, 2));
        Assert.Equal("192.0.2.10", ClientIdResolver.Resolve(context, true, 4));
        Assert.Equal("192.0.2.10", ClientIdResolver.Resolve(context, false));
    }

    [Fact]
    public void Counter_EvictsLeastRecentlyUsedClient()
    {
        var state = new ShieldState();
        var now = DateTimeOffset.UtcNow;
        Assert.Equal(1, state.RecordRequest("busy", now, 60, 2));
        Assert.Equal(1, state.RecordRequest("quiet", now.AddSeconds(1), 60, 2));
        Assert.Equal(2, state.RecordRequest("busy", now.AddSeconds(2), 60, 2));

        state.RecordRequest("new", now.AddSeconds(3), 60, 2);

        Assert.Equal(3, state.RecordRequest("busy", now.AddSeconds(4), 60, 2));
        Assert.Equal(1, state.RecordRequest("quiet", now.AddSeconds(5), 60, 2));
    }

    [Fact]
    public void OneOffenderBlocksOnlyThatClient_SeveralOffendersTripGlobalLockdown()
    {
        var state = new ShieldState();
        var duration = TimeSpan.FromMinutes(1);

        state.RecordOffender("first", "signature", "attack", duration, 10, 2, 10);
        Assert.True(state.IsClientBlocked("first", out _, out _));
        Assert.False(state.IsClientBlocked("second", out _, out _));
        Assert.False(state.IsLockdownActive(out _, out _));

        state.RecordOffender("first", "signature", "attack", duration, 10, 2, 10);
        Assert.False(state.IsLockdownActive(out _, out _));

        state.RecordOffender("second", "signature", "attack", duration, 10, 2, 10);
        Assert.True(state.IsLockdownActive(out _, out _));
    }

    [Fact]
    public async Task HttpMiddleware_BlocksOnlyOffenderUntilDistinctThreshold()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [nameof(Shield.Options.ShieldOptions.TrustForwardedHeader)] = "true",
            [nameof(Shield.Options.ShieldOptions.MinOffendingClientsForLockdown)] = "2",
        });
        builder.Services.AddShield(builder.Configuration);

        await using var app = builder.Build();
        app.UseMiddleware<Shield.Middleware.ShieldMiddleware>();
        app.MapGet("/ok", () => "ok");
        await app.StartAsync();
        var client = app.GetTestClient();

        async Task<HttpStatusCode> Send(string address, bool suspicious = false)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
            request.Headers.Add("X-Forwarded-For", $"198.51.100.1, {address}");
            if (suspicious)
                request.Headers.UserAgent.ParseAdd("sqlmap");
            using var response = await client.SendAsync(request);
            return response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.ServiceUnavailable, await Send("203.0.113.1", suspicious: true));
        Assert.Equal(HttpStatusCode.OK, await Send("203.0.113.2"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, await Send("203.0.113.1"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, await Send("203.0.113.2", suspicious: true));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, await Send("203.0.113.3"));
    }
}
