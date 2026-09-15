public class GraphQlGenerator : BaseGenerator
{
    private readonly GraphQlQueriesGenerator queriesGenerator;
    private readonly GraphQlSubscriptionsGenerator subscriptionsGenerator;
    private readonly GraphQlMutationsGenerator mutationsGenerator;
    private readonly PublisherClassGenerator publisherClassGenerator;
    private readonly InputConverterClassGenerator inputConverterClassGenerator;

    public GraphQlGenerator(GeneratorConfig config)
        : base(config)
    {
        queriesGenerator = new GraphQlQueriesGenerator(config);
        subscriptionsGenerator = new GraphQlSubscriptionsGenerator(config);
        mutationsGenerator = new GraphQlMutationsGenerator(config);
        publisherClassGenerator = new PublisherClassGenerator(config);
        inputConverterClassGenerator = new InputConverterClassGenerator(config);
    }

    public void GenerateGraphQl()
    {
        MakeSrcDir(Config.Output.GraphQlSubFolder);
        queriesGenerator.GenerateGraphQlQueries();
        subscriptionsGenerator.GenerateGraphQlSubscriptions();
        mutationsGenerator.GenerateGraphQlMutations();
        publisherClassGenerator.GeneratePublisher();
        inputConverterClassGenerator.GenerateInputConverter();
    }
}
