using System;

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
            AddDeleteFailedToFindTest(cm, m);
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

            liner.Add("var response = await Gql.Delete" + m.Name + "(TestData.To" + inputTypes.Delete + "());");
            AddAssertDeleteResponse(liner, m);
            liner.AddBlankLine();
            
            liner.Add("var gqlData = await Gql.QueryAll" + m.Name + "s();");
            AddAssertNoErrors(liner);
            liner.Add("var all = gqlData.Data." + m.Name + "s;");
            liner.AddBlankLine();
            liner.Add("CollectionAssert.IsEmpty(all, \"Expected " + m.Name + " to have been deleted.\");");
        });
    }

    private void AddDeleteFailedToFindTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task DeleteShouldReturnErrorWhenFailedToFind" + m.Name + "()", liner =>
        {
            liner.Add("var gqlData = await Gql.Delete" + m.Name + "(TestData.To" + inputTypes.Delete + "());");
            liner.Add("var errors = gqlData.Errors;");
            liner.AddBlankLine();

            AddAssertCollectionOne(liner, m, "errors");
            AddAssertErrorMessage(liner, m, "TestData.Test" + Config.IdType.FirstToUpper());
        });
    }

}
