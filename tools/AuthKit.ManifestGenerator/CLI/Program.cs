using System;
using System.IO;
using System.Threading.Tasks;
using AuthKit.ManifestGenerator.Core;
using AuthKit.ManifestGenerator.Core.Generators;
using Microsoft.Extensions.DependencyInjection;

namespace AuthKit.ManifestGenerator.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: dotnet run -- --input <input-dll> --output <output-file>");
            return 1;
        }

        string? input = null;
        string? output = null;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--input" || args[i] == "-i")
            {
                input = args[i + 1];
            }
            else if (args[i] == "--output" || args[i] == "-o")
            {
                output = args[i + 1];
            }
        }

        if (input == null || output == null)
        {
            Console.WriteLine("Error: Missing required arguments.");
            Console.WriteLine("Usage: dotnet run -- --input <input-dll> --output <output-file>");
            return 1;
        }

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddManifestGeneratorServices();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var manifestGenerator = serviceProvider.GetRequiredService<IManifestGenerator>();
        try
        {
            manifestGenerator.Generate(input, output);
            Console.WriteLine("\n--- Success ---");
            Console.WriteLine("Manifest generated successfully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("\n--- Error ---");
            Console.WriteLine("Failed to generate manifest: {0}", ex.Message);
            return 1;
        }
    }
}
