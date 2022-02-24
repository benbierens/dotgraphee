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
        cm.AddUsing("System.Linq");
        cm.AddUsing("HotChocolate");
        cm.AddUsing("HotChocolate.Subscriptions");
        cm.AddUsing("HotChocolate.Data");

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
        AddToDeletedEntityMethod(cm);
        cm.AddLine("#endregion");

        fm.Build();
    }

    #region Create

    private void AddCreateMutation(ClassMaker cm, GeneratorConfig.ModelConfig model, InputTypeNames inputTypeNames)
    {
        if (IsRequiredSubModel(model)) return;
        cm.AddLine("[UseSingleOrDefault]");
        cm.AddLine("[UseProjection]");
        cm.AddClosure("public async Task<IQueryable<" + model.Name + ">> " + Config.GraphQl.GqlMutationsCreateMethod + model.Name +
        "(" + inputTypeNames.Create + " input, [Service] ITopicEventSender sender)", liner =>
        {
            liner.Add("var entity = input.ToDto();");

            AddDatabaseAddAndSave(liner);

            AddCallToSubscriptionMethod(liner, model, Config.GraphQl.GqlSubscriptionCreatedMethod);
            liner.Add("return dbService.AsQueryableEntity(entity);");
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

        cm.AddLine("[UseSingleOrDefault]");
        cm.AddLine("[UseProjection]");
        cm.AddClosure("public async Task<IQueryable<" + m.Name + ">" + typePostfix + "> " + Config.GraphQl.GqlMutationsUpdateMethod + m.Name +
        "(" + inputTypeNames.Update + " input, [Service] ITopicEventSender sender)", liner =>
        {
            liner.StartClosure("var entity = dbService.Update<" + m.Name + ">(" + idTag + ", entity =>");
            AddModelUpdater(liner, m, "input");
            liner.EndClosure(");");

            AddFailedToFindStrategyEarlyReturn(liner, m, idTag);
            AddCallToSubscriptionMethod(liner, m, Config.GraphQl.GqlSubscriptionUpdatedMethod);
            liner.Add("return dbService.AsQueryableEntity(entity);");
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

        cm.AddClosure("public async Task<" + Config.IdType + typePostfix + "> " + Config.GraphQl.GqlMutationsDeleteMethod + model.Name +
        "(" + inputTypeNames.Delete + " input, [Service] ITopicEventSender sender)", liner =>
        {
            liner.Add("var entity = dbService.Single<" + model.Name + ">(" + idTag + ");");
            AddFailedToFindStrategyEarlyReturn(liner, model, idTag);
            liner.Add("dbService.Delete<" + model.Name + ">(" + idTag + ");");
            AddCallToSubscriptionMethod(liner, model, Config.GraphQl.GqlSubscriptionDeletedMethod);
            liner.Add("return entity.Id;");
        });
    }

    #endregion

    private void AddToDeletedEntityMethod(ClassMaker cm)
    {
        cm.AddClosure("private static T ToDeletedEntity<T>(T entity) where T : IEntity, new()", liner =>
        {
            liner.StartClosure("return new T()");
            liner.Add("Id = entity.Id");
            liner.EndClosure(";");
        });
    }

    private void AddSubscriptionMethods(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        AddSubscriptionMethod(cm, model, Config.GraphQl.GqlSubscriptionCreatedMethod);
        AddSubscriptionMethod(cm, model, Config.GraphQl.GqlSubscriptionUpdatedMethod, false);
        AddSubscriptionMethod(cm, model, Config.GraphQl.GqlSubscriptionDeletedMethod, true, "ToDeletedEntity(entity)");

        var subModels = GetMyRequiredSubModels(model);
        foreach (var sub in subModels)
        {
            AddGetSubModelMethod(cm, model, sub);
        }
    }

    private void AddGetSubModelMethod(ClassMaker cm, GeneratorConfig.ModelConfig model, GeneratorConfig.ModelConfig sub)
    {
        var m = model.Name;
        var s = sub.Name;
        cm.AddClosure("private " + s + " " +GetGetSubModelMethodName(model, sub) + "(" + m + " entity)", liner =>
        {
            liner.Add("return dbService.All<" + s + ">().Single(e => e." + m + "Id == entity.Id);");
        });
    }

    private string GetGetSubModelMethodName(GeneratorConfig.ModelConfig model, GeneratorConfig.ModelConfig sub)
    {
        return "Get" + sub.Name + "For" + model.Name;
    }

    private void AddSubscriptionMethod(ClassMaker cm, GeneratorConfig.ModelConfig model, string method, bool includeRequiredSubModels = true, string payload = "entity")
    {
        var entityName = "entity";
        cm.AddClosure("private async Task Publish" + GetSubscriptionName(model, method) + "(ITopicEventSender sender, " + model.Name + " " + entityName + ")", liner =>
        {
            liner.Add("await sender.SendAsync(" + GetSubscriptionTopicName(model, method) + ", " + payload + ");");

            if (includeRequiredSubModels)
            {
                var subModels = GetMyRequiredSubModels(model);
                foreach (var sub in subModels)
                {
                    var methodName = GetGetSubModelMethodName(model, sub);
                    AddCallToSubscriptionMethod(liner, sub, method, methodName + "(" + entityName + ")");
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
