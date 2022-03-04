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
        
        cm.AddLine("private readonly " + dbInterface + " dbService;");
        cm.AddBlankLine();
        cm.AddClosure("public " + className + "(" + dbInterface + " dbService)", liner =>
        {
            liner.Add("this.dbService = dbService;");
        });

        foreach (var model in Models)
        {
            AddProjectionQueryUsings(cm);
            AddProjectionQueryMethods(cm, model);    
        }

        fm.Build();
    }

    private void AddProjectionQueryUsings(ClassMaker cm)
    {
        cm.AddUsing("HotChocolate.Data");
        cm.AddUsing("HotChocolate.Types");
        cm.AddUsing("System.Linq");
    }

    private void AddProjectionQueryMethods(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        AddProjectionAllQueryMethod(cm, model);
        AddProjectionOneQueryMethod(cm, model);
    }

    private void AddProjectionAllQueryMethod(ClassMaker cm, GeneratorConfig.ModelConfig model)
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

    private void AddProjectionOneQueryMethod(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        cm.AddLine("[UseSingleOrDefault]");
        if (HasAnyNavigationalProperties(model)) cm.AddLine("[UseProjection]");
        cm.AddClosure("public IQueryable<" + model.Name + "> " + model.Name + "(" + Config.IdType + " id)", liner =>
        {
            liner.Add("return dbService.AsQueryable<" + model.Name + ">().Where(e => e.Id == id);");
        });
    }

    private void AddFailedToFindStrategyEarlyReturn(Liner liner, GeneratorConfig.ModelConfig model, string idTag)
    {
        if (IsFailedToFindStrategyNullObject()) liner.Add("if (entity == null) return null;");
        if (IsFailedToFindStrategyErrorCode()) liner.Add("if (entity == null) throw new GraphQLException(\"Unable to find '" + model.Name + "' by Id: '\" + " + idTag + " + \"'\");");
    }
}
