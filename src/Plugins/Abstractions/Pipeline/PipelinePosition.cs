namespace AuthKit.Plugins.Abstractions.Pipeline;

/// <summary>
/// Declares where plugin middleware should be inserted in the AuthKit host pipeline.
/// </summary>
/// <remarks>
/// Each value maps to well defined point in the standard ASP.NET Core pipeline:
/// BeforeRouting -> before UseRouting; AfterRouting -> after UseRouting, before authentication;
/// BeforeAuthentication -> before UseAuthentication; AfterAuthorization -> after UseAuthorization
/// (ie. after authentication AND authorization) BeforeEndpoints -> before endpoints
/// AfterEndpointExecution -> post endpoint execution (response post-processing, not merely
/// registration after UseEndpoints).
/// There is intentionally no AfterAuthentication position.
/// </remarks>
/// <remarks>
/// For the gRPC transport these values are AuthKit <b>semantic</b> positions composed by the
/// host into the single gRPC interceptor chain (before call handling, around the host
/// authentication/authorization interceptors, directly before the service method, and
/// post processing after it) not six native ASP.NET Core gRPC insertion points.
/// </remarks>
public enum PipelinePosition
{
    BeforeRouting = 0,
    AfterRouting = 10,
    BeforeAuthentication = 20,
    AfterAuthorization = 30,
    BeforeEndpoints = 40,
    AfterEndpointExecution = 50
}
