public class GraphQlQueriesGenerator : BaseGenerator
{
    public GraphQlQueriesGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateGraphQlQueries()
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

    private void AddFailedToFindStrategyEarlyReturn(Liner liner, GeneratorConfig.ModelConfig model, string idTag)
    {
        if (IsFailedToFindStrategyNullObject()) liner.Add("if (entity == null) return null;");
        if (IsFailedToFindStrategyErrorCode()) liner.Add("if (entity == null) throw new GraphQLException(\"Unable to find '" + model.Name + "' by Id: '\" + " + idTag + " + \"'\");");
    }
}
