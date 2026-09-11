using System;
using System.IO;
using AuthKit.PluginContractValidator;
using AuthKit.PluginContractValidator.Core;
using AuthKit.PluginContractValidator.Rules;

var pluginsRoot = args.Length > 0 ? args[0] : "src/Plugins";

if (!Directory.Exists(pluginsRoot))
{
    Console.Error.WriteLine($"Plugins root not found: {pluginsRoot}");
    return 1;
}

IPluginLoader loader = new PluginAssemblyLoader();
var validator = new ContractValidator([
    new MetadataRule(),
    new RegistrationRule(),
    new SecuritySchemesRule(),
    new MiddlewareRule(),
    new HealthRule()
]);

var failures = 0;

foreach (var pluginDir in Directory.GetDirectories(pluginsRoot))
{
    var pluginName = Path.GetFileName(pluginDir.TrimEnd(Path.DirectorySeparatorChar));
    var entryDll = Path.Combine(pluginDir, $"{pluginName}.dll");

    if (!File.Exists(entryDll))
    {
        Console.WriteLine($"[SKIP] {pluginName}: entry assembly '{entryDll}' not found");
        continue;
    }

    var loadResult = loader.Load(entryDll);
    if (!loadResult.Succeeded)
    {
        failures++;
        Console.WriteLine($"[FAIL] {pluginName}");
        foreach (var error in loadResult.Errors)
        {
            Console.WriteLine($"   - {error}");
        }

        continue;
    }

    var errors = await validator.ValidateAsync(loadResult.Plugin!);

    if (errors.Count == 0)
    {
        Console.WriteLine($"[PASS] {pluginName} v{loadResult.Plugin!.Instance.Version}");
    }
    else
    {
        failures++;
        Console.WriteLine($"[FAIL] {pluginName}");
        foreach (var error in errors)
        {
            Console.WriteLine($"   - {error}");
        }
    }
}

return failures == 0 ? 0 : 1;
