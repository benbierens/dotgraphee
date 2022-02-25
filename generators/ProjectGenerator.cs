using System.Collections.Generic;
using System.Linq;

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

        AddSourceAssembly();
        AddIntegrationTestAssembly();
    }

    private void AddSourceAssembly()
    {
        RunCommand("dotnet", "new", "graphql", "-o", Config.Output.SourceFolder);
        BumpProjectToDotNetSix();
        InstallPackages(Config.SourcePackages, Config.Output.SourceFolder);
        RunCommand("dotnet", "sln", "add", Config.Output.SourceFolder + "/" + Config.Output.SourceFolder + ".csproj");
        DeleteFile(Config.Output.SourceFolder, "Query.cs");
        RunCommand("dotnet", "tool", "install", "--global", "dotnet-ef");
    }

    private void AddIntegrationTestAssembly()
    {
        RunCommand("dotnet", "new", "nunit", "-o", Config.Output.IntegrationTestFolder);
        RunCommand("dotnet", "sln", "add", Config.Output.IntegrationTestFolder + "/" + Config.Output.IntegrationTestFolder + ".csproj");
        RunCommand("dotnet", "add", Config.Output.IntegrationTestFolder, "reference", Config.Output.SourceFolder + "/" + Config.Output.SourceFolder + ".csproj");
        InstallPackages(Config.IntegrationTestPackages, Config.Output.IntegrationTestFolder);
        DeleteFile(Config.Output.IntegrationTestFolder, "UnitTest1.cs");
    }

    public void ModifyDefaultFiles()
    {
        ModifyStartupFile();
        ModifyIntegrationTestProjectFile();
    }

    public void FormatCode()
    {
        RunCommand("dotnet", "format");
    }

    private void BumpProjectToDotNetSix()
    {
        var mf = ModifyFile(Config.Output.SourceFolder, Config.Output.SourceFolder + ".csproj");
        mf.ReplaceLine("<TargetFramework>net5.0</TargetFramework>",
            "<TargetFramework>net6.0</TargetFramework>");

        mf.Modify();
    }

    private void InstallPackages(string[] packages, string folder)
    {
        foreach (var p in packages)
        {
            RunCommand("dotnet", "add", folder, "package", p);
        }
    }

    private void ModifyStartupFile()
    {
        var mf = ModifyFile(Config.Output.SourceFolder, "Startup.cs");
        mf.AddUsing(Config.GenerateNamespace);
        mf.AddUsing("HotChocolate.AspNetCore");
        

        mf.Insert(22, 3, "services.Add(ServiceDescriptor.Transient<I" + Config.Database.DbAccesserClassName + ", " + Config.Database.DbAccesserClassName + ">());");
        //mf.Insert(23, 3, "services.AddPooledDbContextFactory<" + Config.Database.DbContextClassName + ">(options => { });");

        mf.ReplaceLine(".AddQueryType<Query>();", GetServiceDecorators().ToArray());
                
        mf.Insert(25 + GetVariableServiceDecoratorLines(), 3, "services.AddInMemorySubscriptions();");

        mf.ReplaceLine("app.UseDeveloperExceptionPage();",
            "app.UsePlayground();",
            "app.UseDeveloperExceptionPage();");

        mf.ReplaceLine("app.UseRouting();", 
            "app.UseRouting();", 
            "app.UseWebSockets();",
            "DbService.EnsureCreated();");

        mf.Modify();

        GeneratorPager();
    }

    private void ModifyIntegrationTestProjectFile()
    {
        var mf = ModifyFile(Config.Output.IntegrationTestFolder, Config.Output.IntegrationTestFolder + ".csproj");
        mf.ReplaceLine("<IsPackable>false</IsPackable>",
            "<IsPackable>false</IsPackable>",
            "<Nullable>enable</Nullable>");

        mf.Modify();
    }

    private IEnumerable<string> GetServiceDecorators()
    {
        if (Models.Any(m => m.HasPagingFeature())) yield return ".AddOffsetPagingProvider<OffsetPager>()";
        if (Models.Any(m => HasAnyNavigationalProperties(m))) yield return ".AddProjections()";
        if (Models.Any(m => m.HasFilteringFeature())) yield return ".AddFiltering()";
        if (Models.Any(m => m.HasSortingFeature())) yield return ".AddSorting()";

        yield return ".AddQueryType<" + Config.GraphQl.GqlQueriesClassName + ">()";
        yield return ".AddMutationType<" + Config.GraphQl.GqlMutationsClassName + ">()";
        yield return ".AddSubscriptionType<" + Config.GraphQl.GqlSubscriptionsClassName + ">();";
    }

    private int GetVariableServiceDecoratorLines()
    {
        var result = 3;

        //if (model.HasPagingFeature()) cm.AddLine("[UsePaging]");
        //if (HasAnyNavigationalProperties(model)) cm.AddLine("[UseProjection]");
        //if (model.HasFilteringFeature()) cm.AddLine("[UseFiltering]");
        //if (model.HasSortingFeature()) cm.AddLine("[UseSorting]");

        if (Models.Any(m => m.HasPagingFeature())) result++;
        if (Models.Any(m => HasAnyNavigationalProperties(m))) result++;
        if (Models.Any(m => m.HasFilteringFeature())) result++;
        if (Models.Any(m => m.HasSortingFeature())) result++;

        return result;
    }

    private void GeneratorPager()
    {
        if (!Models.Any(m => m.HasPagingFeature())) return;

        var name = "OffsetPager";
        var fm = StartSrcFile("", name);
        fm.AddUsing("HotChocolate.Internal");
        fm.AddUsing("HotChocolate.Types.Pagination");

        var cm = fm.AddClass(name);
        cm.AddInherrit("OffsetPagingProvider");

        cm.AddClosure("public override bool CanHandle(IExtendedType source)", liner =>
        {
            liner.Add("throw new System.NotImplementedException();");
        });

        cm.AddClosure("protected override OffsetPagingHandler CreateHandler(IExtendedType source, PagingOptions options)", liner =>
        {
            liner.Add("throw new System.NotImplementedException();");
        });

        fm.Build();
    }
}
