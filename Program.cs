using System;
using System.IO;
using System.Linq;

namespace generator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (!args.Any())
            {
                DeployDefaultConfig();
                return;
            }

            if (!File.Exists(args[0]))
            {
                Log.Write("File not found: '" + args[0] + "'");
                return;
            }

            RunGenerator(args[0]);
        }

        private static void DeployDefaultConfig()
        {
            var generator = new DefaultConfigGenerator();
            generator.CreateDefaultConfig();
        }

        private static void RunGenerator(string configFile)
        {
            Log.Write("Loading configuration from '" + configFile + "'...");
            var config = ReadConfig(configFile);

            Log.Write("Validating...");
            var validator = new ConfigValidator();
            validator.Validate(config);
            if (!validator.IsValid) return;

            Log.Write("Running generators...");
            var gen = new Generator(config);
            gen.Generate();

            Log.Write("");
            Log.Write("Done!");
            Log.Write("See the 'Readme.md' of your new project for build and test instructions.");
            Log.Write("");
        }

        private static GeneratorConfig ReadConfig(string configFile)
        {
            try
            {
                return new ConfigLoader().TryParse(configFile);
            }
            catch (Exception ex)
            {
                Log.Write("Error while reading config file:");
                Log.Write(ex.ToString());
                return null;
            }
        }
    }
}
