using System;
using System.Linq;

public class SubscriptionTestsGenerator : BaseGenerator
{
    public SubscriptionTestsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateSubscriptionTests()
    {
        var fm = StartTestFile("SubscriptionTests");
        var cm = fm.AddClass("SubscriptionTests");
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddUsing(Config.GenerateNamespace);
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
        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldPublishSubscriptionOnCreate" + m.Name + "()", liner =>
        {
            liner.Add("var handle = await Gql.SubscribeTo" + m.Name + Config.GraphQl.GqlSubscriptionCreatedMethod + "();");
            liner.AddBlankLine();
            liner.Add("await CreateTest" + m.Name + "();");
            liner.AddBlankLine();
            liner.Add("var entity = handle.AssertReceived();");
            AddEntityFieldAsserts(liner, m, "Incorrect entity published with " + Config.GraphQl.GqlSubscriptionCreatedMethod + " subscription:");
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
            liner.Add("await CreateTest" + m.Name + "();");
            liner.AddBlankLine();

            liner.Add("await Gql.Update" + m.Name + "(TestData.To" + inputTypes.Update + "());");
            liner.AddBlankLine();

            liner.Add("var entity = handle.AssertReceived();");
            foreach (var f in m.Fields)
            {
                AddAssertEqualsTestScalar(liner, m, f, "Incorrect entity published with " + Config.GraphQl.GqlSubscriptionUpdatedMethod + " subscription.");
            }
        });
    }

    private void AddDeleteSubscriptionTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldPublishSubscriptionOnDelete" + m.Name + "()", liner =>
        {
            liner.Add("var handle = await Gql.SubscribeTo" + m.Name + Config.GraphQl.GqlSubscriptionDeletedMethod + "();");
            liner.AddBlankLine();
            liner.Add("await CreateTest" + m.Name + "();");
            liner.AddBlankLine();
            liner.Add("await Gql.Delete" + m.Name + "(TestData.To" + inputTypes.Delete + "());");
            liner.AddBlankLine();

            liner.Add("var entity = handle.AssertReceived();");
            AddEntityFieldAsserts(liner, m, "Incorrect entity published with " + Config.GraphQl.GqlSubscriptionDeletedMethod + " subscription:");
        });
    }
}
