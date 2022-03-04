using Newtonsoft.Json;
using System;
using System.IO;

public class GraphQlClientGenerator : BaseGenerator
{
    private readonly string QueriesFolder = "queries";
    private readonly string MutationsFolder = "mutations";
    private readonly string SubscriptionsFolder = "subscriptions";

    public GraphQlClientGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateGraphQlClient()
    {
        CreateGraphqlRlFile();

        MakeDir(Config.Output.GraphQlClientFolder, QueriesFolder);
        MakeDir(Config.Output.GraphQlClientFolder, MutationsFolder);
        MakeDir(Config.Output.GraphQlClientFolder, SubscriptionsFolder);

        GenerateQueries();
    }

    private void GenerateQueries()
    {
        foreach (var model in Models)
        {
            GenerateQueriesForModel(model);
        }
    }

    private void GenerateQueriesForModel(GeneratorConfig.ModelConfig m)
    {
        WriteRawFile(liner =>
        {
            AddAllQuery(liner, m);
            liner.AddBlankLine();
            AddOneQuery(liner, m);
        }, Config.Output.GraphQlClientFolder, QueriesFolder, m.Name + "Queries.graphql");
    }

    private void AddAllQuery(Liner liner, GeneratorConfig.ModelConfig m)
    {
        liner.StartClosureInLine("query All" + m.Name + "s");
        liner.StartClosureInLine(m.Name.FirstToLower() + "s");
        if (m.HasPagingFeature())
        {
            liner.StartClosureInLine("nodes");
        }
        IncludeModelFields(liner, m);
        if (m.HasPagingFeature())
        {
            liner.EndClosure();
        }
        liner.EndClosure();
        liner.EndClosure();
    }

    private void AddOneQuery(Liner liner, GeneratorConfig.ModelConfig m)
    {
        liner.StartClosureInLine("query One" + m.Name + "($id: " + GetGqlIdType() + ")");
        liner.StartClosureInLine(m.Name.FirstToLower() + "(id: $id)");
        IncludeModelFields(liner, m);
        liner.EndClosure();
        liner.EndClosure();
    }

    private void IncludeModelFields(Liner liner, GeneratorConfig.ModelConfig m)
    {
        liner.Add("id");
        foreach (var f in m.Fields)
        {
            liner.Add(f.Name.FirstToLower());
        }
        var requiredSubModels = GetMyRequiredSubModels(m);
        foreach (var sub in requiredSubModels)
        {
            liner.StartClosureInLine(sub.Name.FirstToLower());
            IncludeModelFields(liner, sub);
            liner.EndClosure();
        }
    }

    private string GetGqlIdType()
    {
        return Config.IdType.FirstToUpper() + "!";
    }

    private void CreateGraphqlRlFile()
    {
        var graphQlRc = new GraphQlRc
        {
            Schema = "schema.graphql",
            Documents = "**/*.graphql",
            Extensions = new GraphQlRcExtensions
            {
                StrawberryShake = new GraphQlRcExtensionsStrawberryShake
                {
                    Name = Config.GenerateNamespace + "Client",
                    Namespace = Config.GenerateNamespace + ".Client",
                    Url = "http://localhost:5000/graphql",
                    DependencyInjection = true
                }
            }
        };

        File.WriteAllLines(Path.Join(Config.Output.ProjectRoot, Config.Output.GraphQlClientFolder, ".graphqlrc.json"),
            new[] { JsonConvert.SerializeObject(graphQlRc, Formatting.Indented) });
    }

    public class GraphQlRc
    {
        public string Schema { get; set; }
        public string Documents { get; set; }
        public GraphQlRcExtensions Extensions { get; set; }
    }

    public class GraphQlRcExtensions
    {
        public GraphQlRcExtensionsStrawberryShake StrawberryShake { get; set; }
    }

    public class GraphQlRcExtensionsStrawberryShake
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Url { get; set; }
        public bool DependencyInjection { get; set; }
    }
}
