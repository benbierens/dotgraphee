public class DeleteTestsGenerator : BaseGenerator
{
    public DeleteTestsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateDeleteTests()
    {
        var fm = StartTestFile("DeleteTests");
        var cm = fm.AddClass("DeleteTests");
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddUsing(Config.GenerateNamespace);
        cm.AddInherrit("BaseGqlTest");
        cm.Modifiers.Clear();

        foreach (var m in Models)
        {
            AddDeleteTest(cm, m);
        }

        fm.Build();
    }

    private void AddDeleteTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldDelete" + m.Name + "()", liner =>
        {
            liner.Add("await CreateTest" + m.Name + "();");
            liner.AddBlankLine();
            liner.StartClosure("await Gql.Delete" + m.Name + "(new " + inputTypes.Delete);
            liner.Add(m.Name + "Id = TestData.Test" + m.Name + ".Id,");
            liner.EndClosure(");");

            liner.Add("var all = await Gql.QueryAll" + m.Name + "s();");
            liner.AddBlankLine();
            liner.Add("CollectionAssert.IsEmpty(all, \"Expected " + m.Name + " to have been deleted.\");");
        });
    }
}
