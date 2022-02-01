public class ReadmeGenerator : BaseGenerator
{
    private const string dotgrapheeUrl = "https://github.com/benbierens/dotgraphee";

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
            Add2(liner, "# dotnet-GraphQl webservice");
            Add2(liner, "## Build Development:");
            Add2(liner, "`dotnet build " + src + "`");
            Add2(liner, "## Run:");
            Add2(liner, "`dotnet run --project " + src + "`");
            Add2(liner, "## Test:");
            Add2(liner, "### All:");
            Add2(liner, "`dotnet test " + test + "`");
            Add2(liner, "### Without GraphQl container tests:");
            Add2(liner, "`dotnet test " + test + " --filter TestCategory!=" + Config.Tests.TestCategory + "`");
            Add2(liner, "## Build Release & Run Docker Image:");
            Add1(liner, "`dotnet publish " + src + " -c release`");
            Add2(liner, "`docker-compose up -d`");
            Add2(liner, "## Migrate the database");
            Add1(liner, "`cd " + Config.Output.SourceFolder + "`");
            Add2(liner, "`dotnet ef database update`");
            Add2(liner, "## dotgraphee");
            Add1(liner, "This project was bootstrapped with dotgraphee: " + dotgrapheeUrl);
        }, "Readme.md");
    }

    private void Add1(Liner liner, string l)
    {
        liner.Add(l);
    }

    private void Add2(Liner liner, string l)
    {
        liner.Add(l);
        liner.AddBlankLine();
    }
}
