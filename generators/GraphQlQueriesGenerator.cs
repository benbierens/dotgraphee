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
        
        cm.AddLine("private readonly " + dbInterface + " dbService;");
        cm.AddBlankLine();
        cm.AddClosure("public " + className + "(" + dbInterface + " dbService)", liner =>
        {
            liner.Add("this.dbService = dbService;");
        });

        foreach (var model in Models)
        {
            if (HasAnyNavigationalPropertiesOrFeatures(model))
            {
                AddProjectionQueryUsings(cm);
                AddProjectionQueryMethod(cm, model);    
            }
            else
            {
                AddSimpleQueryMethods(cm, model);
            }
        }

        fm.Build();
    }

    private void AddSimpleQueryMethods(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        AddAllQueryMethod(cm, model);
        AddQuerySingleMethod(cm, model);
    }

    private void AddAllQueryMethod(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        cm.AddClosure("public " + model.Name + "[] " + model.Name + "s()", liner =>
        {
            liner.Add("return dbService.All<" + model.Name + ">();");
        });
    }

    private void AddQuerySingleMethod(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        var typePostfix = GetNullabilityTypePostfix();
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

    private void AddProjectionQueryUsings(ClassMaker cm)
    {
        cm.AddUsing("HotChocolate.Data");
        cm.AddUsing("HotChocolate.Types");
        cm.AddUsing("System.Linq");
    }

    private void AddProjectionQueryMethod(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        if (model.HasPagingFeature()) cm.AddLine("[UsePaging]");
        if (HasAnyNavigationalProperties(model)) cm.AddLine("[UseProjection]");
        if (model.HasFilteringFeature()) cm.AddLine("[UseFiltering]");
        if (model.HasSortingFeature()) cm.AddLine("[UseSorting]");

        cm.AddClosure("public IQueryable<" + model.Name + "> " + model.Name + "s()", liner =>
        {
            liner.Add("return dbService.AsQueryable<" + model.Name + ">();");
        });
    }

    private void AddFailedToFindStrategyEarlyReturn(Liner liner, GeneratorConfig.ModelConfig model, string idTag)
    {
        if (IsFailedToFindStrategyNullObject()) liner.Add("if (entity == null) return null;");
        if (IsFailedToFindStrategyErrorCode()) liner.Add("if (entity == null) throw new GraphQLException(\"Unable to find '" + model.Name + "' by Id: '\" + " + idTag + " + \"'\");");
    }
}
