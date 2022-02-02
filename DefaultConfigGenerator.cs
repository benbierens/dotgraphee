using System.IO;
using System.Reflection;

public class DefaultConfigGenerator
{
    private const string defaultConfigFilename = "dotgraphee-config.json";

    public void CreateDefaultConfig()
    {
        var here = Directory.GetCurrentDirectory();
        Log.Write("Writing default configuration to: '" + Path.Join(here, defaultConfigFilename)  + "' ...");

        WriteToFile(ReadDefaultConfigResource());

        Log.Write("Done!");
        Log.Write("Modify the configuration file, then run 'dotgraphee " + defaultConfigFilename + "'.");
        Log.Write("To make life easy, all default values are valid.");
    }

    private string ReadDefaultConfigResource()
    {
        var assembly = Assembly.GetEntryAssembly();
        var resourceStream = assembly.GetManifestResourceStream("dotgraphee.default-config.json");
        var reader = new StreamReader(resourceStream);
        return reader.ReadToEnd();
    }

    private void WriteToFile(string content)
    {
        File.WriteAllLines(defaultConfigFilename, new[] { content });
    }
}
