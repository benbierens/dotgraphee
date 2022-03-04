using System.Collections.Generic;

public class UnitTestInputGenerator : BaseGenerator
{
    public UnitTestInputGenerator(GeneratorConfig config) 
        : base(config)
    {
    }

    public void GenerateUnitTestInput()
    {
        var fm = StartUnitTestUtilsFile("UnitTestInput");
        fm.AddUsing(Config.GenerateNamespace);
        var cm = fm.AddClass("UnitTestInput");
        cm.Modifiers.Clear();

        foreach (var m in Models)
        {
            var inputNames = GetInputTypeNames(m);
            cm.AddProperty(inputNames.Create)
                .IsType(inputNames.Create)
                .NoInitializer()
                .Build();
            cm.AddProperty(inputNames.Update)
                .IsType(inputNames.Update)
                .NoInitializer()
                .Build();
            if (!IsRequiredSubModel(m))
            {
                cm.AddProperty(inputNames.Delete)
                    .IsType(inputNames.Delete)
                    .NoInitializer()
                    .Build();
            }
        }
        cm.AddBlankLine();

        AddConstructor(cm);

        fm.Build();
    }

    private void AddConstructor(ClassMaker cm)
    {
        cm.AddClosure("public UnitTestInput(TestData data)", liner =>
        {
            foreach (var m in Models)
            {
                var inputNames = GetInputTypeNames(m);

                var args = GetCreateInputArguments(m);
                liner.Add(inputNames.Create + " = data.To" + inputNames.Create + "(" + args + ");");
                liner.Add(inputNames.Update + " = data.To" + inputNames.Update + "();");
                if (!IsRequiredSubModel(m))
                {
                    liner.Add(inputNames.Delete + " = data.To" + inputNames.Delete + "();");
                }
            }
        });
    }

    private string GetCreateInputArguments(GeneratorConfig.ModelConfig m)
    {
        var args = new List<string>();
        var foreignProperties = GetForeignProperties(m);
        foreach (var f in foreignProperties)
        {
            if (!f.IsRequiredSingular())
            {
                if (f.IsSelfReference)
                {
                    args.Add("null");
                }
                else
                {
                    args.Add("data." + f.Type + "1.Id");
                }
            }
        }

        return string.Join(", ", args);
    }
}
