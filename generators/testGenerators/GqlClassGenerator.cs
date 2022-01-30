using System.Collections.Generic;
using System.Linq;

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

        cm.AddClosure("private async Task<SubscriptionHandle<T>> SubscribeTo<T>(string modelName, params string[] fields)", liner =>
        {
            liner.Add("var s = new SubscriptionHandle<T>(modelName, fields);");
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
                liner.Add("var query = \"{ \\\"query\\\": \\\"query { " + m.Name.FirstToLower() + "s { " + GetQueryFields(m) + " } } \\\" }\";");
                liner.Add("return await Client.PostRequest<All" + m.Name + "sQuery>(query);");
            });
        }

        public void AddQueryOneMethod(ClassMaker cm, GeneratorConfig.ModelConfig m)
        {
            cm.AddClosure("public async Task<GqlData<One" + m.Name + "Query>> QueryOne" + m.Name + "(" + Config.IdType + " id)", liner =>
            {
                liner.Add("var query = \"{ \\\"query\\\": \\\"query { " + m.Name.FirstToLower() + GetIdExpression() + " { " + GetQueryFields(m) + " } } \\\" }\";");
                liner.Add("return await Client.PostRequest<One" + m.Name + "Query>(query);");
            });
        }

        private string GetIdExpression()
        {
            if (TypeUtils.RequiresQuotes(Config.IdType))
            {
                return "(id: \\\\\\\"\" + id + \"\\\\\\\")";
            }
            return "(id: \" + id + \")";
        }

        private string GetQueryFields(GeneratorConfig.ModelConfig m)
        {
            var foreignProperties = GetForeignProperties(m);
            var foreignIds = string.Join(" ", foreignProperties.Select(f => f.WithId.FirstToLower()));
            return "id " + string.Join(" ", m.Fields.Select(f => f.Name.FirstToLower())) + " " + foreignIds;
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
            var templateField = Config.GraphQl.GqlMutationsCreateMethod + m.Name;
            var templateType = templateField + "Response";

            cm.AddClosure("public async Task<GqlData<" + templateType + ">> Create" + m.Name + "(" + inputNames.Create + " input)", liner =>
            {
                liner.Add("var fields = \"\";");
                var fields = new List<string>();
                fields.Add("id");

                foreach (var f in m.Fields)
                {
                    fields.Add(f.Name.FirstToLower());
                    liner.Add("fields +=" + GetValueExpressionAsNonNullable(cm, f));
                }
                var foreignProperties = GetForeignProperties(m);
                foreach (var f in foreignProperties)
                {
                    fields.Add(f.WithId.FirstToLower());
                    if (f.IsSelfReference)
                    {
                        liner.Add("if (input." + f.WithId + " != null) fields += " + GetIdExpression(f) + ";");
                    }
                    else
                    {
                        liner.Add("fields += " + GetIdExpression(f) + ";");
                    }
                }
                liner.AddBlankLine();
                var queryFields = string.Join(" ", fields);
                liner.Add("var mutation = \"{ \\\"query\\\": \\\"mutation { " + Config.GraphQl.GqlMutationsCreateMethod.FirstToLower() + m.Name + "(input: { \" + fields + \" }) { " + queryFields + " } }\\\"}\";");
                liner.Add("return await Client.PostRequest<" + templateType + ">(mutation);");
            });
        }

        private void AddUpdateMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
        {
            var templateField = Config.GraphQl.GqlMutationsUpdateMethod + m.Name;
            var templateType = templateField + "Response";

            cm.AddClosure("public async Task<GqlData<" + templateType + ">> Update" + m.Name + "(" + inputNames.Update + " input)", liner =>
            {
                liner.Add(GetInitializeFieldsWithId(m));
                var fields = new List<string>();
                fields.Add("id");

                foreach (var f in m.Fields)
                {
                    fields.Add(f.Name.FirstToLower());
                    liner.Add("if (input." + f.Name + " != null) fields +=" + GetValueExpressionWithNullabilityAccessor(cm, f));
                }
                var foreignProperties = GetForeignProperties(m);
                foreach (var f in foreignProperties)
                {
                    fields.Add(f.WithId.FirstToLower());
                    liner.Add("if (input." + f.WithId + " != null) fields += " + GetIdExpression(f) + ";");
                }
                liner.AddBlankLine();
                var queryFields = string.Join(" ", fields);
                liner.Add("var mutation = \"{ \\\"query\\\": \\\"mutation { " + Config.GraphQl.GqlMutationsUpdateMethod.FirstToLower() + m.Name + "(input: { \" + fields + \" }) { " + queryFields + " } }\\\"}\";");
                liner.Add("return await Client.PostRequest<" + templateType + ">(mutation);");
            });
        }

        private void AddDeleteMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
        {
            var templateField = Config.GraphQl.GqlMutationsDeleteMethod + m.Name;
            var templateType = templateField + "Response";

            cm.AddClosure("public async Task<GqlData<" + templateType+ ">> Delete" + m.Name + "(" + inputNames.Delete + " input)", liner =>
            {
                var fields = GetDeleteFields(m);
                liner.Add("var mutation = \"{ \\\"query\\\": \\\"mutation { " + Config.GraphQl.GqlMutationsDeleteMethod.FirstToLower() + m.Name + "(input: {" + fields + "}) { id } }\\\"}\";");
                liner.Add("return await Client.PostRequest<" + templateType + ">(mutation);");
            });
        }

        private static string GetValueExpressionWithNullabilityAccessor(ClassMaker cm, GeneratorConfig.ModelField f)
        {
            var valueAccessor = TypeUtils.GetValueAccessor(f.Type);
            return GetValueExpression(cm, f, valueAccessor);
        }

        private static string GetValueExpressionAsNonNullable(ClassMaker cm, GeneratorConfig.ModelField f)
        {
            return GetValueExpression(cm, f, "");
        }

        private static string GetValueExpression(ClassMaker cm, GeneratorConfig.ModelField f, string accessor)
        {
            var converter = TypeUtils.GetToStringConverter(f.Type);
            cm.AddUsing(TypeUtils.GetConverterRequiredUsing(f.Type));

            if (TypeUtils.RequiresQuotes(f.Type))
            {
                return " \" " + f.Name.FirstToLower() + ": \\\\\\\"\" + input." + f.Name + accessor + converter + " + \"\\\\\\\"\";";
            }
            else
            {
                return " \" " + f.Name.FirstToLower() + ": \" + input." + f.Name + accessor + converter + ";";
            }
        }

        private string GetIdExpression(ForeignProperty f)
        {
            if (TypeUtils.RequiresQuotes(Config.IdType))
            {
                return "\" " + f.WithId.FirstToLower() + ": \\\\\\\"\" + input." + f.WithId + " + \"\\\\\\\"\"";
            }

            return "\" " + f.WithId.FirstToLower() + ": \" + input." + f.WithId;
        }

        private string GetInitializeFieldsWithId(GeneratorConfig.ModelConfig m)
        {
            if (TypeUtils.RequiresQuotes(Config.IdType))
            {
                return "var fields = \"" + m.Name.FirstToLower() + "Id: \\\\\\\"\" + input." + m.Name + "Id + \"\\\\\\\"\";";
            }
            return "var fields = \"" + m.Name.FirstToLower() + "Id: \" + input." + m.Name + "Id;";
        }
    
        private string GetDeleteFields(GeneratorConfig.ModelConfig m)
        {
            if (TypeUtils.RequiresQuotes(Config.IdType))
            {
                return m.Name.FirstToLower() + "Id: \\\\\\\"\" + input." + m.Name + "Id + \"\\\\\\\"";
            }
            return m.Name.FirstToLower() + "Id: \" + input." + m.Name + "Id + \"";
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
                var fields = GetCreatedSubscriptionFields(m);
                liner.Add("return await SubscribeTo<" + m.Name + ">(\"" + m.Name.FirstToLower() + methodName + "\", " + fields + ");");
            });
        }

        private string GetCreatedSubscriptionFields(GeneratorConfig.ModelConfig m)
        {
            var foreignProperties = GetForeignProperties(m);
            var fields = new[] { "\"id\"" }
                .Concat(m.Fields.Select(f => "\"" + f.Name.FirstToLower() + "\""))
                .Concat(foreignProperties.Select(f => "\"" + f.WithId.FirstToLower() + "\""));

            return string.Join(", ", fields);
        }
    }
}
