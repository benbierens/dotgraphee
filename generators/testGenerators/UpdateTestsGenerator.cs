using System.Linq;

public class UpdateTestsGenerator : BaseTestGenerator
{
    public UpdateTestsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateUpdateTests()
    {
        var fm = StartTestFile("UpdateTests");
        var cm = fm.AddClass("UpdateTests");
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddInherrit("BaseGqlTest");
        cm.Modifiers.Clear();

        foreach (var m in Models)
        {
            if (m.Fields.Any())
            {
                AddUpdateTest(cm, m);
                AddUpdateAndQueryTest(cm, m);
                AddUpdateFailedToFindTest(cm, m);
            }
        }

        fm.Build();
    }

    private void AddUpdateTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task UpdateShouldReturnUpdated" + m.Name + "()", liner =>
        {
            AddCreateLine(liner, m);
            liner.AddBlankLine();

            liner.Add("var gqlData = await Gql.Update" + m.Name + "(TestData.To" + inputTypes.Update + "());");
            AddAssert(liner).NoErrors();
            liner.Add("var entity = gqlData.Data." + Config.GraphQl.GqlMutationsUpdateMethod + m.Name + ";");
            if (IsFailedToFindStrategyNullObject())
            {
                AddAssert(liner).EntityNotNull(Config.GraphQl.GqlMutationsUpdateMethod);
            }
            liner.AddBlankLine();

            AddAssert(liner).IdEquals(m, "Update failed.");
            foreach (var f in m.Fields)
            {
                AddAssert(liner).EqualsTestScalar(m, f, "Update failed.");
            }
        });
    }

    private void AddUpdateAndQueryTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task QueryShouldReturnUpdated" + m.Name + "()", liner =>
        {
            AddCreateLine(liner, m);
            liner.AddBlankLine();

            liner.Add("await Gql.Update" + m.Name + "(TestData.To" + inputTypes.Update + "());");
            liner.AddBlankLine();            

            liner.Add("var gqlData = await Gql.QueryAll" + m.Name + "s();");
            AddAssert(liner).NoErrors();
            AddDereferenceToAllVariable(liner, m);
            liner.AddBlankLine();

            AddAssert(liner).CollectionOne(m, "all");
            liner.Add("var entity = all[0];");
            AddAssert(liner).IdEquals(m, "Update failed.");
            foreach (var f in m.Fields)
            {
                AddAssert(liner).EqualsTestScalar(m, f, "Update failed.");
            }
        });
    }

    private void AddUpdateFailedToFindTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task UpdateShouldReturn" + GetErrorOrNull() + "WhenFailedToFind" + m.Name + "()", liner =>
        {
            liner.Add("var gqlData = await Gql.Update" + m.Name + "(TestData.To" + inputTypes.Update + "());");
            liner.Add("var errors = gqlData.Errors;");
            liner.AddBlankLine();

            AddAssert(liner).FailedToFindMutationResponse(m, Config.GraphQl.GqlMutationsUpdateMethod);
        });
    }
}
