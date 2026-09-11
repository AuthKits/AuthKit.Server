using Google.Protobuf.Reflection;

namespace DevTools.Catalog;

/// <summary>
/// Provides the gRPC services available in the current process together with
/// the protobuf schema required to render and invoke their methods.
/// </summary>
public interface IGrpcServiceCatalog
{
    /// <summary>
    /// Gets the discovered gRPC services ordered by full name.
    /// </summary>
    /// <returns>
    /// A collection of <see cref="GrpcServiceInfo"/> describing the
    /// discovered services and their methods.
    /// </returns>
    IReadOnlyList<GrpcServiceInfo> GetServices();

    /// <summary>
    /// Locates a method by its full method name.
    /// </summary>
    /// <param name="serviceName">The full name of the service.</param>
    /// <param name="methodName">The name of the method.</param>
    /// <returns>
    /// The <see cref="GrpcMethodCatalogEntry"/> for the matching method,
    /// or <c>null</c> when no such method exists in the catalog.
    /// </returns>
    GrpcMethodCatalogEntry? TryGetMethod(string serviceName, string methodName);
}

/// <summary>
/// Catalog entry carrying the live Protobuf reflection objects needed to
/// serialize and invoke the method.
/// </summary>
/// <remarks>
/// The <see cref="MethodDescriptor"/> is required at invocation time so the
/// dynamic <see cref="Google.Protobuf.IMessage"/> marshallers can be built
/// without generated client stubs.
/// </remarks>
public sealed record GrpcMethodCatalogEntry(
    GrpcMethodInfo Info,
    MethodDescriptor Descriptor);