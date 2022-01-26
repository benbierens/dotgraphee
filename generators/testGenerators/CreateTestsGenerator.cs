public class CreateTestsGenerator : BaseGenerator
{
    public CreateTestsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateCreateTests()
    {
        var fm = StartTestFile("CreateTests");
        var cm = fm.AddClass("CreateTests");
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddInherrit("BaseGqlTest");
        cm.Modifiers.Clear();

        foreach (var m in Models)
        {
            AddCreateTest(cm, m);
        }

        fm.Build();
    }

    private void AddCreateTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldCreate" + m.Name + "()", liner =>
        {
            liner.Add("var entity = await CreateTest" + m.Name + "();");
            liner.AddBlankLine();
            AddEntityFieldAsserts(liner, m, "Incorrect entity returned after creation:");
        });
    }
}
