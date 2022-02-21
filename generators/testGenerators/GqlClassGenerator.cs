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
        var fm = StartTestUtilsFile("Gql");
        var cm = fm.AddClass("Gql");
        
        cm.AddUsing("System.Collections.Generic");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddUsing(Config.GenerateNamespace);

        cm.AddLine("private readonly List<ISubscriptionHandle> handles = new List<ISubscriptionHandle>();");
        cm.AddBlankLine();

        cm.AddClosure("public async Task CloseActiveSubscriptionHandles()", liner =>
        {
            liner.StartClosure("foreach (var h in handles)");
            liner.Add("await h.Unsubscribe();");
            liner.EndClosure();
            liner.Add("handles.Clear();");
        });

        foreach (var m in Models)
        {
            cm.AddLine("#region " + m.Name);
            cm.AddBlankLine();
            queryAllMethodSubgenerator.AddQueryAllMethod(cm, m);
            queryAllMethodSubgenerator.AddQueryOneMethod(cm, m);
            mutationMethodsSubgenerator.AddMutationMethods(cm, m);
            subscriptionMethodsSubgenerator.AddSubscribeMethods(cm, m);
            cm.AddLine("#endregion");
            cm.AddBlankLine();
        }

        cm.AddClosure("private async Task<SubscriptionHandle<T>> SubscribeTo<T>(string modelName)", liner =>
        {
            liner.Add("var s = new SubscriptionHandle<T>(modelName);");
            liner.Add("await s.Subscribe();");
            liner.Add("handles.Add(s);");
            liner.Add("return s;");
        });

        fm.Build();
    }

    public class QueryMethodsSubgenerator : BaseGenerator
    {
        public QueryMethodsSubgenerator(GeneratorConfig config)
            : base(config)
        {
        }

        public void AddQueryAllMethod(ClassMaker cm, GeneratorConfig.ModelConfig m)
        {
            cm.AddClosure("public async Task<GqlData<All" + m.Name + "sQuery>> QueryAll" + m.Name + "s()", liner =>
            {
                liner.Add("var query = GqlBuild.Query(\"" + m.Name.FirstToLower() + "s\").WithOutput<" + m.Name + ">().Build();");
                liner.Add("return await Client.PostRequest<All" + m.Name + "sQuery>(query);");
            });
        }

        public void AddQueryOneMethod(ClassMaker cm, GeneratorConfig.ModelConfig m)
        {
            cm.AddClosure("public async Task<GqlData<One" + m.Name + "Query>> QueryOne" + m.Name + "(" + Config.IdType + " id)", liner =>
            {
                liner.Add("var query = GqlBuild.Query(\"" + m.Name.FirstToLower() + "\").WithId(id).WithOutput<" + m.Name + ">().Build();");
                liner.Add("return await Client.PostRequest<One" + m.Name + "Query>(query);");
            });
        }
    }

    public class MutationMethodsSubgenerator : BaseGenerator
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
            AddDeleteMutationMethod(cm, m, inputNames);
        }

        private void AddCreateMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
        {
            if (IsRequiredSubModel(m)) return;

            var templateField = Config.GraphQl.GqlMutationsCreateMethod + m.Name;
            var templateType = templateField + "Response";

            cm.AddClosure("public async Task<GqlData<" + templateType + ">> Create" + m.Name + "(" + inputNames.Create + " input)", liner =>
            {
                liner.Add("var mutation = GqlBuild.Mutation(\"" + templateField.FirstToLower() + "\").WithInput(input).WithOutput<" + m.Name + ">().Build();");
                liner.Add("return await Client.PostRequest<" + templateType + ">(mutation);");
            });
        }

        private void AddUpdateMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
        {
            var templateField = Config.GraphQl.GqlMutationsUpdateMethod + m.Name;
            var templateType = templateField + "Response";

            cm.AddClosure("public async Task<GqlData<" + templateType + ">> Update" + m.Name + "(" + inputNames.Update + " input)", liner =>
            {
                liner.Add("var mutation = GqlBuild.Mutation(\"" + templateField.FirstToLower() + "\").WithInput(input).WithOutput<" + m.Name + ">().Build();");
                liner.Add("return await Client.PostRequest<" + templateType + ">(mutation);");
            });
        }

        private void AddDeleteMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
        {
            if (IsRequiredSubModel(m)) return;
            
            var templateField = Config.GraphQl.GqlMutationsDeleteMethod + m.Name;
            var templateType = templateField + "Response";

            cm.AddClosure("public async Task<GqlData<" + templateType+ ">> Delete" + m.Name + "(" + inputNames.Delete + " input)", liner =>
            {
                liner.Add("var mutation = GqlBuild.Mutation(\"" + templateField.FirstToLower() + "\").WithInput(input).WithOutput<" + m.Name + ">().Build();");
                liner.Add("return await Client.PostRequest<" + templateType + ">(mutation);");
            });
        }
    }

    public class SubscriptionMethodsSubgenerator : BaseGenerator
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
            cm.AddClosure("public async Task<SubscriptionHandle<" + m.Name + ">> SubscribeTo" + m.Name + methodName + "()", liner =>
            {
                liner.Add("return await SubscribeTo<" + m.Name + ">(\"" + m.Name.FirstToLower() + methodName + "\");");
            });
        }
    }
}
