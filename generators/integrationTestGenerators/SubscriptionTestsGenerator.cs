using System.Linq;

public class SubscriptionTestsGenerator : BaseTestGenerator
{
    public SubscriptionTestsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateSubscriptionTests()
    {
        var fm = StartIntegrationTestFile("SubscriptionTests");
        var cm = fm.AddClass("SubscriptionTests");
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddInherrit("BaseGqlTest");
        cm.Modifiers.Clear();

        cm.AddSubClass("CreateSubscriptionTests", createCm =>
        {
            createCm.AddInherrit("SubscriptionTests");
            createCm.Modifiers.Clear();
            foreach (var m in Models) AddCreateSubscriptionTest(createCm, m);
        });

        cm.AddSubClass("UpdateSubscriptionTests", updateCm =>
        {
            updateCm.AddInherrit("SubscriptionTests");
            updateCm.Modifiers.Clear();
            foreach (var m in Models)
            {
                if (m.Fields.Any())
                {
                    AddUpdateSubscriptionTest(updateCm, m);
                }
            }
        });

        cm.AddSubClass("DeleteSubscriptionTests", deleteCm =>
        {
            deleteCm.AddInherrit("SubscriptionTests");
            deleteCm.Modifiers.Clear();
            foreach (var m in Models) AddDeleteSubscriptionTest(deleteCm, m);
        });

        fm.Build();
    }

    private void AddCreateSubscriptionTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        if (!IsRequiredSubModel(m))
        {
            AddCreateRootSubscriptionTest(cm, m);

            var requiredSingulars = GetMyRequiredSubModels(m);
            foreach (var r in requiredSingulars)
            {
                AddCreateRequiredSingularSubscriptionTest(cm, m, r);
            }
        }
    }

    private void AddCreateRootSubscriptionTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldPublishSubscriptionOnCreate" + m.Name + "()", liner =>
        {
            liner.Add("var handle = await Gql.SubscribeTo" + m.Name + Config.GraphQl.GqlSubscriptionCreatedMethod + "();");
            liner.AddBlankLine();
            AddCreateLine(liner, m);
            liner.AddBlankLine();
            AddAssertReceiveToEntityVariable(liner, m, Config.GraphQl.GqlSubscriptionCreatedMethod);
            AddAssert(liner).EntityField(m, "Incorrect entity published with " + Config.GraphQl.GqlSubscriptionCreatedMethod + " subscription:");
        });
    }

    private void AddCreateRequiredSingularSubscriptionTest(ClassMaker cm, GeneratorConfig.ModelConfig m, GeneratorConfig.ModelConfig r)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public async Task Create" + m.Name + "ShouldPublishSubscriptionOnCreate" + r.Name + "()", liner =>
        {
            liner.Add("var handle = await Gql.SubscribeTo" + r.Name + Config.GraphQl.GqlSubscriptionCreatedMethod + "();");
            liner.AddBlankLine();
            AddCreateLine(liner, m);
            liner.AddBlankLine();
            AddAssertReceiveToEntityVariable(liner, r, Config.GraphQl.GqlSubscriptionCreatedMethod);
            AddAssert(liner).EntityField(r, "Incorrect entity published with " + Config.GraphQl.GqlSubscriptionCreatedMethod + " subscription:");
        });
    }

    private void AddUpdateSubscriptionTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldPublishSubscriptionOnUpdate" + m.Name + "()", liner =>
        {
            liner.Add("var handle = await Gql.SubscribeTo" + m.Name + Config.GraphQl.GqlSubscriptionUpdatedMethod + "();");
            liner.AddBlankLine();
            AddCreateLine(liner, m);
            liner.AddBlankLine();

            liner.Add("await Gql.Update" + m.Name + "(TestData.To" + inputTypes.Update + "());");
            liner.AddBlankLine();

            AddAssertReceiveToEntityVariable(liner, m, Config.GraphQl.GqlSubscriptionUpdatedMethod);
            foreach (var f in m.Fields)
            {
                AddAssert(liner).EqualsTestScalar(m, f, "Incorrect entity published with " + Config.GraphQl.GqlSubscriptionUpdatedMethod + " subscription.");
            }
        });
    }

    private void AddDeleteSubscriptionTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        if (!IsRequiredSubModel(m))
        {
            AddDeleteRootSubscriptionTest(cm, m);

            var requiredSingulars = GetMyRequiredSubModels(m);
            foreach (var r in requiredSingulars)
            {
                AddDeleteRequiredSingularSubscriptionTest(cm, m, r);
            }
        }
    }

    private void AddDeleteRootSubscriptionTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldPublishSubscriptionOnDelete" + m.Name + "()", liner =>
        {
            liner.Add("var handle = await Gql.SubscribeTo" + m.Name + Config.GraphQl.GqlSubscriptionDeletedMethod + "();");
            liner.AddBlankLine();
            AddCreateLine(liner, m);
            liner.AddBlankLine();
            liner.Add("await Gql.Delete" + m.Name + "(TestData.To" + inputTypes.Delete + "());");
            liner.AddBlankLine();

            AddAssertReceiveToEntityVariable(liner, m, Config.GraphQl.GqlSubscriptionDeletedMethod);
            AddAssert(liner).IdEquals(m, "Incorrect entity ID published with " + Config.GraphQl.GqlSubscriptionDeletedMethod + " subscription:");
        });
    }

    private void AddDeleteRequiredSingularSubscriptionTest(ClassMaker cm, GeneratorConfig.ModelConfig m, GeneratorConfig.ModelConfig r)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task Delete" + m.Name + "ShouldPublishSubscriptionOnDelete" + r.Name + "()", liner =>
        {
            liner.Add("var handle = await Gql.SubscribeTo" + r.Name + Config.GraphQl.GqlSubscriptionDeletedMethod + "();");
            liner.AddBlankLine();
            AddCreateLine(liner, m);
            liner.AddBlankLine();
            liner.Add("await Gql.Delete" + m.Name + "(TestData.To" + inputTypes.Delete + "());");
            liner.AddBlankLine();

            AddAssertReceiveToEntityVariable(liner, r, Config.GraphQl.GqlSubscriptionDeletedMethod);
            AddAssert(liner).IdEquals(r, "Incorrect entity ID published with " + Config.GraphQl.GqlSubscriptionDeletedMethod + " subscription:");
        });
    }

    private void AddAssertReceiveToEntityVariable(Liner liner, GeneratorConfig.ModelConfig m, string methodName)
    {
        liner.Add("var entity = handle.AssertReceived()." + m.Name + methodName + ";");
    }

}
