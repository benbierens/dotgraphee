public class UnitTestGenerator : BaseGenerator
{
    private readonly UnitTestDataGenerator unitTestDataGenerator;
    private readonly QueriesUnitTestsGenerator queriesUnitTestsGenerator;
    

    public UnitTestGenerator(GeneratorConfig config)
        : base(config)
    {
        unitTestDataGenerator = new UnitTestDataGenerator(config);
        queriesUnitTestsGenerator = new QueriesUnitTestsGenerator(config);
    }

    public void GenerateUnitTests()
    {
        MakeUnitTestDir(Config.Output.GeneratedFolder);
        unitTestDataGenerator.GenerateUnitTestData();

        MakeUnitTestDir(Config.Output.GeneratedFolder, Config.Output.GraphQlSubFolder);
        queriesUnitTestsGenerator.GenerateQueriesUnitTests();
    }
}
