namespace ExamplePlugin.Options;

/// <summary>
/// Configuration root for the ExamplePlugin.
/// </summary>
/// <remarks>
/// Values are bound from the <c>Plugins:authkit.example</c> configuration section
/// through <see cref="AuthKit.Plugins.Abstractions.Contracts.AuthKitPluginContext"/>.
/// </remarks>
public sealed class ExampleOptions
{
    /// <summary>
    /// A message the plugin echoes from its reference endpoint.
    /// </summary>
    public string Greeting { get; set; } = "Hello from ExamplePlugin!";

    /// <summary>
    /// Enables the demo background service.
    /// </summary>
    public bool EnableBackgroundService { get; set; } = true;
}