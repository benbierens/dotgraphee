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
        cm.AddUsing("HotChocolate.Subscriptions");
        cm.AddUsing("HotChocolate.Data");
        cm.AddUsing("HotChocolate");

        cm.AddLine("private readonly IDbService dbService;");
        cm.AddLine("private readonly IPublisher publisher;");
        cm.AddLine("private readonly IInputConverter inputConverter;");
        cm.AddBlankLine();
        cm.AddClosure("public " + className + "(" + dbInterface + " dbService, IPublisher publisher, IInputConverter inputConverter)", liner =>
        {
            liner.Add("this.dbService = dbService;");
            liner.Add("this.publisher = publisher;");
            liner.Add("this.inputConverter = inputConverter;");
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
        if (IsRequiredSubModel(model)) return;
        cm.AddLine("[UseSingleOrDefault]");
        cm.AddLine("[UseProjection]");
        cm.AddClosure("public async Task<IQueryable<" + model.Name + ">> " + Config.GraphQl.GqlMutationsCreateMethod + model.Name +
        "(" + inputTypeNames.Create + " input, [Service] ITopicEventSender sender)", liner =>
        {
            liner.Add("var entity = inputConverter.ToDto(input);");
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
            AddCallToSubscriptionMethod(liner, model, Config.GraphQl.GqlSubscriptionDeletedMethod);
            liner.Add("dbService.Delete<" + model.Name + ">(" + idTag + ");");
            liner.Add("return entity.Id;");
        });
    }

    #endregion

    private void AddCallToSubscriptionMethod(Liner liner, GeneratorConfig.ModelConfig model, string subscriptionMethod, string entityName = "entity")
    {
        liner.Add("await publisher.Publish" + GetSubscriptionName(model, subscriptionMethod) + "(sender, " + entityName + ");");
    }

    private void AddFailedToFindStrategyEarlyReturn(Liner liner, GeneratorConfig.ModelConfig model, string idTag)
    {
        if (IsFailedToFindStrategyNullObject()) liner.Add("if (entity == null) return null;");
        if (IsFailedToFindStrategyErrorCode()) liner.Add("if (entity == null) throw new GraphQLException(\"Unable to find '" + model.Name + "' by Id: '\" + " + idTag + " + \"'\");");
    }

    private string GetSubscriptionName(GeneratorConfig.ModelConfig model, string gqlSubscriptionMethod)
    {
        return model.Name + gqlSubscriptionMethod;
    }
}
