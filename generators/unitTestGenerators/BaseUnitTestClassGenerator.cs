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
        fm.AddUsing("System.Collections.Generic");

        var cm = fm.AddClass("BaseUnitTest");
        cm.AddAttribute("TestFixture");
        cm.Modifiers.Clear();
        cm.Modifiers.Add("abstract");

        cm.AddProperty("TestData")
            .IsType("UnitTestData")
            .InitializeAsExplicitNull()
            .Build();

        cm.AddProperty(GetDbAccessorName())
            .IsType("Mock<I" + GetDbAccessorName() + ">")
            .InitializeAsExplicitNull()
            .Build();

        cm.AddBlankLine();

        cm.AddLine("[SetUp]");
        cm.AddClosure("public void BaseSetUp()", liner =>
        {
            liner.Add("TestData = new UnitTestData();");
            liner.Add(GetDbAccessorName() + " = new Mock<I" + GetDbAccessorName() + ">();");
        });

        cm.AddClosure("public Mock<IQueryable<T>> " + GetMockDbServiceQueryableFunctionName() + "<T>(params T[] list) where T : class, IEntity", liner =>
        {
            liner.Add("var data = list.AsQueryable();");
            liner.Add("var mock = CreateMockQueryable(data);");
            //liner.Add("mock.Setup(r => r.GetEnumerator()).Returns(data.GetEnumerator());");
            //liner.Add("mock.Setup(r => r.Provider).Returns(data.Provider);");
            //liner.Add("mock.Setup(r => r.ElementType).Returns(data.ElementType);");
            //liner.Add("mock.Setup(r => r.Expression).Returns(data.Expression);");
            liner.Add(GetDbAccessorName() + ".Setup(s => s.AsQueryable<T>()).Returns(mock.Object);");
            liner.Add("return mock;");
        });

        cm.AddClosure("public Mock<IQueryable<T>> " + GetMockDbServiceQueryableEntityFunctionName() + "<T>(T argumentEntity) where T : class, IEntity", liner =>
        {
            liner.Add("var data = new List<T>().AsQueryable();");
            liner.Add("var mock = CreateMockQueryable(data);");
            liner.Add(GetDbAccessorName() + ".Setup(s => s.AsQueryableEntity(argumentEntity)).Returns(mock.Object);");
            liner.Add("return mock;");
        });

        cm.AddClosure("public static void AssertCollectionEquivalent<T>(IQueryable<T> queryable, params T[] list) where T : class", liner =>
        {
            liner.Add("var data = queryable.ToArray();");
            liner.Add("CollectionAssert.AreEquivalent(list, data);");
        });

        cm.AddClosure("public static void AssertQueryableAreEqual<T>(IQueryable<T> expected, IQueryable<T> actual)", liner =>
        {
            liner.Add("if (ReferenceEquals(expected, actual)) return;");
            liner.Add("Assert.NotNull(expected);");
            liner.Add("Assert.NotNull(actual);");
            liner.Add("CollectionAssert.AreEquivalent(expected.ToArray(), actual.ToArray());");
        });

        cm.AddClosure("private static Mock<IQueryable<T>> CreateMockQueryable<T>(IQueryable<T> source)", liner =>
        {
            liner.Add("var mock = new Mock<IQueryable<T>>();");
            liner.Add("mock.Setup(r => r.GetEnumerator()).Returns(source.GetEnumerator());");
            liner.Add("mock.Setup(r => r.Provider).Returns(source.Provider);");
            liner.Add("mock.Setup(r => r.ElementType).Returns(source.ElementType);");
            liner.Add("mock.Setup(r => r.Expression).Returns(source.Expression);");
            liner.Add("return mock;");
        });

        fm.Build();
    }
}
