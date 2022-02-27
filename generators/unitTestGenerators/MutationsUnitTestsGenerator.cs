using System;

public class MutationsUnitTestsGenerator : BaseUnitTestGenerator
{
    private readonly string mutationsName;

    public MutationsUnitTestsGenerator(GeneratorConfig config)
        : base(config)
    {
        mutationsName = config.Config.GraphQl.GqlMutationsClassName.FirstToLower();
    }

    public void GenerateMutationUnitTests()
    {
        var fm = StartUnitTestFile("Mutations", Config.Output.GraphQlSubFolder);
        fm.AddUsing("NUnit.Framework");
        fm.AddUsing("HotChocolate.Subscriptions");
        fm.AddUsing("Moq");
        fm.AddUsing("System");
        fm.AddUsing("System.Threading.Tasks");
        fm.AddUsing(Config.GenerateNamespace);

        var cm = fm.AddClass("MutationTests");
        cm.AddInherrit("BaseUnitTest");
        cm.Modifiers.Clear();
        cm.AddAttribute("TestFixture");

        cm.AddLine("private " + Config.GraphQl.GqlMutationsClassName + " " + mutationsName + " = null!;");
        cm.AddLine("private Mock<ITopicEventSender> sender = null!;");
        cm.AddLine("private Mock<IPublisher> publisher = null!;");
        cm.AddLine("private Mock<IInputConverter> inputConverter = null!;");

        cm.AddBlankLine();
        cm.AddLine("[SetUp]");
        cm.AddClosure("public void SetUp()", liner =>
        {
            liner.Add("sender = new Mock<ITopicEventSender>();");
            liner.Add("publisher = new Mock<IPublisher>();");
            liner.Add("inputConverter = new Mock<IInputConverter>();");
            liner.AddBlankLine();
            liner.Add(mutationsName + " = new " + Config.GraphQl.GqlMutationsClassName + "(" + GetDbAccessorName() + ".Object, publisher.Object, inputConverter.Object);");
            liner.AddBlankLine();
            
            foreach (var m in Models)
            {
                var inputTypes = GetInputTypeNames(m);
                liner.Add("inputConverter.Setup(i => i.ToDto(TestData." + inputTypes.Create + ")).Returns(TestData." + m.Name + "1);");
            }
        });

        foreach (var m in Models)
        {
            cm.BeginRegion(m.Name);

            var inputTypes = GetInputTypeNames(m);
            AddCreateTests(cm, m, inputTypes);
            AddUpdateTests(cm, m, inputTypes);
            AddDeleteTests(cm, m, inputTypes);

            cm.EndRegion();
        }

        fm.Build();
    }

    private void AddCreateTests(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputTypes)
    {
        if (IsRequiredSubModel(m)) return;
        var method = Config.GraphQl.GqlMutationsCreateMethod;

        AddAsyncTest(cm, method + m.Name + "ShouldConvertInput", liner =>
        {
            liner.Add("await " + mutationsName + "." + method + m.Name + "(TestData." + inputTypes.Create + ", sender.Object);");
            liner.AddBlankLine();
            liner.Add("inputConverter.Verify(i => i.ToDto(TestData." + inputTypes.Create + "));");
        });

        AddAsyncTest(cm, method + m.Name + "ShouldAdd" + m.Name, liner =>
        {
            liner.Add("await " + mutationsName + "." + method + m.Name + "(TestData." + inputTypes.Create + ", sender.Object);");
            liner.AddBlankLine();
            liner.Add("DbService.Verify(db => db.Add(TestData." + m.Name + "1));");
        });

        var pub = "Publish" + m.Name + Config.GraphQl.GqlSubscriptionCreatedMethod;
        AddAsyncTest(cm, method + m.Name + "Should" + pub, liner =>
        {
            liner.Add("await " + mutationsName + "." + method + m.Name + "(TestData." + inputTypes.Create + ", sender.Object);");
            liner.AddBlankLine();
            liner.Add("publisher.Verify(p => p." + pub + "(sender.Object, TestData." + m.Name + "1));");
        });

        AddAsyncTest(cm, method + m.Name + "ShouldReturn" + m.Name + "Queryable", liner =>
        {
            liner.Add("var expectedResult = MockDbServiceQueryableEntity(TestData." + m.Name + "1);");
            liner.AddBlankLine();
            liner.Add("var result = await " + mutationsName + "." + method + m.Name + "(TestData." + inputTypes.Create + ", sender.Object);");
            liner.AddBlankLine();
            liner.Add("AssertQueryableAreEqual(expectedResult.Object, result);");
        });
    }

    private void AddUpdateTests(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputTypes)
    {
    }

    private void AddDeleteTests(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputTypes)
    {
    }
}
