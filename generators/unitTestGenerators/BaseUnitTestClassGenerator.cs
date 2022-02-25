public class BaseUnitTestClassGenerator : BaseUnitTestGenerator
{
    public BaseUnitTestClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateBaseUnitTestClass()
    {
        var fm = StartUnitTestUtilsFile("BaseUnitTest");
        fm.AddUsing(Config.GenerateNamespace);
        fm.AddUsing("Moq");
        fm.AddUsing("NUnit.Framework");
        fm.AddUsing("System.Linq");

        var cm = fm.AddClass("BaseUnitTest");
        cm.AddAttribute("TestFixture");
        cm.Modifiers.Clear();
        cm.Modifiers.Add("abstract");

        cm.AddProperty("TestData")
            .IsType("UnitTestData")
            .InitializeAsExplicitNull()
            .Build();

        cm.AddProperty(GetDbAccessorName())
            .IsType(GetDbAccessorName())
            .InitializeAsExplicitNull()
            .Build();

        cm.AddLine("[SetUp]");
        cm.AddClosure("public void BaseSetUp()", liner =>
        {
            liner.Add("TestData = new UnitTestData();");
            liner.Add("dbService = new Mock<IDbService>();");
        });

        cm.AddClosure("public Mock<IQueryable<T>> " + GetMockDbServiceQueryableFunctionName() + "<T>(params T[] list) where T : class, IEntity", liner =>
        {
            liner.Add("var mock = new Mock<IQueryable<T>>();");
            liner.Add("var data = list.AsQueryable();");
            liner.Add("mock.Setup(r => r.GetEnumerator()).Returns(data.GetEnumerator());");
            liner.Add("mock.Setup(r => r.Provider).Returns(data.Provider);");
            liner.Add("mock.Setup(r => r.ElementType).Returns(data.ElementType);");
            liner.Add("mock.Setup(r => r.Expression).Returns(data.Expression);");
            liner.Add(GetDbAccessorName() + ".Setup(s => s.AsQueryable<T>()).Returns(mock.Object);");
            liner.Add("return mock;");
        });

        cm.AddClosure("public static void AssertCollectionEquivalent<T>(IQueryable<T> queryable, params T[] list) where T : class", liner =>
        {
            liner.Add("var data = queryable.ToArray();");
            liner.Add("CollectionAssert.AreEquivalent(list, data);");
        });

        fm.Build();
    }
}
