public class GqlClassGenerator : BaseGenerator
{
    private readonly QueryMethodsSubgenerator queryAllMethodSubgenerator;
    private readonly MutationMethodsSubgenerator mutationMethodsSubgenerator;
    private readonly SubscriptionMethodsSubgenerator subscriptionMethodsSubgenerator;

    public GqlClassGenerator(GeneratorConfig config)
        : base(config)
    {
        queryAllMethodSubgenerator = new QueryMethodsSubgenerator(config);
        mutationMethodsSubgenerator = new MutationMethodsSubgenerator(config);
        subscriptionMethodsSubgenerator = new SubscriptionMethodsSubgenerator(config);
    }

    public void CreateGqlClass()
    {
        var fm = StartIntegrationTestUtilsFile("Gql");
        var cm = fm.AddClass("Gql");

        cm.AddUsing("System");
        cm.AddUsing("System.Collections.Generic");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddUsing(Config.GenerateNamespace + ".Client");
        cm.AddUsing("Microsoft.Extensions.DependencyInjection");
        cm.AddUsing("StrawberryShake");

        cm.AddLine("private readonly IDotGraphEE_DemoClient client = null!;");
        cm.AddLine("private readonly List<ISubscriptionHandle> handles = new List<ISubscriptionHandle>();");
        cm.AddBlankLine();

        var address = "localhost:5000/graphql";

        cm.AddClosure("public Gql()", liner =>
        {
            liner.Add("var serviceCollection = new ServiceCollection();");
            liner.AddBlankLine();
            liner.Add("serviceCollection");
            liner.Indent();
            liner.Add(".Add" + ClientName + "()");
            liner.Add(".ConfigureHttpClient(client => client.BaseAddress = new Uri(\"http://" + address + "\"))");
            liner.Add(".ConfigureWebSocketClient(client => client.Uri = new Uri(\"ws://" + address + "\"));");
            liner.Deindent();
            liner.AddBlankLine();
            liner.Add("IServiceProvider services = serviceCollection.BuildServiceProvider();");
            liner.Add("client = services.GetRequiredService<I" + ClientName + ">();");
        });

        cm.AddClosure("public void CloseActiveSubscriptionHandles()", liner =>
        {
            liner.StartClosure("foreach (var h in handles)");
            liner.Add("h.Unsubscribe();");
            liner.EndClosure();
            liner.Add("handles.Clear();");
        });

        foreach (var m in Models)
        {
            cm.BeginRegion(m.Name);
            queryAllMethodSubgenerator.AddQueryAllMethod(cm, m);
            queryAllMethodSubgenerator.AddQueryOneMethod(cm, m);
            mutationMethodsSubgenerator.AddMutationMethods(cm, m);
            subscriptionMethodsSubgenerator.AddSubscribeMethods(cm, m);
            cm.EndRegion();
        }

        cm.AddClosure("private SubscriptionHandle<TResult> SubscribeTo<TResult>(Func<IDotGraphEE_DemoClient, IObservable<IOperationResult<TResult>>> selector) where TResult : class", liner =>
        {
            liner.Add("var s = new SubscriptionHandle<TResult>(client);");
            liner.Add("s.Subscribe(selector);");
            liner.Add("handles.Add(s);");
            liner.Add("return s;");
        });

        fm.Build();
    }

    public class QueryMethodsSubgenerator : BaseTestGenerator
    {
        public QueryMethodsSubgenerator(GeneratorConfig config)
            : base(config)
        {
        }

        public void AddQueryAllMethod(ClassMaker cm, GeneratorConfig.ModelConfig m)
        {
            cm.AddClosure("public async Task<IOperationResult<IAll" + m.Name + "sResult>> QueryAll" + m.Name + "s()", liner =>
            {
                liner.Add("return await client.All" + m.Name + "s.ExecuteAsync();");
            });
        }

        public void AddQueryOneMethod(ClassMaker cm, GeneratorConfig.ModelConfig m)
        {
            cm.AddClosure("public async Task<IOperationResult<IOne" + m.Name + "Result>> QueryOne" + m.Name + "(" + Config.IdType + " id)", liner =>
            {
                liner.Add("return await client.One" + m.Name + ".ExecuteAsync(id);");
            });
        }
    }

    public class MutationMethodsSubgenerator : BaseTestGenerator
    {
        public MutationMethodsSubgenerator(GeneratorConfig config)
            : base(config)
        {
        }

        public void AddMutationMethods(ClassMaker cm, GeneratorConfig.ModelConfig m)
        {
            var inputNames = GetInputTypeNames(m);
            AddCreateMutationMethod(cm, m, inputNames);
            AddUpdateMutationMethod(cm, m, inputNames);
            AddDeleteMutationMethod(cm, m);
        }

        private void AddCreateMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
        {
            if (IsRequiredSubModel(m)) return;

            var templateField = Config.GraphQl.GqlMutationsCreateMethod + m.Name;
            var templateType = templateField + "Result";

            cm.AddClosure("public async Task<IOperationResult<I" + templateType + ">> Create" + m.Name + "(" + inputNames.Create + " input)", liner =>
            {
                liner.Add("return await client." + templateField + ".ExecuteAsync(input);");
            });
        }

        private void AddUpdateMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
        {
            var templateField = Config.GraphQl.GqlMutationsUpdateMethod + m.Name;
            var templateType = templateField + "Result";

            cm.AddClosure("public async Task<IOperationResult<I" + templateType + ">> Update" + m.Name + "(" + inputNames.Update + " input)", liner =>
            {
                liner.Add("return await client." + templateField + ".ExecuteAsync(input);");
            });
        }

        private void AddDeleteMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m)
        {
            if (IsRequiredSubModel(m)) return;

            var templateField = Config.GraphQl.GqlMutationsDeleteMethod + m.Name;
            var templateType = templateField + "Result";

            cm.AddClosure("public async Task<IOperationResult<I" + templateType + ">> Delete" + m.Name + "(" + Config.IdType + " input)", liner =>
            {
                liner.Add("return await client." + templateField + ".ExecuteAsync(input);");
            });
        }
    }

    public class SubscriptionMethodsSubgenerator : BaseTestGenerator
    {
        public SubscriptionMethodsSubgenerator(GeneratorConfig config)
            : base(config)
        {
        }

        public void AddSubscribeMethods(ClassMaker cm, GeneratorConfig.ModelConfig m)
        {
            AddSubscribeMethod(cm, m, Config.GraphQl.GqlSubscriptionCreatedMethod);
            AddSubscribeMethod(cm, m, Config.GraphQl.GqlSubscriptionUpdatedMethod);
            AddSubscribeMethod(cm, m, Config.GraphQl.GqlSubscriptionDeletedMethod);
        }

        private void AddSubscribeMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, string methodName)
        {
            var nameMethod = m.Name + methodName;
            cm.AddClosure($"public SubscriptionHandle<I{nameMethod}Result> SubscribeTo{nameMethod}()", liner =>
            {
                liner.Add($"return SubscribeTo(c => c.{nameMethod}.Watch());");
            });
        }
    }
}
