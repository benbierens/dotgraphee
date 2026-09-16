
using System.Linq;

public class QueryTestsGenerator : BaseTestGenerator
{
    public QueryTestsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateQueryTests()
    {
        var fm = StartIntegrationTestFile("QueryTests");
        var cm = fm.AddClass("QueryTests");
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddInherrit("BaseGqlTest");
        cm.Modifiers.Clear();

        foreach (var m in Models)
        {
            AddQueryAllTest(cm, m);
            AddQueryOneTest(cm, m);
            AddQueryOneFailedToFindTest(cm, m);
        }
        
        fm.Build();
    }

    private void AddQueryAllTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldQueryAll" + m.Name + "s()", liner =>
        {
            AddCreateTestEntityLine(liner, m);
            liner.AddBlankLine();
            liner.Add("var gqlData = await Gql.QueryAll" + m.Name + "s();");
            AddAssert(liner).NoErrors();
            AddDereferenceToAllVariable(liner, m);

            liner.AddBlankLine();
            AddAssert(liner, "all").EntityNotNull("QueryAll" + m.Name);
            AddAssert(liner).CollectionOne(m, "all");
            liner.AddBlankLine();

            liner.Add("var entity = all[0];");
            AddAssert(liner).EntityFields(m, "All-query incorrect.");
        });
    }

    private void AddQueryOneTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldQueryOne" + m.Name + "ById()", liner =>
        {
            AddCreateTestEntityLine(liner, m);
            liner.AddBlankLine();
            liner.Add("var gqlData = await Gql.QueryOne" + m.Name + "(TestData." + m.Name + "1.Id);");
            AddAssert(liner).NoErrors();
            liner.Add("var entity = gqlData.Data?." + m.Name + ";");
            AddAssert(liner).EntityNotNull("QueryOne" + m.Name);
            liner.AddBlankLine();
            AddAssert(liner).EntityFields(m, "One-query incorrect.");
        });
    }

    private void AddQueryOneFailedToFindTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldReturnNullWhenQueryOne" + m.Name + "ByIncorrectId()", liner =>
        {
            liner.Add("var gqlData = await Gql.QueryOne" + m.Name + "(TestData." + Config.IdType.FirstToUpper() + ");");
            AddAssert(liner).NoErrors();
            liner.AddBlankLine();
            AddAssert(liner).NullReturned(m, "");
        });
    }

    private void AddCreateTestEntityLine(Liner liner, GeneratorConfig.ModelConfig m)
    {
        if (IsRequiredSubModel(m))
        {
            var superModels = GetMyRequiredSuperModels(m);
            liner.Add("await CreateTest" + superModels.First().Name + "();");
        }
        else
        {
            liner.Add("await CreateTest" + m.Name + "();");
        }
    }
}
