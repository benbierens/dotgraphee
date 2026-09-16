public class GraphQlClientGenerator : BaseGenerator
{
    private readonly GraphQlFilesGenerator filesGenerator;
    private readonly SubscriptionHandleClassGenerator subscriptionHandleClassGenerator;
    private readonly QueryClassGenerator queryClassGenerator;
    private readonly GqlClassGenerator gqlClassGenerator;
    private readonly InclusionBuilderGenerator inclusionBuilderGenerator;
    private readonly GqlBuildClassGenerator gqlBuildClassGenerator;
    private readonly ClientClassGenerator clientClassGenerator;
    private readonly GqlDataClassGenerator gqlDataClassGenerator;

    public GraphQlClientGenerator(GeneratorConfig config)
        : base(config)
    {
        filesGenerator = new GraphQlFilesGenerator(config);
        subscriptionHandleClassGenerator = new SubscriptionHandleClassGenerator(config);
        queryClassGenerator = new QueryClassGenerator(config);
        gqlClassGenerator = new GqlClassGenerator(config);
        inclusionBuilderGenerator = new InclusionBuilderGenerator(config);
        gqlBuildClassGenerator = new GqlBuildClassGenerator(config);
        clientClassGenerator = new ClientClassGenerator(config);
        gqlDataClassGenerator = new GqlDataClassGenerator(config);
    }

    public void GenerateGraphQlClient()
    {
        filesGenerator.GenerateGraphQlFiles();
        subscriptionHandleClassGenerator.CreateSubscriptionHandleClass();
        queryClassGenerator.CreateQueryClasses();
        gqlClassGenerator.CreateGqlClass();
        inclusionBuilderGenerator.CreateInclusionBuilderClass();
        gqlBuildClassGenerator.CreateGqlBuildClass();
        clientClassGenerator.CreateClientClass();
        gqlDataClassGenerator.CreateGqlDataClass();
    }
}
