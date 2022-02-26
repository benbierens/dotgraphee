
public class QueriesUnitTestsGenerator : BaseUnitTestGenerator
{
    private string queriesName;

    public QueriesUnitTestsGenerator(GeneratorConfig config)
        : base(config)
    {
        queriesName = Config.GraphQl.GqlQueriesClassName.FirstToLower();
    }

    public void GenerateQueriesUnitTests()
    {
        var fm = StartUnitTestFile("Queries", Config.Output.GraphQlSubFolder);
        fm.AddUsing("NUnit.Framework");
        fm.AddUsing(Config.GenerateNamespace);

        var cm = fm.AddClass("QueriesTests");
        cm.AddInherrit("BaseUnitTest");
        cm.Modifiers.Clear();
        cm.AddAttribute("TestFixture");

        cm.AddLine("private " + Config.GraphQl.GqlQueriesClassName + " " + queriesName + " = null!;");

        cm.AddBlankLine();
        cm.AddLine("[SetUp]");
        cm.AddClosure("public void SetUp()", liner =>
        {
            liner.Add(queriesName + " = new " + Config.GraphQl.GqlQueriesClassName + "(" + GetDbAccessorName() + ".Object);");
        });

        foreach (var m in Models)
        {
            AddReturnsAllTest(cm, m);
            AddReturnsOneTest(cm, m);
        }

        fm.Build();
    }

    private void AddReturnsAllTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        AddTest(cm, m.Name + "sShouldReturnAll" + m.Name + "s", liner =>
        {
            liner.Add(GetMockDbServiceQueryableFunctionName() + "(" +
                "TestData." + m.Name + "1," +
                "TestData." + m.Name + "2" +
                ");");

            liner.AddBlankLine();
            liner.Add("var result = " + queriesName + "." + m.Name + "s();");
            liner.AddBlankLine();

            liner.Add("AssertCollectionEquivalent(result, " +
                "TestData." + m.Name + "1," +
                "TestData." + m.Name + "2" +
                ");");
        });
    }

    private void AddReturnsOneTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        AddTest(cm, m.Name + "ShouldReturnOne" + m.Name + "s", liner =>
        {
            liner.Add(GetMockDbServiceQueryableFunctionName() + "(" +
                "TestData." + m.Name + "1," +
                "TestData." + m.Name + "2" +
                ");");

            liner.AddBlankLine();
            liner.Add("var result = " + queriesName + "." + m.Name + "(TestData." +
                m.Name + "2.Id" +
                ");");
            liner.AddBlankLine();

            liner.Add("AssertCollectionEquivalent(result, " +
                "TestData." + m.Name + "2" +
                ");");
        });
    }
}
