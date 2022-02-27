public class UnitTestGenerator : BaseGenerator
{
    private readonly UnitTestDataGenerator unitTestDataGenerator;
    private readonly BaseUnitTestClassGenerator baseUnitTestGenerator;
    private readonly QueriesUnitTestsGenerator queriesUnitTestsGenerator;
    private readonly SubscriptionsUnitTestsGenerator subscriptionsUnitTestsGenerator;
    private readonly MutationsUnitTestsGenerator mutationsUnitTestsGenerator;

    public UnitTestGenerator(GeneratorConfig config)
        : base(config)
    {
        unitTestDataGenerator = new UnitTestDataGenerator(config);
        baseUnitTestGenerator = new BaseUnitTestClassGenerator(config);
        queriesUnitTestsGenerator = new QueriesUnitTestsGenerator(config);
        subscriptionsUnitTestsGenerator = new SubscriptionsUnitTestsGenerator(config);
        mutationsUnitTestsGenerator = new MutationsUnitTestsGenerator(config);
    }

    public void GenerateUnitTests()
    {
        MakeUnitTestDir(Config.Output.GeneratedFolder);
        unitTestDataGenerator.GenerateUnitTestData();
        baseUnitTestGenerator.GenerateBaseUnitTestClass();

        MakeUnitTestDir(Config.Output.GeneratedFolder, Config.Output.GraphQlSubFolder);
        queriesUnitTestsGenerator.GenerateQueriesUnitTests();
        subscriptionsUnitTestsGenerator.GenerateSubscriptionsUnitTests();
        mutationsUnitTestsGenerator.GenerateMutationUnitTests();
    }
}
