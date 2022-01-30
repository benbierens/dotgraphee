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
        [Check(CheckType.NotEmpty)]
        public string GenerateNamespace { get; set; }
        public string HeaderComment { get; set; }

        public ConfigOutputSection Output { get; set; }
        public ConfigDatabaseSection Database { get; set; }
        public ConfigGraphQlSection GraphQl { get; set; }
        public ConfigTestSection Tests { get; set; }

        [Check(CheckType.OneOf, "int", "string")]
        public string IdType { get; set; }

        [Check(CheckType.ParsesTo, typeof(FailedToFindStrategy))]
        public string FailedToFindStrategy { get; set; }
        [Check(CheckType.NotEmpty)]
        public string SelfRefNavigationPropertyPrefix { get; set; }
        public string[] Packages { get; set; }

        public FailedToFindStrategy GetFailedToFindStrategy()
        {
            return Enum.Parse<FailedToFindStrategy>(FailedToFindStrategy);
        }
    }

    public class ConfigOutputSection
    {
        [Check(CheckType.NotEmpty)]
        public string ProjectRoot { get; set; }
        [Check(CheckType.NotEmpty)]
        public string SourceFolder { get; set; }
        [Check(CheckType.NotEmpty)]
        public string TestFolder { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GeneratedFolder { get; set; }
        [Check(CheckType.NotEmpty)]
        public string DtoSubFolder { get; set; }
        [Check(CheckType.NotEmpty)]
        public string DatabaseSubFolder { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GraphQlSubFolder { get; set; }
    }

    public class ConfigDatabaseSection
    {
        [Check(CheckType.NotEmpty)]
        public string DbContextClassName { get; set; }
        [Check(CheckType.NotEmpty)]
        public string DbContextFileName { get; set; }
        [Check(CheckType.NotEmpty)]
        public string DbAccesserClassName { get; set; }
        [Check(CheckType.NotEmpty)]
        public string DbAccesserFileName { get; set; }
        [Check(CheckType.NotEmpty)]
        public string DbContainerName { get; set; }

        public ConfigDatabaseConnectionLocalSection LocalDev { get; set; }
        public ConfigDatabaseConnectionDockerSection Docker { get; set; }
    }

    public class ConfigDatabaseConnectionLocalSection : ConfigDatabaseConnectionDockerSection
    {
        [Check(CheckType.NotEmpty)]
        public string DbHost { get; set; }
    }

    public class ConfigDatabaseConnectionDockerSection
    {
        [Check(CheckType.NotEmpty)]
        public string DbName { get; set; }
        [Check(CheckType.NotEmpty)]
        public string DbUsername { get; set; }
        [Check(CheckType.NotEmpty)]
        public string DbPassword { get; set; }
    }

    public class ConfigGraphQlSection
    {
        [Check(CheckType.NotEmpty)]
        public string GqlTypesFileName { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GqlQueriesClassName { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GqlQueriesFileName { get; set; }

        [Check(CheckType.NotEmpty)]
        public string GqlMutationsClassName { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GqlMutationsFilename { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GqlMutationsInputTypePostfix { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GqlMutationsCreateMethod { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GqlMutationsUpdateMethod { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GqlMutationsDeleteMethod { get; set; }

        [Check(CheckType.NotEmpty)]
        public string GqlSubscriptionsClassName { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GqlSubscriptionsFilename { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GqlSubscriptionCreatedMethod { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GqlSubscriptionUpdatedMethod { get; set; }
        [Check(CheckType.NotEmpty)]
        public string GqlSubscriptionDeletedMethod { get; set; }
    }

    public class ConfigTestSection
    {
        [Check(CheckType.NotEmpty)]
        public string TestCategory { get; set; }
        [Check(CheckType.NotEmpty)]
        public string SubFolder { get; set; }
        [Check(CheckType.NotEmpty)]
        public string UtilsFolder { get; set; }
    }

    public class ModelConfig
    {
        [Check(CheckType.NotEmpty)]
        public string Name { get; set; }
        public ModelField[] Fields { get; set; }
        public string[] HasMany { get; set; }
    }

    public class ModelField
    {
        [Check(CheckType.NotEmpty)]
        public string Name { get; set; }
        [Check(CheckType.OneOfSupportedTypes)]
        public string Type { get; set; }
    }

    public ConfigSection Config { get; set; }
    public ModelConfig[] Models { get; set; }
}
