public class GraphQlMutationsGenerator : BaseGenerator
{
    public GraphQlMutationsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateGraphQlMutations()
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

        cm.AddLine("#region Subscriptions");
        cm.AddBlankLine();
        foreach (var model in Models)
        {
            AddSubscriptionMethods(cm, model);
        }
        cm.AddLine("#endregion");

        fm.Build();
    }

    #region Create

    private void AddCreateMutation(ClassMaker cm, GeneratorConfig.ModelConfig model, InputTypeNames inputTypeNames)
    {
        if (IsRequiredSubModel(model)) return;
        cm.AddClosure("public async Task<" + model.Name + "> " + Config.GraphQl.GqlMutationsCreateMethod + model.Name +
        "(" + inputTypeNames.Create + " input, [Service] ITopicEventSender sender)", liner =>
        {
            liner.Add("var entity = input.ToDto();");

            AddDatabaseAddAndSave(liner);

            AddCallToSubscriptionMethod(liner, model, Config.GraphQl.GqlSubscriptionCreatedMethod);
            liner.Add("return entity;");
        });
    }

    private void AddDatabaseAddAndSave(Liner liner)
    {
        liner.Add("dbService.Add(entity);");
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
            AddCallToSubscriptionMethod(liner, m, Config.GraphQl.GqlSubscriptionUpdatedMethod);
            liner.Add("return entity;");
        });
    }

    private void AddModelUpdater(Liner liner, GeneratorConfig.ModelConfig model, string inputName)
    {
        foreach (var field in model.Fields)
        {
            AddAssignmentLine(liner, field.Name, inputName);
        }
        var foreignProperties = GetForeignProperties(model);
        foreach (var f in foreignProperties)
        {
            if (!f.IsRequiredSingular())
            {
                AddAssignmentLine(liner, f.WithId, inputName);
            }
        }
    }

    private void AddAssignmentLine(Liner liner, string fieldName, string inputName)
    {
        liner.Add("entity." + fieldName + " = " + inputName + "." + fieldName + ";");
    }

    #endregion

    #region Delete

    private void AddDeleteMutation(ClassMaker cm, GeneratorConfig.ModelConfig model, InputTypeNames inputTypeNames)
    {
        if (IsRequiredSubModel(model)) return;
        var typePostfix = GetNullabilityTypePostfix();
        var idTag = "input." + model.Name + "Id";

        cm.AddClosure("public async Task<" + model.Name + typePostfix + "> " + Config.GraphQl.GqlMutationsDeleteMethod + model.Name +
        "(" + inputTypeNames.Delete + " input, [Service] ITopicEventSender sender)", liner =>
        {
            liner.Add("var entity = dbService.Delete<" + model.Name + ">(" + idTag + ");");
            AddFailedToFindStrategyEarlyReturn(liner, model, idTag);
            AddCallToSubscriptionMethod(liner, model, Config.GraphQl.GqlSubscriptionDeletedMethod);
            liner.Add("return entity;");
        });
    }

    #endregion

    private void AddSubscriptionMethods(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        AddSubscriptionMethod(cm, model, Config.GraphQl.GqlSubscriptionCreatedMethod);
        AddSubscriptionMethod(cm, model, Config.GraphQl.GqlSubscriptionUpdatedMethod, false);
        AddSubscriptionMethod(cm, model, Config.GraphQl.GqlSubscriptionDeletedMethod);
    }

    private void AddSubscriptionMethod(ClassMaker cm, GeneratorConfig.ModelConfig model, string method, bool includeRequiredSubModels = true)
    {
        var entityName = "entity";
        cm.AddClosure("private async Task Publish" + GetSubscriptionName(model, method) + "(ITopicEventSender sender, " + model.Name + " " + entityName + ")", liner =>
        {
            liner.Add("await sender.SendAsync(" + GetSubscriptionTopicName(model, method) + ", " + entityName + ");");

            if (includeRequiredSubModels)
            {
                var subModels = GetMyRequiredSubModels(model);
                foreach (var sub in subModels)
                {
                    AddCallToSubscriptionMethod(liner, sub, method, entityName + "." + sub.Name);
                }
            }
        });
    }

    private void AddCallToSubscriptionMethod(Liner liner, GeneratorConfig.ModelConfig model, string subscriptionMethod, string entityName = "entity")
    {
        liner.Add("await Publish" + GetSubscriptionName(model, subscriptionMethod) + "(sender, " + entityName + ");");
    }

    private string GetSubscriptionName(GeneratorConfig.ModelConfig model, string gqlSubscriptionMethod)
    {
        return model.Name + gqlSubscriptionMethod;
    }

    private string GetSubscriptionTopicName(GeneratorConfig.ModelConfig model, string gqlSubscriptionMethod)
    {
        return "nameof(" + Config.GraphQl.GqlSubscriptionsClassName + "." + GetSubscriptionName(model, gqlSubscriptionMethod) + ")";
    }

    private void AddFailedToFindStrategyEarlyReturn(Liner liner, GeneratorConfig.ModelConfig model, string idTag)
    {
        if (IsFailedToFindStrategyNullObject()) liner.Add("if (entity == null) return null;");
        if (IsFailedToFindStrategyErrorCode()) liner.Add("if (entity == null) throw new GraphQLException(\"Unable to find '" + model.Name + "' by Id: '\" + " + idTag + " + \"'\");");
    }
}