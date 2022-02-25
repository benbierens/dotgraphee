using System;

public class QueriesUnitTestsGenerator : BaseTestGenerator
{
    public QueriesUnitTestsGenerator(GeneratorConfig config)
        : base(config)
    {
    }


    //private Queries queries = null!;

    //[SetUp]
    //public void SetUp()
    //{
    //    queries = new Queries(dbService.Object);
    //}

    //[Test]
    //public void UserAccountsShouldReturnAllUserAccounts()
    //{
    //    MockDbServiceQueryable(TestData.UserAccount1, TestData.UserAccount2);

    //    var result = queries.UserAccounts();

    //    AssertCollectionEquivalent(result, TestData.UserAccount1, TestData.UserAccount2);
    //}

    //[Test]
    //public void UserAccountShouldReturnOneUserAccount()
    //{
    //    MockDbServiceQueryable(TestData.UserAccount1, TestData.UserAccount2);

    //    var result = queries.UserAccount(TestData.UserAccount2.Id);

    //    AssertCollectionEquivalent(result, TestData.UserAccount2);
    //}


    // base unit test:
//using DotGraphEE_Demo;
//using Moq;
//using NUnit.Framework;
//using System.Linq;

//[TestFixture]
//public abstract class BaseUnitTest
//{
//public UnitTestData TestData { get; set; } = null!;
//public Mock<IDbService> dbService = null!;

//[SetUp]
//public void BaseSetUp()
//{
//    TestData = new UnitTestData();
//    dbService = new Mock<IDbService>();
//}

//public Mock<IQueryable<T>> MockDbServiceQueryable<T>(params T[] list) where T : class, IEntity
//{
//    var mock = new Mock<IQueryable<T>>();
//    var data = list.AsQueryable();

//    mock.Setup(r => r.GetEnumerator()).Returns(data.GetEnumerator());
//    mock.Setup(r => r.Provider).Returns(data.Provider);
//    mock.Setup(r => r.ElementType).Returns(data.ElementType);
//    mock.Setup(r => r.Expression).Returns(data.Expression);

//    dbService.Setup(s => s.AsQueryable<T>()).Returns(mock.Object);

//    return mock;
//}

//public static void AssertCollectionEquivalent<T>(IQueryable<T> queryable, params T[] list) where T : class
//{
//    var data = queryable.ToArray();
//    CollectionAssert.AreEquivalent(list, data);
//}

//}


    public void GenerateQueriesUnitTests()
    {
        var fm = StartUnitTestFile("Queries", Config.Output.GraphQlSubFolder);
        fm.AddUsing("Moq");
        fm.AddUsing("System.Linq");
        fm.AddUsing("NUnit.Framework");
        fm.AddUsing(Config.GenerateNamespace);

        var cm = fm.AddClass("QueriesTests");
        cm.Modifiers.Clear();
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
