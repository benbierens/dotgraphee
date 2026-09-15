
public class Generator : BaseGenerator
{
    private readonly ProjectGenerator projectGenerator;
    private readonly DomainGenerator domainGenerator;
    private readonly DatabaseGenerator databaseGenerator;
    private readonly GraphQlGenerator graphQlGenerator;
    private readonly DockerGenerator dockerGenerator;
    private readonly ReadmeGenerator readmeGenerator;
    private readonly GraphQlClientGenerator graphQlClientGenerator;
    private readonly IntegrationTestGenerator integrationTestGenerator;
    private readonly UnitTestGenerator unitTestGenerator;

    public Generator(GeneratorConfig config)
        : base(config)
    {
        projectGenerator = new ProjectGenerator(config);
        domainGenerator = new DomainGenerator(config);
        databaseGenerator = new DatabaseGenerator(config);
        graphQlGenerator = new GraphQlGenerator(config);
        dockerGenerator = new DockerGenerator(config);
        readmeGenerator = new ReadmeGenerator(config);
        graphQlClientGenerator = new GraphQlClientGenerator(config);
        integrationTestGenerator = new IntegrationTestGenerator(config);
        unitTestGenerator = new UnitTestGenerator(config);
    }

    public void Generate()
    {
        MakeDir();
        MakeDir(Config.Output.DomainFolder);
        MakeDir(Config.Output.SourceFolder);
        MakeDir(Config.Output.GraphQlClientFolder);
        MakeDir(Config.Output.IntegrationTestFolder);
        MakeDir(Config.Output.UnitTestFolder);

        projectGenerator.CreateDotNetProject();

        domainGenerator.GenerateDomain();
        databaseGenerator.GenerateDbContext();
        graphQlGenerator.GenerateGraphQl();

        projectGenerator.ModifyDefaultFiles();

        dockerGenerator.GenerateDockerFiles();
        graphQlClientGenerator.GenerateGraphQlClient();
        integrationTestGenerator.GenerateIntegrationTests();
        unitTestGenerator.GenerateUnitTests();
        readmeGenerator.GenerateReadme();

        databaseGenerator.CreateInitialMigration();
    }
}
