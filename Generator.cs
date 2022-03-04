
public class Generator : BaseGenerator
{
    private readonly ProjectGenerator projectGenerator;
    private readonly DtoGenerator dtoGenerator;
    private readonly DatabaseGenerator databaseGenerator;
    private readonly GraphQlGenerator graphQlGenerator;
    private readonly DockerGenerator dockerGenerator;
    private readonly ReadmeGenerator readmeGenerator;
    private readonly IntegrationTestGenerator integrationTestGenerator;
    private readonly UnitTestGenerator unitTestGenerator;

    public Generator(GeneratorConfig config)
        : base(config)
    {
        projectGenerator = new ProjectGenerator(config);
        dtoGenerator = new DtoGenerator(config);
        databaseGenerator = new DatabaseGenerator(config);
        graphQlGenerator = new GraphQlGenerator(config);
        dockerGenerator = new DockerGenerator(config);
        readmeGenerator = new ReadmeGenerator(config);
        integrationTestGenerator = new IntegrationTestGenerator(config);
        unitTestGenerator = new UnitTestGenerator(config);
    }

    public void Generate()
    {
        MakeDir();
        MakeDir(Config.Output.SourceFolder);
        MakeDir(Config.Output.IntegrationTestFolder);
        MakeDir(Config.Output.UnitTestFolder);

        projectGenerator.CreateDotNetProject();

        MakeSrcDir(Config.Output.GeneratedFolder);
        dtoGenerator.GenerateDtos();
        databaseGenerator.GenerateDbContext();
        graphQlGenerator.GenerateGraphQl();

        projectGenerator.ModifyDefaultFiles();

        dockerGenerator.GenerateDockerFiles();
        integrationTestGenerator.GenerateIntegrationTests();
        unitTestGenerator.GenerateUnitTests();
        readmeGenerator.GenerateReadme();

        databaseGenerator.CreateInitialMigration();

        projectGenerator.FormatCode();
    }
}
