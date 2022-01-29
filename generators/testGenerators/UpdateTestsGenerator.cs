using System;
using System.Linq;

public class UpdateTestsGenerator : BaseGenerator
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
        cm.AddUsing(Config.GenerateNamespace);
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
            liner.Add("await CreateTest" + m.Name + "();");
            liner.AddBlankLine();

            liner.Add("var gqlData = await Gql.Update" + m.Name + "(TestData.To" + inputTypes.Update + "());");
            AddAssertNoErrors(liner);
            liner.Add("var entity = gqlData.Data." + Config.GraphQl.GqlMutationsUpdateMethod + m.Name + ";");
            liner.AddBlankLine();

            AddAssertId(liner, m, "Update failed.");
            foreach (var f in m.Fields)
            {
                AddAssertEqualsTestScalar(liner, m, f, "Update failed.");
            }
        });
    }

    private void AddUpdateAndQueryTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task QueryShouldReturnUpdated" + m.Name + "()", liner =>
        {
            liner.Add("await CreateTest" + m.Name + "();");
            liner.AddBlankLine();

            liner.Add("await Gql.Update" + m.Name + "(TestData.To" + inputTypes.Update + "());");
            liner.AddBlankLine();            

            liner.Add("var gqlData = await Gql.QueryAll" + m.Name + "s();");
            AddAssertNoErrors(liner);
            liner.Add("var all = gqlData.Data." + m.Name + "s;");
            liner.AddBlankLine();

            AddAssertCollectionOne(liner, m, "all");
            liner.Add("var entity = all[0];");
            AddAssertId(liner, m, "Update failed.");
            foreach (var f in m.Fields)
            {
                AddAssertEqualsTestScalar(liner, m, f, "Update failed.");
            }
        });
    }

    private void AddUpdateFailedToFindTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddLine("[Test]");
        cm.AddClosure("public async Task UpdateShouldReturnErrorWhenFailedToFind" + m.Name + "()", liner =>
        {
            liner.Add("var gqlData = await Gql.Update" + m.Name + "(TestData.To" + inputTypes.Update + "());");
            liner.Add("var errors = gqlData.Errors;");
            liner.AddBlankLine();

            AddAssertCollectionOne(liner, m, "errors");
            AddAssertErrorMessage(liner, m, "TestData.Test" + m.Name + ".Id");
        });
    }
}
