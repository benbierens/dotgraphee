public class ProjectGenerator : BaseGenerator
{
    public ProjectGenerator(GeneratorConfig config)
        :base(config)
    {
    }

    public void CreateDotNetProject()
    {
        RunCommand("dotnet", "new", "-i", "HotChocolate.Templates.Server");
        RunCommand("dotnet", "new", "sln");
        RunCommand("dotnet", "new", "graphql", "-o", Config.Output.SourceFolder);

        BumpProjectToDotNetSix();


        foreach (var p in Config.Packages)
        {
            RunCommand("dotnet", "add", Config.Output.SourceFolder, "package", p);
        }

        RunCommand("dotnet", "new", "nunit", "-o", Config.Output.TestFolder);
        RunCommand("dotnet", "sln", "add", Config.Output.SourceFolder + "/" + Config.Output.SourceFolder + ".csproj");
        RunCommand("dotnet", "sln", "add", Config.Output.TestFolder + "/" + Config.Output.TestFolder + ".csproj");
        RunCommand("dotnet", "add", Config.Output.TestFolder, "reference", Config.Output.SourceFolder + "/" + Config.Output.SourceFolder + ".csproj");

        RunCommand("dotnet", "tool", "install", "--global", "dotnet-ef");

        DeleteFile(Config.Output.SourceFolder, "Query.cs");
    }

    public void ModifyDefaultFiles()
    {
        ModifyStartupFile();
        ModifyTestProjectFile();
    }

    private void BumpProjectToDotNetSix()
    {
        var mf = ModifyFile(Config.Output.SourceFolder, Config.Output.SourceFolder + ".csproj");
        mf.ReplaceLine("<TargetFramework>net5.0</TargetFramework>",
            "<TargetFramework>net6.0</TargetFramework>");

        mf.Modify();
    }

    private void ModifyStartupFile()
    {
        var mf = ModifyFile(Config.Output.SourceFolder, "Startup.cs");
        mf.AddUsing(Config.GenerateNamespace);
        mf.AddUsing("HotChocolate.AspNetCore");

        mf.ReplaceLine(".AddQueryType<Query>();",
                ".AddQueryType<" + Config.GraphQl.GqlQueriesClassName + ">()",
                ".AddMutationType<" + Config.GraphQl.GqlMutationsClassName + ">()",
                ".AddSubscriptionType<" + Config.GraphQl.GqlSubscriptionsClassName + ">();",
                "",
                "services.AddInMemorySubscriptions();");

        mf.ReplaceLine("app.UseDeveloperExceptionPage();",
            "app.UsePlayground();",
            "app.UseDeveloperExceptionPage();");

        mf.ReplaceLine("app.UseRouting();", 
            "app.UseRouting();", 
            "app.UseWebSockets();",
            "Db.Context.Database.EnsureCreated();");

        mf.Modify();
    }

    private void ModifyTestProjectFile()
    {
        var mf = ModifyFile(Config.Output.TestFolder, "test.csproj");
        mf.ReplaceLine("<IsPackable>false</IsPackable>",
            "<IsPackable>false</IsPackable>",
            "<Nullable>enable</Nullable>");

        mf.Modify();
    }
}
