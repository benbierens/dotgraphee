public class ReadmeGenerator : BaseGenerator
{
    public ReadmeGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateReadme()
    {
        var src = Config.Output.SourceFolder;
        var test = Config.Output.TestFolder;

        WriteRawFile(liner =>
        {
            liner.Add("# DotNet-GraphQL Backend");
            liner.Add("## Build Development:");
            liner.Add("`dotnet build " + src + "`");
            liner.Add("## Run:");
            liner.Add("`dotnet run --project " + src + "`");
            liner.Add("## Test:");
            liner.Add("### All:");
            liner.Add("`dotnet test " + test + "`");
            liner.Add("### Without GraphQL Tests:");
            liner.Add("`dotnet test " + test + " --filter TestCategory!=" + Config.Tests.TestCategory + "`");
            liner.Add("## Build Release & Run Docker Image:");
            liner.Add("`dotnet publish " + src + " -c release`");
            liner.Add("`docker-compose up -d`");
            liner.Add("## Migrate the database");
            liner.Add("`cd " + Config.Output.SourceFolder + "`");
            liner.Add("`dotnet ef database update`");
        }, "Readme.md");
    }
}
