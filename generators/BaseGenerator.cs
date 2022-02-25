using System.Diagnostics;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;

public class BaseGenerator
{
    public BaseGenerator(GeneratorConfig config)
    {
        Config = config.Config;
        Models = config.Models;
    }

    public GeneratorConfig.ConfigSection Config { get; private set; }
    public GeneratorConfig.ModelConfig[] Models { get; private set; }

    protected FileMaker StartSrcFile(string subfolder, string filename)
    {
        var f = Path.Join(Config.Output.ProjectRoot, Config.Output.SourceFolder, Config.Output.GeneratedFolder, subfolder, filename + ".cs");
        return new FileMaker(Config, f, Config.GenerateNamespace);
    }

    protected FileMaker StartIntegrationTestUtilsFile(string filename)
    {
        var f = Path.Join(Config.Output.ProjectRoot, Config.Output.IntegrationTestFolder, Config.IntegrationTests.UtilsFolder, filename + ".cs");
        return new FileMaker(Config, f, Config.Output.IntegrationTestFolder);
    }

    protected FileMaker StartIntegrationTestFile(string filename)
    {
        var f = Path.Join(Config.Output.ProjectRoot, Config.Output.IntegrationTestFolder, filename + ".cs");
        return new FileMaker(Config, f, Config.Output.IntegrationTestFolder);
    }

    protected FileMaker StartUnitTestFile(string filename, string subFolder)
    {
        var f = Path.Join(Config.Output.ProjectRoot, Config.Output.UnitTestFolder, Config.Output.GeneratedFolder, subFolder, filename + ".Tests.cs");
        return new FileMaker(Config, f, Config.Output.UnitTestFolder);
    }

    protected CodeFileModifier ModifyFile(string filename)
    {
        return ModifyFile("", filename);
    }

    protected CodeFileModifier ModifyFile(string subfolder, string filename)
    {
        var f = Path.Join(Config.Output.ProjectRoot, subfolder, filename);
        return new CodeFileModifier(f);
    }

    protected ClassMaker StartClass(FileMaker fm, string className)
    {
        return fm.AddClass(className);
    }

    protected void MakeDir(params string[] path)
    {
        var arr = new[] { Config.Output.ProjectRoot }.Concat(path).ToArray();
        var p = Path.Join(arr);
        if (!Directory.Exists(p))
        {
            Directory.CreateDirectory(p);
        }
    }

    protected void MakeSrcDir(params string[] path)
    {
        var arr = new[] { Config.Output.SourceFolder }.Concat(path).ToArray();
        MakeDir(arr);
    }

    protected void MakeIntegrationTestDir(params string[] path)
    {
        var arr = new[] { Config.Output.IntegrationTestFolder }.Concat(path).ToArray();
        MakeDir(arr);
    }

    protected void MakeUnitTestDir(params string[] path)
    {
        var arr = new[] { Config.Output.UnitTestFolder }.Concat(path).ToArray();
        MakeDir(arr);
    }

    protected void WriteRawFile(Action<Liner> onLiner, params string[] filePath)
    {
        var arr = new[] { Config.Output.ProjectRoot }.Concat(filePath).ToArray();
        var liner = new Liner();
        onLiner(liner);
        File.WriteAllLines(Path.Combine(arr), liner.GetLines());
    }

    protected void DeleteFile(params string[] path)
    {
        var arr = new[] { Config.Output.ProjectRoot }.Concat(path).ToArray();
        File.Delete(Path.Join(arr));
    }

    public ForeignProperty[] GetForeignProperties(GeneratorConfig.ModelConfig model)
    {
        var manyFp = Models.Where(m => m.HasMany.Contains(model.Name)).Select(m => m.Name).ToArray();
        var oneFp = Models.Where(m => m.HasOne.Contains(model.Name)).Select(m => m.Name).ToArray();

        return GetForeignPropertiesForModelNames(model, manyFp, true, false)
             .Concat(GetForeignPropertiesForModelNames(model, oneFp, false, false))
             .ToArray();
    }

    protected bool IsRequiredSubModel(GeneratorConfig.ModelConfig me)
    {
        return Models.Any(m => m.HasOne.Contains(me.Name));
    }

    protected bool HasRequiredSubModels(GeneratorConfig.ModelConfig me)
    {
        return me.HasOne.Any();
    }

    protected bool HasAnyNavigationalPropertiesOrFeatures(GeneratorConfig.ModelConfig m)
    {
        return HasAnyNavigationalProperties(m) ||
            m.HasSortingFeature() ||
            m.HasFilteringFeature() ||
            m.HasPagingFeature();
    }

    protected bool HasAnyNavigationalProperties(GeneratorConfig.ModelConfig model)
    {
        if (model.HasMany.Any()) return true;
        if (model.HasOne.Any()) return true;
        if (model.MaybeHasOne.Any()) return true;
        if (Models.Any(m =>
            m.HasMany.Contains(model.Name) ||
            m.HasOne.Contains(model.Name)
        )) return true; 

        return false;
    }

    protected GeneratorConfig.ModelConfig[] GetMyRequiredSubModels(GeneratorConfig.ModelConfig me)
    {
        return Models.Where(m => me.HasOne.Contains(m.Name)).ToArray();
    }

    protected GeneratorConfig.ModelConfig[] GetMyRequiredSuperModels(GeneratorConfig.ModelConfig me)
    {
        return Models.Where(m => m.HasOne.Contains(me.Name)).ToArray();
    }

    protected GeneratorConfig.ModelConfig[] GetMyOptionalSubModels(GeneratorConfig.ModelConfig me)
    {
        return Models.Where(m => me.MaybeHasOne.Contains(m.Name)).ToArray();
    }

    protected GeneratorConfig.ModelConfig[] GetMyOptionalSuperModels(GeneratorConfig.ModelConfig me)
    {
        return Models.Where(m => m.MaybeHasOne.Contains(me.Name)).ToArray();
    }

    protected void RunCommand(string cmd, params string[] args)
    {
        var info = new ProcessStartInfo();
        info.Arguments = string.Join(" ", args);
        info.FileName = cmd;
        info.WorkingDirectory = Config.Output.ProjectRoot;
        var p = Process.Start(info);
        p.WaitForExit();
    }

    protected void AddModelFields(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        foreach (var f in model.Fields)
        {
            cm.AddUsing(TypeUtils.GetTypeRequiredUsing(f.Type));
            cm.AddProperty(f.Name)
                .IsType(f.Type)
                .Build();
        }
    }

    protected InputTypeNames GetInputTypeNames(GeneratorConfig.ModelConfig model)
    {
        return new InputTypeNames
        {
            Create = Config.GraphQl.GqlMutationsCreateMethod + model.Name + Config.GraphQl.GqlMutationsInputTypePostfix,
            Update = Config.GraphQl.GqlMutationsUpdateMethod + model.Name + Config.GraphQl.GqlMutationsInputTypePostfix,
            Delete = Config.GraphQl.GqlMutationsDeleteMethod + model.Name + Config.GraphQl.GqlMutationsInputTypePostfix
        };
    }

    protected string GetNullabilityTypePostfix()
    {
        var strategy = Config.GetFailedToFindStrategy();
        switch (strategy)
        {
            case GeneratorConfig.FailedToFindStrategy.useNullObject:
                return "?";

            case GeneratorConfig.FailedToFindStrategy.useErrorCode:
                return "";
        }

        throw new Exception("Unknown FailedToFindStrategy: " + strategy);
    }

    public bool IsFailedToFindStrategyNullObject()
    {
        return Config.GetFailedToFindStrategy() == GeneratorConfig.FailedToFindStrategy.useNullObject;
    }

    public bool IsFailedToFindStrategyErrorCode()
    {
        return Config.GetFailedToFindStrategy() == GeneratorConfig.FailedToFindStrategy.useErrorCode;
    }

    protected string GetErrorOrNull()
    {
        if (IsFailedToFindStrategyErrorCode()) return "Error";
        if (IsFailedToFindStrategyNullObject()) return "Null";
        throw new Exception("Unknown FailedToFind strategy");
    }

    protected void IterateModelsInDependencyOrder(Action<GeneratorConfig.ModelConfig> onModel)
    {
        var remainingModels = Models.ToList();
        var initialized = new List<string>();

        while (remainingModels.Count > 0)
        {
            var model = remainingModels[0];
            remainingModels.RemoveAt(0);

            if (CanInitialize(model, initialized))
            {
                onModel(model);
                initialized.Add(model.Name);
            }
            else
            {
                remainingModels.Add(model);
            }
        }
    }


    private ForeignProperty[] GetForeignPropertiesForModelNames(GeneratorConfig.ModelConfig model, string[] names, bool isPlural, bool isOptional)
    {
        return names.Select(f => new ForeignProperty
        {
            Type = f,
            Name = GetForeignPropertyPrefix(model, f) + f,
            WithId = GetForeignPropertyPrefix(model, f) + f + "Id",
            IsSelfReference = IsSelfReference(model, f),
            IsPlural = isPlural,
            IsOptional = isOptional
        }).ToArray();
    }

    private string GetForeignPropertyPrefix(GeneratorConfig.ModelConfig m, string hasManyEntry)
    {
        if (IsSelfReference(m, hasManyEntry)) return Config.SelfRefNavigationPropertyPrefix;
        return "";
    }

    private bool IsSelfReference(GeneratorConfig.ModelConfig model, string hasManyEntry)
    {
        return hasManyEntry == model.Name;
    }


    private bool CanInitialize(GeneratorConfig.ModelConfig m, List<string> initialized)
    {
        var foreign = GetForeignProperties(m);
        return foreign.All(f => f.IsSelfReference || initialized.Contains(f.Name));
    }
}
