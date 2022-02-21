public class GraphQlGenerator : BaseGenerator
{
    private readonly GraphQlQueriesGenerator _queriesGenerator;
    private readonly GraphQlSubscriptionsGenerator _subscriptionsGenerator;
    private readonly GraphQlTypesGenerator _typesGenerator;
    private readonly GraphQlMutationsGenerator _mutationsGenerator;

    public GraphQlGenerator(GeneratorConfig config)
        : base(config)
    {
        _queriesGenerator = new GraphQlQueriesGenerator(config);
        _subscriptionsGenerator = new GraphQlSubscriptionsGenerator(config);
        _typesGenerator = new GraphQlTypesGenerator(config);
        _mutationsGenerator = new GraphQlMutationsGenerator(config);
    }

    public void GenerateGraphQl()
    {
        MakeSrcDir(Config.Output.GeneratedFolder, Config.Output.GraphQlSubFolder);
        _queriesGenerator.GenerateGraphQlQueries();
        _subscriptionsGenerator.GenerateGraphQlSubscriptions();
        _typesGenerator.GenerateGraphQlTypes();  
        _mutationsGenerator.GenerateGraphQlMutations();  
    }
}
