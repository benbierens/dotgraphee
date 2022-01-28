using System;
using System.IO;
using Newtonsoft.Json;

public class ConfigLoader
{
    public GeneratorConfig Get(string[] args)
    {
        foreach (var s in args)
        {
            var c = TryParse(s);
            if (c != null) return c;
        }

        return TryParse("generatorConfig.json");
    }

    private GeneratorConfig TryParse(string filename)
    {
        try
        {
            if (!File.Exists(filename)) return null;
            var lines = File.ReadAllLines(filename);
            var config = JsonConvert.DeserializeObject<GeneratorConfig>(string.Join(" ", lines));
            foreach (var m in config.Models)
            {
                if (m.Fields == null) m.Fields = new GeneratorConfig.ModelField[0];
                if (m.HasMany == null) m.HasMany = new string[0];
            }
            return config;
        }
        catch
        {
            return null;
        }
    }
}

public class GeneratorConfig
{
    public enum FailedToFindStrategy
    {
        useNullObject,
        useErrorCode
    }

    public class ConfigSection
    {
        public string GenerateNamespace { get; set; }
        public ConfigOutputSection Output { get; set; }
        public ConfigDatabaseSection Database { get; set; }
        public ConfigGraphQlSection GraphQl { get; set; }
        public ConfigTestSection Tests { get; set; }
        public string IdType { get; set; }
        public string FailedToFindStrategy { get; set; }
        public string SelfRefNavigationPropertyPrefix { get; set; }
        public string[] Packages { get; set; }

        public FailedToFindStrategy GetFailedToFindStrategy()
        {
            return Enum.Parse<FailedToFindStrategy>(FailedToFindStrategy);
        }
    }

    public class ConfigOutputSection
    {
        public string ProjectRoot { get; set; }
        public string SourceFolder { get; set; }
        public string TestFolder { get; set; }
        public string GeneratedFolder { get; set; }
        public string DtoSubFolder { get; set; }
        public string DatabaseSubFolder { get; set; }
        public string GraphQlSubFolder { get; set; }
    }

    public class ConfigDatabaseSection
    {
        public string DbContextClassName { get; set; }
        public string DbAccesserClassName { get; set; }
        public string DbContextFileName { get; set; }
        public string DbContainerName { get; set; }

        public ConfigDatabaseConnectionSection LocalDev { get; set; }
        public ConfigDatabaseConnectionSection Docker { get; set; }
    }

    public class ConfigDatabaseConnectionSection
    {
        public string DbHost { get; set; }
        public string DbName { get; set; }
        public string DbUsername { get; set; }
        public string DbPassword { get; set; }
    }

    public class ConfigGraphQlSection
    {
        public string GqlTypesFileName { get; set; }
        public string GqlQueriesClassName { get; set; }
        public string GqlQueriesFileName { get; set; }

        public string GqlMutationsClassName { get; set; }
        public string GqlMutationsFilename { get; set; }
        public string GqlMutationsInputTypePostfix { get; set; }
        public string GqlMutationsCreateMethod { get; set; }
        public string GqlMutationsUpdateMethod { get; set; }
        public string GqlMutationsDeleteMethod { get; set; }

        public string GqlSubscriptionsClassName { get; set; }
        public string GqlSubscriptionsFilename { get; set; }
        public string GqlSubscriptionCreatedMethod { get; set; }
        public string GqlSubscriptionUpdatedMethod { get; set; }
        public string GqlSubscriptionDeletedMethod { get; set; }
    }

    public class ConfigTestSection
    {
        public string TestCategory { get; set; }
        public string SubFolder { get; set; }
        public string UtilsFolder { get; set; }
    }

    public class ModelConfig
    {
        public string Name { get; set; }
        public ModelField[] Fields { get; set; }
        public string[] HasMany { get; set; }
    }

    public class ModelField
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public ConfigSection Config { get; set; }
    public ModelConfig[] Models { get; set; }
}
