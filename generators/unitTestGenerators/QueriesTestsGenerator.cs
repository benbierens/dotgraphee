using System;

public class QueriesUnitTestsGenerator : BaseTestGenerator
{
    public QueriesUnitTestsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateQueriesUnitTests()
    {
        var fm = StartUnitTestFile("Queries", Config.Output.GraphQlSubFolder);
        fm.AddUsing("Moq");
        fm.AddUsing("System.Linq");
        fm.AddUsing("NUnit.Framework");
        fm.AddUsing(Config.GenerateNamespace);

        var cm = fm.AddClass("QueriesTests");
        cm.AddAttribute("TestFixture");

        var dbAccessName = Config.Database.DbAccesserClassName.FirstToLower();
        cm.AddLine("private Mock<I" + Config.Database.DbAccesserClassName + "> " + dbAccessName + " = null!;");
        var queriesName = Config.GraphQl.GqlQueriesClassName.FirstToLower();
        cm.AddLine("private " + Config.GraphQl.GqlQueriesClassName + " " + queriesName + " = null!;");

        cm.AddBlankLine();
        cm.AddLine("[SetUp]");
        cm.AddClosure("public void SetUp()", liner =>
        {
            liner.Add(dbAccessName + " = new Mock<I" + Config.Database.DbAccesserClassName + ">();");
            liner.Add(queriesName + " = new " + Config.GraphQl.GqlQueriesClassName + "(" + dbAccessName + ".Object);");
        });

        foreach (var m in Models)
        {
            AddTest(cm, m.Name + "sShouldReturnQueryable" + m.Name + "s", liner =>
            {
                liner.Add("var queryable = new Mock<IQueryable<" + m.Name + ">>();");
                liner.Add(dbAccessName + ".Setup(s => s.AsQueryable<" + m.Name + ">()).Returns(queryable.Object);");
                liner.AddBlankLine();
                liner.Add("var result = " + queriesName + "." + m.Name + "s();");
                liner.AddBlankLine();
                liner.Add("Assert.That(result, Is.EqualTo(queryable.Object));");
            });
        }

        fm.Build();
    }

    private void AddTest(ClassMaker cm, string name, Action<Liner> liner)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public void " + name + "()", liner);
    }
}
