public class GraphQlSubscriptionsGenerator : BaseGenerator
{
    public GraphQlSubscriptionsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateGraphQlSubscriptions()
    {
        var className = Config.GraphQl.GqlSubscriptionsClassName;
        var dbInterface = "I" + Config.Database.DbAccesserClassName;

        var fm = StartSrcFile(Config.Output.GraphQlSubFolder, Config.GraphQl.GqlSubscriptionsFilename);
        var cm = StartClass(fm, className);
        cm.AddUsing("HotChocolate");
        cm.AddUsing("HotChocolate.Data");
        cm.AddUsing("HotChocolate.Types");
        cm.AddUsing("System.Linq");

        cm.AddLine("private readonly " + dbInterface + " dbService;");
        cm.AddBlankLine();
        cm.AddClosure("public " + className + "(" + dbInterface + " dbService)", liner =>
        {
            liner.Add("this.dbService = dbService;");
        });

        foreach (var model in Models)
        {
            AddSubscriptionMethod(cm, model.Name, Config.GraphQl.GqlSubscriptionCreatedMethod);
            AddSubscriptionMethod(cm, model.Name, Config.GraphQl.GqlSubscriptionUpdatedMethod);
            AddSubscriptionMethod(cm, model.Name, Config.GraphQl.GqlSubscriptionDeletedMethod, false);
        }

        fm.Build();
    }

    private void AddSubscriptionMethod(ClassMaker cm, string modelName, string method, bool useProjection = true)
    {
        var n = modelName;
        var l = n.FirstToLower();
        cm.AddLine("[Subscribe]");

        if (useProjection)
        {
            cm.AddLine("[UseSingleOrDefault]");
            cm.AddLine("[UseProjection]");
            cm.AddClosure("public IQueryable<" + n + "> " + n + method + "([EventMessage] " + n + " " + l + ")", liner =>
            {
                liner.Add("return dbService.AsQueryableEntity(" + l + ");");
            });
        }
        else
        {
            cm.AddClosure("public " + n + " " + n + method + "([EventMessage] " + n + " " + l + ")", liner =>
            {
                liner.Add("return " + l +";");
            });
        }
    }
}
