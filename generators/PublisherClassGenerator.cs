public class PublisherClassGenerator : BaseGenerator
{
    public PublisherClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GeneratePublisher()
    {
        var fm = StartSrcFile(Config.Output.GraphQlSubFolder, "Publisher");
        fm.AddUsing("HotChocolate.Subscriptions");
        fm.AddUsing("System.Linq");
        fm.AddUsing("System.Threading.Tasks");

        AddPublisherInterface(fm);
        AddPublisherClass(fm);

        fm.Build();
    }

    private void AddPublisherInterface(FileMaker fm)
    {
        var im = fm.AddInterface("IPublisher");
        foreach (var model in Models)
        {
            AddInterfaceMethods(im, model);
        }
    }

    private void AddInterfaceMethods(ClassMaker im, GeneratorConfig.ModelConfig m)
    {
        var arguments = "(ITopicEventSender sender, " + m.Name + " entity);";
        im.AddLine("Task Publish" + m.Name + Config.GraphQl.GqlSubscriptionCreatedMethod + arguments);
        im.AddLine("Task Publish" + m.Name + Config.GraphQl.GqlSubscriptionUpdatedMethod + arguments);
        im.AddLine("Task Publish" + m.Name + Config.GraphQl.GqlSubscriptionDeletedMethod + arguments);
    }

    private void AddPublisherClass(FileMaker fm)
    {
        var cm = fm.AddClass("Publisher");
        cm.Modifiers.Clear();
        cm.AddInherrit("IPublisher");

        cm.AddLine("private readonly IDbService dbService;");
        cm.AddBlankLine();
        cm.AddClosure("public Publisher(I" + Config.Database.DbAccesserClassName + " dbService)", liner =>
        {
            liner.Add("this.dbService = dbService;");
        });

        foreach (var model in Models)
        {
            AddSubscriptionMethods(cm, model);
        }
        AddToDeletedEntityMethod(cm);
    }

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
        cm.AddClosure("private " + s + " " + GetGetSubModelMethodName(model, sub) + "(" + m + " entity)", liner =>
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
            if (includeRequiredSubModels)
            {
                var subModels = GetMyRequiredSubModels(model);
                foreach (var sub in subModels)
                {
                    var methodName = GetGetSubModelMethodName(model, sub);
                    AddCallToSubscriptionMethod(liner, sub, method, methodName + "(" + entityName + ")");
                }
            }

            liner.Add("await sender.SendAsync(" + GetSubscriptionTopicName(model, method) + ", " + payload + ");");
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
}
