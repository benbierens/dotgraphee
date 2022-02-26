using System;

public class SubscriptionsUnitTestsGenerator : BaseUnitTestGenerator
{
    private string subsName;

    public SubscriptionsUnitTestsGenerator(GeneratorConfig config)
        : base(config)
    {
        subsName = Config.GraphQl.GqlSubscriptionsClassName.FirstToLower();
    }

    public void GenerateSubscriptionsUnitTests()
    {
        var fm = StartUnitTestFile("Subscriptions", Config.Output.GraphQlSubFolder);
        fm.AddUsing("NUnit.Framework");
        fm.AddUsing(Config.GenerateNamespace);

        var cm = fm.AddClass("SubscriptionsTests");
        cm.AddInherrit("BaseUnitTest");
        cm.Modifiers.Clear();
        cm.AddAttribute("TestFixture");

        cm.AddLine("private " + Config.GraphQl.GqlSubscriptionsClassName + " " + subsName + " = null!;");

        cm.AddBlankLine();
        cm.AddLine("[SetUp]");
        cm.AddClosure("public void SetUp()", liner =>
        {
            liner.Add(subsName + " = new " + Config.GraphQl.GqlSubscriptionsClassName + "(" + GetDbAccessorName() + ".Object);");
        });

        foreach (var m in Models)
        {
            AddCreatedTest(cm, m);
            AddUpdatedTest(cm, m);
            AddDeletedTest(cm, m);
        }

        fm.Build();
    }

    private void AddCreatedTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        AddQueryableSubscriptionFunctionTest(cm, m, Config.GraphQl.GqlSubscriptionCreatedMethod);
    }

    private void AddUpdatedTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        AddQueryableSubscriptionFunctionTest(cm, m, Config.GraphQl.GqlSubscriptionUpdatedMethod);
    }

    private void AddDeletedTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var target = m.Name + Config.GraphQl.GqlSubscriptionDeletedMethod;
        AddTest(cm, target + "ShouldReturnEntity", liner =>
        {
            liner.Add("var result = " + subsName + "." + target + "(TestData." + m.Name + "1);");
            liner.AddBlankLine();
            liner.Add("Assert.That(result, Is.EqualTo(TestData." + m.Name + "1));");
        });
    }

    private void AddQueryableSubscriptionFunctionTest(ClassMaker cm, GeneratorConfig.ModelConfig m, string subscription)
    {
        var target = m.Name + subscription;
        AddTest(cm, target + "ShouldReturnQueryable", liner =>
        {
            liner.Add("var expectedResult = " + GetMockDbServiceQueryableEntityFunctionName() + "(TestData." + m.Name + "1);");
            liner.AddBlankLine();
            liner.Add("var result = " + subsName + "." + target + "(TestData." + m.Name + "1);");
            liner.AddBlankLine();
            liner.Add("AssertQueryableAreEqual(expectedResult.Object, result);");
        });
    }
}
