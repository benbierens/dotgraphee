public class GraphQlTypesGenerator : BaseGenerator
{
    public GraphQlTypesGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateGraphQlTypes()
    {
        var fm = StartSrcFile(Config.Output.GraphQlSubFolder, Config.GraphQl.GqlTypesFileName);

        foreach (var model in Models)
        {
            var inputTypeNames = GetInputTypeNames(model);

            var createClass = StartClass(fm, inputTypeNames.Create);
            AddModelFields(createClass, model);
            AddForeignIdProperties(createClass, model);
            AddSubModelInputProperties(createClass, model);
            createClass.AddBlankLine();
            AddToDtoMethod(createClass, model);

            var updateClass = StartClass(fm, inputTypeNames.Update);
            updateClass.AddProperty(model.Name + "Id")
                .IsType(Config.IdType)
                .Build();
            AddModelFields(updateClass, model);
            AddForeignIdProperties(updateClass, model);
            AddOptionalSubModelIds(updateClass, model);

            if (!IsRequiredSubModel(model))
            {
                var deleteClass = StartClass(fm, inputTypeNames.Delete);
                deleteClass.AddProperty(model.Name + "Id")
                    .IsType(Config.IdType)
                    .Build();
            }
        }

        fm.Build();
    }

    private void AddOptionalSubModelIds(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        var optionalSubModels = GetMyOptionalSubModels(model);
        foreach (var subModel in optionalSubModels)
        {
            cm.AddProperty(subModel.Name + "Id")
                .IsType(Config.IdType)
                .IsNullable()
                .Build();
        }
    }

    private void AddToDtoMethod(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        cm.AddClosure("public " + model.Name + " ToDto()", liner =>
        {
            var requiredSubModels = GetMyRequiredSubModels(model);
            var optionalSubModels = GetMyOptionalSubModels(model);

            liner.StartClosure("return new " + model.Name);
            AddModelInitializer(liner, model);
            foreach (var subModel in requiredSubModels)
            {
                liner.Add(subModel.Name + " = " + subModel.Name + ".ToDto(),");
            }
            foreach (var subModel in optionalSubModels)
            {
                liner.Add(subModel.Name + "Id = " + subModel.Name + "Id,");
            }

            liner.EndClosure(";");
        });
    }

    private void AddModelInitializer(Liner liner, GeneratorConfig.ModelConfig model, string inputName = null)
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

    private void AddSubModelInputProperties(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        var required = GetMyRequiredSubModels(model);
        foreach (var subModel in required)
        {
            cm.AddProperty(subModel.Name)
                .IsType(Config.GraphQl.GqlMutationsCreateMethod + subModel.Name + Config.GraphQl.GqlMutationsInputTypePostfix)
                .InitializeAsExplicitNull()
                .Build();
        }

        var optional = GetMyOptionalSubModels(model);
        foreach (var subModel in optional)
        {
            cm.AddProperty(subModel.Name + "Id")
                .IsType(Config.IdType)
                .IsNullable()
                .Build();
        }
    }

    private void AddForeignIdProperties(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        var foreignProperties = GetForeignProperties(model);
        foreach (var f in foreignProperties)
        {
            if (!f.IsRequiredSingular())
            {
                if (f.IsSelfReference)
                {
                    cm.AddProperty(f.WithId)
                        .IsType(Config.IdType)
                        .IsNullable()
                        .Build();
                }
                else
                {
                    cm.AddProperty(f.WithId)
                        .IsType(Config.IdType)
                        .Build();
                }
            }
        }
    }
}
