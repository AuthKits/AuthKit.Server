namespace AuthKit.Plugins.Abstractions.Pipeline;

/// <summary>
/// Defines the supported locations for plugin application middleware.
/// </summary>
public enum PluginPipelinePosition
{
    /// <summary>Before ASP.NET Core routing.</summary>
    BeforeRouting = 0,

    /// <summary>After ASP.NET Core routing.</summary>
    AfterRouting = 10,

    /// <summary>Before host authentication.</summary>
    BeforeAuthentication = 20,

    /// <summary>After host authentication.</summary>
    AfterAuthentication = 30,

    /// <summary>Before host authorization.</summary>
    BeforeAuthorization = 40,

    /// <summary>After host authorization.</summary>
    AfterAuthorization = 50,

    /// <summary>Before endpoint execution.</summary>
    BeforeEndpoints = 60,

    /// <summary>After endpoint configuration.</summary>
    AfterEndpoints = 70
}