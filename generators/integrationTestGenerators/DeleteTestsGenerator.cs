
public class DeleteTestsGenerator : BaseTestGenerator
{
    public DeleteTestsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateDeleteTests()
    {
        var fm = StartIntegrationTestFile("DeleteTests");
        var cm = fm.AddClass("DeleteTests");
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddInherrit("BaseGqlTest");
        cm.Modifiers.Clear();

        foreach (var m in Models)
        {
            if (!IsRequiredSubModel(m))
            {
                AddDeleteTest(cm, m);
                AddDeleteFailedToFindTest(cm, m);
            }

            var requiredSingulars = GetMyRequiredSubModels(m);
            foreach (var r in requiredSingulars)
            {
                AddDeleteTestForRequiredSingular(cm, m, r);
            }
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
            AddAssert(liner).DeleteResponse(m);
            liner.AddBlankLine();
            
            liner.Add("var gqlData = await Gql.QueryAll" + m.Name + "s();");
            AddAssert(liner).NoErrors();
            AddDereferenceToAllVariable(liner, m);
            liner.AddBlankLine();
            AddAssert(liner).CollectionEmpty(m, "all");
        });
    }

    private void AddDeleteTestForRequiredSingular(ClassMaker cm, GeneratorConfig.ModelConfig m, GeneratorConfig.ModelConfig r)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task Delete" + m.Name + "ShouldDelete" + r.Name + "()", liner =>
        {
            liner.Add("await CreateTest" + m.Name + "();");
            liner.AddBlankLine();

            liner.Add("var response = await Gql.Delete" + m.Name + "(TestData.To" + inputTypes.Delete + "());");
            AddAssert(liner).DeleteResponse(m);
            liner.AddBlankLine();
            
            liner.Add("var gqlData = await Gql.QueryAll" + r.Name + "s();");
            AddAssert(liner).NoErrors();
            AddDereferenceToAllVariable(liner, r);
            liner.AddBlankLine();
            AddAssert(liner).CollectionEmpty(r, "all");
        });
    }

    private void AddDeleteFailedToFindTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task DeleteShouldReturn" + GetErrorOrNull() + "WhenFailedToFind" + m.Name + "()", liner =>
        {
            liner.Add("var gqlData = await Gql.Delete" + m.Name + "(TestData.To" + inputTypes.Delete + "());");
            liner.Add("var errors = gqlData.Errors;");
            liner.AddBlankLine();

            AddAssert(liner).FailedToFindMutationResponse(m, Config.GraphQl.GqlMutationsDeleteMethod);
        });
    }

}
