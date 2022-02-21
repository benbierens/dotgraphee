public class GraphQlSubscriptionsGenerator : BaseGenerator
{
    public GraphQlSubscriptionsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateGraphQlSubscriptions()
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
}
