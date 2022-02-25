public class UnitTestGenerator : BaseGenerator
{
    private readonly QueriesUnitTestsGenerator queriesUnitTestsGenerator;

    public UnitTestGenerator(GeneratorConfig config)
        : base(config)
    {
        queriesUnitTestsGenerator = new QueriesUnitTestsGenerator(config);
    }

    public void GenerateUnitTests()
    {
        MakeUnitTestDir(Config.Output.GeneratedFolder);

        MakeUnitTestDir(Config.Output.GeneratedFolder, Config.Output.GraphQlSubFolder);
        queriesUnitTestsGenerator.GenerateQueriesUnitTests();
    }
}
