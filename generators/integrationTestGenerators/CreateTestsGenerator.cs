public class CreateTestsGenerator : BaseTestGenerator
{
    public CreateTestsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateCreateTests()
    {
        var fm = StartIntegrationTestFile("CreateTests");
        var cm = fm.AddClass("CreateTests");
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddInherrit("BaseGqlTest");
        cm.Modifiers.Clear();

        foreach (var m in Models)
        {
            if (!IsRequiredSubModel(m))
            {
                AddCreateTest(cm, m);
            }
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
            AddAssert(liner).EntityFields(m, "Incorrect entity returned after creation:");

            var requiredSingulars = GetMyRequiredSubModels(m);
            foreach (var r in requiredSingulars)
            {
                liner.AddBlankLine();
                AddAssert(liner, "entity." + r.Name)
                    .EntityFields(r, "Incorrect sub-entity returned after creation:");
                
            }
        });
    }
}
