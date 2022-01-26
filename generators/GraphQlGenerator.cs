public class GraphQlGenerator : BaseGenerator
{
    public GraphQlGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateGraphQl()
    {
        MakeSrcDir(Config.Output.GeneratedFolder, Config.Output.GraphQlSubFolder);
        GenerateQueries();
        GenerateSubscriptions();
        GenerateTypes();
        GenerateMutations();
    }

    #region Queries

    private void GenerateQueries()
    {
        var fm = StartSrcFile(Config.Output.GraphQlSubFolder, Config.GraphQl.GqlQueriesFileName);
        var cm = StartClass(fm, Config.GraphQl.GqlQueriesClassName);
        cm.AddUsing("System.Threading.Tasks");
        cm.AddUsing("Microsoft.EntityFrameworkCore");

        foreach (var model in Models)
        {
            cm.AddClosure("public async Task<" + model.Name + "[]> " + model.Name + "s()", liner =>
            {
                liner.Add("return await " + Config.Database.DbAccesserClassName + ".Context." + model.Name + "s.ToArrayAsync();");
            });

            cm.AddClosure("public async Task<" + model.Name + "> " + model.Name + "(" + Config.IdType + " id)", liner =>
            {
                liner.Add("return await " + Config.Database.DbAccesserClassName + ".Context." + model.Name + "s.FindAsync(id);");
            });
        }

        fm.Build();
    }

    #endregion

    #region Subscriptions

    private void GenerateSubscriptions()
    {
        var fm = StartSrcFile(Config.Output.GraphQlSubFolder, Config.GraphQl.GqlSubscriptionsFilename);
        var cm = StartClass(fm, Config.GraphQl.GqlSubscriptionsClassName);
        cm.AddUsing("HotChocolate");
        cm.AddUsing("HotChocolate.Types");

        foreach (var model in Models)
        {
            AddSubscriptionMethod(cm, model.Name, Config.GraphQl.GqlSubscriptionCreatedMethod);
            AddSubscriptionMethod(cm, model.Name, Config.GraphQl.GqlSubscriptionUpdatedMethod);
            AddSubscriptionMethod(cm, model.Name, Config.GraphQl.GqlSubscriptionDeletedMethod);
        }

        fm.Build();
    }

    private void AddSubscriptionMethod(ClassMaker cm, string modelName, string method)
    {
        var n = modelName;
        var l = n.FirstToLower();
        cm.AddLine("[Subscribe]");
        cm.AddLine("public " + n + " " + n + method + "([EventMessage] " + n + " _" + l + ") => _" + l + ";");
        cm.AddBlankLine();
    }

    #endregion

    #region Types

    private void GenerateTypes()
    {
        var fm = StartSrcFile(Config.Output.GraphQlSubFolder, Config.GraphQl.GqlTypesFileName);

        foreach (var model in Models)
        {
            var inputTypeNames = GetInputTypeNames(model);

            var addClass = StartClass(fm, inputTypeNames.Create);
            AddModelFields(addClass, model);
            AddForeignIdProperties(addClass, model);

            var updateClass = StartClass(fm, inputTypeNames.Update);
            updateClass.AddProperty(model.Name + "Id")
                .IsType(Config.IdType)
                .Build();

            AddModelFieldsAsNullable(updateClass, model);
            AddForeignIdPropertiesAsNullable(updateClass, model);

            var deleteClass = StartClass(fm, inputTypeNames.Delete);
            deleteClass.AddProperty(model.Name + "Id")
                .IsType(Config.IdType)
                .Build();
        }

        fm.Build();
    }

    private void AddForeignIdProperties(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        var foreignProperties = GetForeignProperties(model);
        foreach (var f in foreignProperties)
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

    private void AddModelFieldsAsNullable(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        foreach (var f in model.Fields)
        {
            cm.AddProperty(f.Name)
                .IsType(f.Type)
                .IsNullable()
                .Build();
        }
    }

    private void AddForeignIdPropertiesAsNullable(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        var foreignProperties = GetForeignProperties(model);
        foreach (var f in foreignProperties)
        {
            cm.AddProperty(f.WithId)
                .IsType(Config.IdType)
                .IsNullable()
                .Build();
        }
    }

    #endregion

    #region Mutations

    private void GenerateMutations()
    {
        var fm = StartSrcFile(Config.Output.GraphQlSubFolder, Config.GraphQl.GqlMutationsFilename);
        var cm = StartClass(fm, Config.GraphQl.GqlMutationsClassName);
        cm.AddUsing("System.Threading.Tasks");
        cm.AddUsing("HotChocolate");
        cm.AddUsing("HotChocolate.Subscriptions");

        foreach (var model in Models)
        {
            var inputTypeNames = GetInputTypeNames(model);

            AddCreateMutation(cm, model, inputTypeNames);
            AddUpdateMutation(cm, model, inputTypeNames);
            AddDeleteMutation(cm, model, inputTypeNames);
        }

        fm.Build();
    }

    #region Create

    private void AddCreateMutation(ClassMaker cm, GeneratorConfig.ModelConfig model, InputTypeNames inputTypeNames)
    {
        cm.AddClosure("public async Task<" + model.Name + "> " + Config.GraphQl.GqlMutationsCreateMethod + model.Name +
        "(" + inputTypeNames.Create + " input, [Service] ITopicEventSender sender)", liner =>
        {
            liner.StartClosure("var createEntity = new " + model.Name);
            AddModelInitializer(liner, model, "input");
            liner.EndClosure(";");

            AddDatabaseAddAndSave(liner);

            liner.Add("await sender.SendAsync(\"" + model.Name + Config.GraphQl.GqlSubscriptionCreatedMethod + "\", createEntity);");
            liner.Add("return createEntity;");
        });
    }

    private void AddModelInitializer(Liner liner, GeneratorConfig.ModelConfig model, string inputName)
    {
        foreach (var field in model.Fields)
        {
            liner.Add(field.Name + " = " + inputName + "." + field.Name + ",");
        }
        var foreignProperties = GetForeignProperties(model);
        foreach (var f in foreignProperties)
        {
            liner.Add(f.WithId + " = " + inputName + "." + f.WithId + ",");
        }
    }

    private void AddDatabaseAddAndSave(Liner liner)
    {
        AddDbVar(liner);
        liner.Add("db.Add(createEntity);");
        liner.Add("db.SaveChanges();");
    }

    #endregion

    #region Update

    private void AddUpdateMutation(ClassMaker cm, GeneratorConfig.ModelConfig model, InputTypeNames inputTypeNames)
    {
        cm.AddClosure("public async Task<" + model.Name + "> " + Config.GraphQl.GqlMutationsUpdateMethod + model.Name +
        "(" + inputTypeNames.Update + " input, [Service] ITopicEventSender sender)", liner =>
        {
            AddDbVar(liner);
            liner.Add("var updateEntity = db.Set<" + model.Name + ">().Find(input." + model.Name + "Id);");
            AddModelUpdater(liner, model, "input");
            liner.Add("db.SaveChanges();");

            liner.Add("await sender.SendAsync(\"" + model.Name + Config.GraphQl.GqlSubscriptionUpdatedMethod + "\", updateEntity);");
            liner.Add("return updateEntity;");
        });
    }

    private void AddModelUpdater(Liner liner, GeneratorConfig.ModelConfig model, string inputName)
    {
        foreach (var field in model.Fields)
        {
            AddAssignmentLine(liner, field.Type, field.Name, inputName);
        }
        var foreignProperties = GetForeignProperties(model);
        foreach (var f in foreignProperties)
        {
            AddAssignmentLine(liner, Config.IdType, f.WithId, inputName);
        }
    }

    private void AddAssignmentLine(Liner liner, string type, string fieldName, string inputName)
    {
        liner.Add("if (" + inputName + "." + fieldName + " != null) updateEntity." + fieldName + " = " + inputName + "." + fieldName + TypeUtils.GetValueAccessor(type) + ";");
    }

    #endregion

    #region Delete

    private void AddDeleteMutation(ClassMaker cm, GeneratorConfig.ModelConfig model, InputTypeNames inputTypeNames)
    {
        cm.AddClosure("public async Task<" + model.Name + "> " + Config.GraphQl.GqlMutationsDeleteMethod + model.Name +
        "(" + inputTypeNames.Delete + " input, [Service] ITopicEventSender sender)", liner =>
        {
            AddDbVar(liner);
            liner.Add("var deleteEntity = db.Set<" + model.Name + ">().Find(input." + model.Name + "Id);");
            liner.Add("db.Remove(deleteEntity);");
            liner.Add("db.SaveChanges();");

            liner.Add("await sender.SendAsync(\"" + model.Name + Config.GraphQl.GqlSubscriptionDeletedMethod + "\", deleteEntity);");
            liner.Add("return deleteEntity;");
        });
    }

    #endregion

    #endregion

    private void AddDbVar(Liner liner)
    {
        liner.Add("var db = " + Config.Database.DbAccesserClassName + ".Context;");
    }
}
