using Core.KeyManagement.Interfaces;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Host.Grpc;

public class JwksService(
    ILogger<JwksService> logger,
    IJwtKeyStore keyStore) : Jwks.JwksBase
{
    public override Task<JwksResponse> GetJwks(Empty request, ServerCallContext context)
    {
        var response = new JwksResponse();

        foreach (var key in keyStore.GetPublicJwks())
        {
            response.Keys.Add(new Jwk
            {
                Kty = key.Kty,
                Use = key.Use,
                Kid = key.Kid,
                Alg = key.Alg,
                N = key.N,
                E = key.E,
                X5C = { key.X5c }
            });
        }

        logger.LogDebug("Returning {KeyCount} public keys in gRPC JWKS response", response.Keys.Count);

        return Task.FromResult(response);
    }

    public override Task<SingleKeyResponse> GetKeyByKid(KeyLookupRequest request, ServerCallContext context)
    {
        var key = keyStore.GetPublicJwks()
            .FirstOrDefault(k => k.Kid == request.Kid);

        if (key is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Key with kid '{request.Kid}' was not found."));
        }

        var metadata = keyStore.GetMetadata(request.Kid);

        var response = new SingleKeyResponse
        {
            Key = new Jwk
            {
                Kty = key.Kty,
                Use = key.Use,
                Kid = key.Kid,
                Alg = key.Alg,
                N = key.N,
                E = key.E,
                X5C = { key.X5c }
            },
            Metadata = metadata is null ? null : new KeyMetadata
            {
                Kid = metadata.Kid,
                CreatedAt = metadata.CreatedAt.ToString("O"),
                Revoked = metadata.Revoked,
                Algorithm = metadata.Algorithm,
                Purpose = metadata.Purpose
            }
        };

        return Task.FromResult(response);
    }

    public override Task<HealthResponse> GetJwksHealth(Empty request, ServerCallContext context)
    {
        var publicKeys = keyStore.GetPublicJwks().ToList();
        var activeCredentials = keyStore.GetActiveSigningCredentials();
        var activeKid = activeCredentials.Kid;
        var activeMetadata = activeKid is null ? null : keyStore.GetMetadata(activeKid);
        var isHealthy = publicKeys.Count > 0 && !string.IsNullOrWhiteSpace(activeKid);

        var response = new HealthResponse
        {
            Status = isHealthy ? "healthy" : "degraded",
            AvailableKeys = publicKeys.Count,
            ActiveKeyId = activeKid ?? string.Empty,
            ActiveKeyCreatedAt = activeMetadata?.CreatedAt.ToString("O") ?? string.Empty,
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        response.Details.Add("has_active_key", (activeKid is not null).ToString());
        response.Details.Add("key_ids", string.Join(",", publicKeys.Select(k => k.Kid)));

        return Task.FromResult(response);
    }

    public override Task<KeyStoreStatsResponse> GetJwksStats(Empty request, ServerCallContext context)
    {
        var publicKeys = keyStore.GetPublicJwks().ToList();
        var metadata = publicKeys
            .Select(k => keyStore.GetMetadata(k.Kid))
            .Where(m => m is not null)
            .ToList();

        var oldest = metadata.MinBy(m => m!.CreatedAt);
        var newest = metadata.MaxBy(m => m!.CreatedAt);

        var response = new KeyStoreStatsResponse
        {
            TotalKeys = publicKeys.Count,
            OldestKeyAge = oldest is null ? string.Empty : (DateTime.UtcNow - oldest.CreatedAt.UtcDateTime).ToString(),
            NewestKeyAge = newest is null ? string.Empty : (DateTime.UtcNow - newest.CreatedAt.UtcDateTime).ToString(),
            Timestamp = DateTime.UtcNow.ToString("O")
        };

        response.KeyIds.AddRange(publicKeys.Select(k => k.Kid));

        return Task.FromResult(response);
    }
}