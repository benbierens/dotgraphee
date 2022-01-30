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
        var className = Config.GraphQl.GqlQueriesFileName;
        var dbInterface = "I" + Config.Database.DbAccesserClassName;

        var fm = StartSrcFile(Config.Output.GraphQlSubFolder, className);
        var cm = StartClass(fm, className);
        if (IsFailedToFindStrategyErrorCode()) cm.AddUsing("HotChocolate");

        var typePostfix = GetNullabilityTypePostfix();
        
        cm.AddLine("private readonly " + dbInterface + " dbService;");
        cm.AddBlankLine();
        cm.AddClosure("public " + className + "(" + dbInterface + " dbService)", liner =>
        {
            liner.Add("this.dbService = dbService;");
        });

        foreach (var model in Models)
        {
            cm.AddClosure("public " + model.Name + "[] " + model.Name + "s()", liner =>
            {
                liner.Add("return dbService.All<" + model.Name + ">();");
            });

            cm.AddClosure("public " + model.Name + typePostfix + " " + model.Name + "(" + Config.IdType + " id)", liner =>
            {
                if (IsFailedToFindStrategyErrorCode())
                {
                    liner.Add("var entity = dbService.Single<" + model.Name + ">(id);");
                    AddFailedToFindStrategyEarlyReturn(liner, model, "id");
                    liner.Add("return entity;");
                }
                if (IsFailedToFindStrategyNullObject())
                {
                    liner.Add("return dbService.Single<" + model.Name + ">(id);");
                }
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
        var className = Config.GraphQl.GqlMutationsClassName;
        var dbInterface = "I" + Config.Database.DbAccesserClassName;

        var fm = StartSrcFile(Config.Output.GraphQlSubFolder, Config.GraphQl.GqlMutationsFilename);
        var cm = StartClass(fm, className);
        cm.AddUsing("System.Threading.Tasks");
        cm.AddUsing("HotChocolate");
        cm.AddUsing("HotChocolate.Subscriptions");

        cm.AddLine("private readonly IDbService dbService;");
        cm.AddBlankLine();
        cm.AddClosure("public " + className + "(" + dbInterface + " dbService)", liner =>
        {
            liner.Add("this.dbService = dbService;");
        });

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

            liner.Add("await sender.SendAsync(" + GetSubscriptionTopicName(model, Config.GraphQl.GqlSubscriptionCreatedMethod) + ", createEntity);");
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
        liner.Add("dbService.Add(createEntity);");
    }

    #endregion

    #region Update

    private void AddUpdateMutation(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputTypeNames)
    {
        var typePostfix = GetNullabilityTypePostfix();
        var idTag = "input." + m.Name + "Id";

        cm.AddClosure("public async Task<" + m.Name + typePostfix + "> " + Config.GraphQl.GqlMutationsUpdateMethod + m.Name +
        "(" + inputTypeNames.Update + " input, [Service] ITopicEventSender sender)", liner =>
        {
            liner.StartClosure("var entity = dbService.Update<" + m.Name + ">(" + idTag + ", entity =>");
            AddModelUpdater(liner, m, "input");
            liner.EndClosure(");");

            AddFailedToFindStrategyEarlyReturn(liner, m, idTag);
            liner.Add("await sender.SendAsync(" + GetSubscriptionTopicName(m, Config.GraphQl.GqlSubscriptionUpdatedMethod) + ", entity);");
            liner.Add("return entity;");
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
        liner.Add("if (" + inputName + "." + fieldName + " != null) entity." + fieldName + " = " + inputName + "." + fieldName + TypeUtils.GetValueAccessor(type) + ";");
    }

    #endregion

    #region Delete

    private void AddDeleteMutation(ClassMaker cm, GeneratorConfig.ModelConfig model, InputTypeNames inputTypeNames)
    {
        var typePostfix = GetNullabilityTypePostfix();
        var idTag = "input." + model.Name + "Id";

        cm.AddClosure("public async Task<" + model.Name + typePostfix + "> " + Config.GraphQl.GqlMutationsDeleteMethod + model.Name +
        "(" + inputTypeNames.Delete + " input, [Service] ITopicEventSender sender)", liner =>
        {
            liner.Add("var entity = dbService.Delete<" + model.Name + ">(" + idTag + ");");
            AddFailedToFindStrategyEarlyReturn(liner, model, idTag);
            liner.Add("await sender.SendAsync(" + GetSubscriptionTopicName(model, Config.GraphQl.GqlSubscriptionDeletedMethod) + ", entity);");
            liner.Add("return entity;");
        });
    }

    #endregion

    #endregion

    private string GetSubscriptionTopicName(GeneratorConfig.ModelConfig model, string gqlSubscriptionMethod)
    {
        return "nameof(" + Config.GraphQl.GqlSubscriptionsClassName + "." + model.Name + gqlSubscriptionMethod + ")";
    }

    private void AddFailedToFindStrategyEarlyReturn(Liner liner, GeneratorConfig.ModelConfig model, string idTag)
    {
        if (IsFailedToFindStrategyNullObject()) liner.Add("if (entity == null) return null;");
        if (IsFailedToFindStrategyErrorCode()) liner.Add("if (entity == null) throw new GraphQLException(\"Unable to find '" + model.Name + "' by Id: '\" + " + idTag + " + \"'\");");
    }
}
