using System;
using System.Collections.Generic;
using System.Linq;

public class GqlClassGenerator : BaseGenerator
{
    private readonly QueryAllMethodSubgenerator queryAllMethodSubgenerator;
    private readonly MutationMethodsSubgenerator mutationMethodsSubgenerator;
    private readonly SubscriptionMethodsSubgenerator subscriptionMethodsSubgenerator;

    public GqlClassGenerator(GeneratorConfig config)
        : base(config)
    {
        queryAllMethodSubgenerator = new QueryAllMethodSubgenerator(config);
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
            queryAllMethodSubgenerator.AddQueryAllMethod(cm, m);
            mutationMethodsSubgenerator.AddMutationMethods(cm, m);
            subscriptionMethodsSubgenerator.AddSubscribeMethods(cm, m);
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

    public class QueryAllMethodSubgenerator : BaseGenerator
    {
        public QueryAllMethodSubgenerator(GeneratorConfig config)
            : base(config)
        {
        }

        public void AddQueryAllMethod(ClassMaker cm, GeneratorConfig.ModelConfig m)
        {
            cm.AddClosure("public async Task<List<" + m.Name + ">> QueryAll" + m.Name + "s()", liner =>
            {
                liner.Add("var query = \"{ \\\"query\\\": \\\"query { " + m.Name.FirstToLower() + "s { " + GetQueryFields(m) + " } } \\\" }\";");
                liner.Add("var data = await Client.PostRequest<All" + m.Name + "sQuery>(query);");
                liner.Add("return data." + m.Name + "s;");
            });
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
            cm.AddClosure("public async Task<" + m.Name + "> Create" + m.Name + "(" + inputNames.Create + " input)", liner =>
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
                        liner.Add("if (input." + f.WithId + " != null) fields += \" " + f.WithId.FirstToLower() + ": \" + input." + f.WithId + ";");
                    }
                    else
                    {
                        liner.Add("fields += \" " + f.WithId.FirstToLower() + ": \" + input." + f.WithId + ";");
                    }
                }
                liner.AddBlankLine();
                var queryFields = string.Join(" ", fields);
                liner.Add("var mutation = \"{ \\\"query\\\": \\\"mutation { " + Config.GraphQl.GqlMutationsCreateMethod.FirstToLower() + m.Name + "(input: { \" + fields + \" }) { " + queryFields + " } }\\\"}\";");

                var templateField = Config.GraphQl.GqlMutationsCreateMethod + m.Name;
                var templateType = templateField + "Response";
                liner.Add("var data = await Client.PostRequest<" + templateType + ">(mutation);");
                liner.Add("return data." + templateField + ";");
            });
        }

        private void AddUpdateMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
        {
            cm.AddClosure("public async Task<" + m.Name + "> Update" + m.Name + "(" + inputNames.Update + " input)", liner =>
            {
                liner.Add("var fields = \"" + m.Name.FirstToLower() + "Id: \" + input." + m.Name + "Id;");
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
                    liner.Add("if (input." + f.WithId + " != null) fields += \" " + f.WithId.FirstToLower() + ": \" + input." + f.WithId + ";");
                }
                liner.AddBlankLine();
                var queryFields = string.Join(" ", fields);
                liner.Add("var mutation = \"{ \\\"query\\\": \\\"mutation { " + Config.GraphQl.GqlMutationsUpdateMethod.FirstToLower() + m.Name + "(input: { \" + fields + \" }) { " + queryFields + " } }\\\"}\";");

                var templateField = Config.GraphQl.GqlMutationsUpdateMethod + m.Name;
                var templateType = templateField + "Response";
                liner.Add("var data = await Client.PostRequest<" + templateType + ">(mutation);");
                liner.Add("return data." + templateField + ";");
            });
        }

        private void AddDeleteMutationMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
        {
            cm.AddClosure("public async Task<" + Config.IdType + "> Delete" + m.Name + "(" + inputNames.Delete + " input)", liner =>
            {
                var fields = m.Name.FirstToLower() + "Id: \" + input." + m.Name + "Id + \"";
                liner.Add("var mutation = \"{ \\\"query\\\": \\\"mutation { " + Config.GraphQl.GqlMutationsDeleteMethod.FirstToLower() + m.Name + "(input: {" + fields + "}) { id } }\\\"}\";");
                liner.Add("var data = await Client.PostRequest<MutationResponse>(mutation);");
                liner.Add("return data.Id;");
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
