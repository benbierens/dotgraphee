public class TestGenerator : BaseGenerator
{
    private readonly BaseGqlTestClassGenerator baseGqlTestClassGenerator;
    private readonly SubscriptionHandleClassGenerator subscriptionHandleClassGenerator;
    private readonly DockerControllerClassGenerator dockerControllerClassGenerator;
    private readonly QueryClassGenerator queryClassGenerator;
    private readonly TestDataClassGenerator testDataClassGenerator;
    private readonly GqlClassGenerator gqlClassGenerator;
    private readonly InclusionBuilderGenerator inclusionBuilderGenerator;
    private readonly GqlBuildClassGenerator gqlBuildClassGenerator;
    private readonly ClientClassGenerator clientClassGenerator;

    private readonly CreateTestsGenerator createTestsGenerator;
    private readonly QueryTestsGenerator queryTestsGenerator;
    private readonly UpdateTestsGenerator updateTestsGenerator;
    private readonly DeleteTestsGenerator deleteTestsGenerator;
    private readonly SubscriptionTestsGenerator subscriptionTestsGenerator;

    public TestGenerator(GeneratorConfig config)
        : base(config)
    {
        baseGqlTestClassGenerator = new BaseGqlTestClassGenerator(config);
        subscriptionHandleClassGenerator = new SubscriptionHandleClassGenerator(config);
        dockerControllerClassGenerator = new DockerControllerClassGenerator(config);
        queryClassGenerator = new QueryClassGenerator(config);
        testDataClassGenerator = new TestDataClassGenerator(config);
        gqlClassGenerator = new GqlClassGenerator(config);
        inclusionBuilderGenerator = new InclusionBuilderGenerator(config);
        gqlBuildClassGenerator = new GqlBuildClassGenerator(config);
        clientClassGenerator = new ClientClassGenerator(config);

        createTestsGenerator = new CreateTestsGenerator(config);
        queryTestsGenerator = new QueryTestsGenerator(config);
        updateTestsGenerator = new UpdateTestsGenerator(config);
        deleteTestsGenerator = new DeleteTestsGenerator(config);
        subscriptionTestsGenerator = new SubscriptionTestsGenerator(config);
    }

    public void GenerateTests()
    {
        MakeIntegrationTestDir(Config.Tests.SubFolder);
        MakeIntegrationTestDir(Config.Tests.SubFolder, Config.Tests.UtilsFolder);
        
        baseGqlTestClassGenerator.CreateBaseGqlTestClass();
        subscriptionHandleClassGenerator.CreateSubscriptionHandleClass();
        dockerControllerClassGenerator.CreateDockerControllerClass();
        queryClassGenerator.CreateQueryClasses();
        testDataClassGenerator.CreateTestDataClass();
        gqlClassGenerator.CreateGqlClass();
        inclusionBuilderGenerator.CreateInclusionBuilderClass();
        gqlBuildClassGenerator.CreateGqlBuildClass();
        clientClassGenerator.CreateClientClass();

        createTestsGenerator.CreateCreateTests();
        queryTestsGenerator.CreateQueryTests();
        updateTestsGenerator.CreateUpdateTests();
        deleteTestsGenerator.CreateDeleteTests();
        subscriptionTestsGenerator.CreateSubscriptionTests();
    }
}
