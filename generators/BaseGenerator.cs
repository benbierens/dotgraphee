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

    protected GeneratorConfig.ConfigSection Config { get; private set; }
    protected GeneratorConfig.ModelConfig[] Models { get; private set; }

    protected FileMaker StartSrcFile(string subfolder, string filename)
    {
        var f = Path.Join(Config.Output.ProjectRoot, Config.Output.SourceFolder, Config.Output.GeneratedFolder, subfolder, filename + ".cs");
        return new FileMaker(f, Config.GenerateNamespace);
    }

    protected FileMaker StartTestUtilsFile(string filename)
    {
        var f = Path.Join(Config.Output.ProjectRoot, Config.Output.TestFolder, Config.Tests.SubFolder, Config.Tests.UtilsFolder, filename + ".cs");
        return new FileMaker(f, Config.Output.TestFolder);
    }

    protected FileMaker StartTestFile(string filename)
    {
        var f = Path.Join(Config.Output.ProjectRoot, Config.Output.TestFolder, Config.Tests.SubFolder, filename + ".cs");
        return new FileMaker(f, Config.Output.TestFolder);
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

    protected void MakeTestDir(params string[] path)
    {
        var arr = new[] { Config.Output.TestFolder }.Concat(path).ToArray();
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

    protected ForeignProperty[] GetForeignProperties(GeneratorConfig.ModelConfig model)
    {
        var fp = Models.Where(m => m.HasMany != null && m.HasMany.Contains(model.Name)).Select(m => m.Name).ToArray();

        return fp.Select(f => new ForeignProperty
        {
            Type = f,
            Name = GetForeignPropertyPrefix(model, f) + f,
            WithId = GetForeignPropertyPrefix(model, f) + f + "Id",
            IsSelfReference = IsSelfReference(model, f)

        }).ToArray();
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

    protected void AddEntityFieldAsserts(Liner liner, GeneratorConfig.ModelConfig m, string errorMessage)
    {
        foreach (var f in m.Fields)
        {
            liner.Add("Assert.That(entity." + f.Name + ", Is.EqualTo(TestData.Test" + m.Name + "." + f.Name + "), \"" + errorMessage + " " + m.Name + "." + f.Name + "\");");
        }
        var foreignProperties = GetForeignProperties(m);
        foreach (var f in foreignProperties)
        {
            if (!f.IsSelfReference)
            {
                liner.Add("Assert.That(entity." + f.WithId + ", Is.EqualTo(TestData.Test" + f.Type + ".Id), \"" + errorMessage+ " " + m.Name + "." + f.WithId + "\");");
            }
        }
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
