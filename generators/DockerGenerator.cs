public class DockerGenerator : BaseGenerator
{
    private const string dockerFolder = "docker";

    public DockerGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    private GeneratorConfig.ConfigDatabaseConnectionDockerSection DockerDb
    {
        get
        {
            return Config.Database.Docker;
        }
    }

    public void GenerateDockerFiles()
    {
        MakeDir(dockerFolder);

        WriteRawFile(liner =>
        {
            liner.Add("FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build");
            liner.Add("WORKDIR /app");
            liner.Add("COPY *.sln ./");
            liner.Add("COPY " + Config.Output.DomainFolder + " ./" + Config.Output.DomainFolder);
            liner.Add("COPY " + Config.Output.SourceFolder + " ./" + Config.Output.SourceFolder);
            liner.Add("RUN dotnet publish ./" + Config.Output.SourceFolder + " -c Release");
            liner.AddBlankLine();
            liner.Add("FROM mcr.microsoft.com/dotnet/aspnet:6.0");
            liner.Add("WORKDIR /app");
            liner.Add("COPY --from=build /app/" + Config.Output.SourceFolder + "/bin/Release/net6.0/publish/ ./");
            liner.Add("ENTRYPOINT [\"dotnet\", \"" + Config.Output.SourceFolder + ".dll\"]");
        }, dockerFolder, "Dockerfile");

        WriteRawFile(liner =>
        {
            liner.Add("version: '3'");
            liner.Add("services:");
            liner.Indent();
            AddDatabaseService(liner);
            AddGraphqlService(liner);
            liner.Deindent();

            liner.Add("volumes:");
            liner.Indent();
            liner.Add("db-data:");
            liner.Indent();
            liner.Add("driver: local");
            liner.Deindent();
            liner.Deindent();
        }, "docker-compose.yml");
    }

    private void AddDatabaseService(Liner liner)
    {
        liner.Add(Config.Database.DbContainerName + ":");
        liner.Indent();

        liner.Add("container_name: " + Config.Database.DbContainerName);
        liner.Add("image: postgres");
        liner.Add("restart: always");
        liner.Add("environment:");
        liner.Indent();
        liner.Add("- POSTGRES_PASSWORD=" + DockerDb.DbPassword);
        liner.Add("- POSTGRES_DB=" + DockerDb.DbName);
        liner.Deindent();

        liner.Add("volumes:");
        liner.Indent();
        liner.Add("- db-data:/var/lib/postgresql");
        liner.Deindent();

        liner.Deindent();
    }

    private void AddGraphqlService(Liner liner)
    {
        liner.Add("graphql:");
        liner.Indent();

        liner.Add("build:");
        liner.Indent();
        liner.Add("context: .");
        liner.Add("dockerfile: ./docker/Dockerfile");
        liner.Deindent();
        liner.Add("environment:");
        liner.Indent();
        liner.Add("- HOST=localhost");
        liner.Add("- DB_HOST=" + Config.Database.DbContainerName);
        liner.Add("- DB_DATABASENAME=" + DockerDb.DbName);
        liner.Add("- DB_USERNAME=" + DockerDb.DbUsername);
        liner.Add("- DB_PASSWORD=" + DockerDb.DbPassword);
        liner.Deindent();
        liner.Add("ports:");
        liner.Indent();
        liner.Add("- \"80:80\"");
        liner.Deindent();
        liner.Add("depends_on:");
        liner.Indent();
        liner.Add("- " + Config.Database.DbContainerName);
        liner.Deindent();

        liner.Deindent();
    }
}
