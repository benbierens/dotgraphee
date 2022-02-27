public class InputConverterClassGenerator : BaseGenerator
{
    public InputConverterClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateInputConverter()
    {
        var fm = StartSrcFile(Config.Output.GraphQlSubFolder, "InputConverter");

        AddInputConverterInterface(fm);
        AddInputConverterClass(fm);

        fm.Build();
    }

    private void AddInputConverterInterface(FileMaker fm)
    {
        var im = fm.AddInterface("IInputConverter");

        foreach (var m in Models)
        {
            var inputTypes = GetInputTypeNames(m);
            im.AddLine(m.Name + " ToDto(" + inputTypes.Create + " input);");
        }
    }

    private void AddInputConverterClass(FileMaker fm)
    {
        var cm = fm.AddClass("InputConverter");
        cm.Modifiers.Clear();
        cm.AddInherrit("IInputConverter");

        foreach (var m in Models)
        {
            var inputTypes = GetInputTypeNames(m);
            AddToDtoMethod(cm, m, inputTypes);
        }
    }

    private void AddToDtoMethod(ClassMaker cm, GeneratorConfig.ModelConfig model, InputTypeNames inputTypes)
    {
        cm.AddClosure("public " + model.Name + " ToDto(" + inputTypes.Create + " input)", liner =>
        {
            var requiredSubModels = GetMyRequiredSubModels(model);
            var optionalSubModels = GetMyOptionalSubModels(model);

            liner.StartClosure("return new " + model.Name);
            AddEntityIdInitializer(liner);
            AddModelInitializer(liner, model, "input");
            foreach (var subModel in requiredSubModels)
            {
                liner.Add(subModel.Name + " = ToDto(input." + subModel.Name + "),");
            }
            foreach (var subModel in optionalSubModels)
            {
                liner.Add(subModel.Name + "Id = input." + subModel.Name + "Id,");
            }

            liner.EndClosure(";");
        });
    }

    private void AddEntityIdInitializer(Liner liner)
    {
        if (Config.IdType != "string") return;
        liner.Add("Id = Guid.NewGuid().ToString(),");
    }

    private void AddModelInitializer(Liner liner, GeneratorConfig.ModelConfig model, string inputName)
    {
        var addresser = GetModelInitializerAddresser(inputName);

        foreach (var field in model.Fields)
        {
            liner.Add(field.Name + " = " + addresser + field.Name + ",");
        }
        var foreignProperties = GetForeignProperties(model);
        foreach (var f in foreignProperties)
        {
            if (!f.IsRequiredSingular())
            {
                liner.Add(f.WithId + " = " + addresser + f.WithId + ",");
            }
        }
    }

    private string GetModelInitializerAddresser(string inputName)
    {
        if (inputName == null) return "";
        return inputName + ".";
    }
}
