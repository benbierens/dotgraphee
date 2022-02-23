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

    public class QueryMethodsSubgenerator : BaseGqlGenerator
    {
        public QueryMethodsSubgenerator(GeneratorConfig config)
            : base(config)
        {
        }

        public void AddQueryAllMethod(ClassMaker cm, GeneratorConfig.ModelConfig m)
        {
            cm.AddClosure("public async Task<GqlData<All" + m.Name + "sQuery>> QueryAll" + m.Name + "s()", liner =>
            {
                AddQueryAll(liner, m);
                liner.Add("return await Client.PostRequest<All" + m.Name + "sQuery>(request);");
            });
        }

        public void AddQueryOneMethod(ClassMaker cm, GeneratorConfig.ModelConfig m)
        {
            cm.AddClosure("public async Task<GqlData<One" + m.Name + "Query>> QueryOne" + m.Name + "(" + Config.IdType + " id)", liner =>
            {
                AddQueryOne(liner, m);

                if (m.HasFilteringFeature())
                {
                    liner.Add("var all = await Client.PostRequest<All" + m.Name + "sQuery>(request);");
                    liner.StartClosure("return new GqlData<One" + m.Name + "Query>");
                    liner.Add("Errors = all.Errors,");
                    liner.StartClosure("Data = new One" + m.Name + "Query");
                    liner.Add(m.Name + " = all" + GetDereferenceForGqlData(m) + "[0]");
                    liner.EndClosure();
                    liner.EndClosure(";");
                }
                else
                {
                    liner.Add("return await Client.PostRequest<One" + m.Name + "Query>(request);");
                }
            });
        }
    }

    public class MutationMethodsSubgenerator : BaseGqlGenerator
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
                AddMutation(liner, m, templateField);
                liner.Add("return await Client.PostRequest<" + templateType + ">(request);");
            });
        }

        private void AddUpdateMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
        {
            var templateField = Config.GraphQl.GqlMutationsUpdateMethod + m.Name;
            var templateType = templateField + "Response";

            cm.AddClosure("public async Task<GqlData<" + templateType + ">> Update" + m.Name + "(" + inputNames.Update + " input)", liner =>
            {
                AddMutation(liner, m, templateField);
                liner.Add("return await Client.PostRequest<" + templateType + ">(request);");
            });
        }

        private void AddDeleteMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
        {
            if (IsRequiredSubModel(m)) return;
            
            var templateField = Config.GraphQl.GqlMutationsDeleteMethod + m.Name;
            var templateType = templateField + "Response";

            cm.AddClosure("public async Task<GqlData<" + templateType+ ">> Delete" + m.Name + "(" + inputNames.Delete + " input)", liner =>
            {
                AddMutation(liner, m, templateField);
                liner.Add("return await Client.PostRequest<" + templateType + ">(request);");
            });
        }
    }

    public class SubscriptionMethodsSubgenerator : BaseGqlGenerator
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

    public abstract class BaseGqlGenerator : BaseTestGenerator
    {
        protected BaseGqlGenerator(GeneratorConfig config)
            : base(config)
        {
        }

        public void AddQueryAll(Liner liner, GeneratorConfig.ModelConfig m)
        {
            Add(liner, m, "Query", m.Name.FirstToLower() + "s", GetPagingAndBuildTag(m));
        }

        public void AddQueryOne(Liner liner, GeneratorConfig.ModelConfig m)
        {
            if (m.HasFilteringFeature())
            {
                Add(liner, m, "Query", m.Name.FirstToLower() + "s", GetPagingAndBuildTag(m), ".WithFilterId(id)");

            }
            else
            {
                Add(liner, m, "Query", m.Name.FirstToLower(), GetPagingAndBuildTag(m), ".WithId(id)");
            }
        }

        public void AddMutation(Liner liner, GeneratorConfig.ModelConfig m, string templateField)
        {
            Add(liner, m, "Mutation", templateField.FirstToLower(), GetBuildTag(), ".WithInput(input)");
        }

        private void Add(Liner liner, GeneratorConfig.ModelConfig m, string verb, string target, string closer, string input = "")
        {
            if (!HasRequiredSubModels(m))
            {
                liner.Add("var request = GqlBuild." + verb + "(\"" + target + "\")" + input + ".WithOutput<" + m.Name + ">()" + closer);
            }
            else
            {
                liner.Add("var request = GqlBuild." + verb + "(\"" + target + "\")" + input + ".WithOutput<" + m.Name + ">(i => i");
                var subs = GetMyRequiredSubModels(m);
                foreach (var sub in subs) AddInclusion(liner, m, sub);
                liner.Add(")" + closer);
            }
            liner.AddBlankLine();
        }

        private string GetPagingAndBuildTag(GeneratorConfig.ModelConfig m)
        {
            if (m.HasPagingFeature()) return ".WithPaging()" + GetBuildTag();
            return GetBuildTag();
        }

        private string GetBuildTag()
        {
            return ".Build();";
        }

        private void AddInclusion(Liner liner, GeneratorConfig.ModelConfig model, GeneratorConfig.ModelConfig subModel)
        {
            var l = model.Name.FirstToLower();
            liner.Indent();
            if (!HasRequiredSubModels(subModel))
            {
                liner.Add(".Include(" + l + " => " + l + "." + subModel.Name + ")");
            }
            else
            {
                liner.Add(".Include(" + l + " => " + l + "." + subModel.Name + ", i => i");
                var subSubs = GetMyRequiredSubModels(subModel);
                foreach (var sub in subSubs) AddInclusion(liner, subModel, sub);
                liner.Add(")");
            }
            liner.Deindent();
        }
    }
}
