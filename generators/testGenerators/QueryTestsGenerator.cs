
using System;

public class QueryTestsGenerator : BaseGenerator
{
    public QueryTestsGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateQueryTests()
    {
        var fm = StartTestFile("QueryTests");
        var cm = fm.AddClass("QueryTests");
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");
        cm.AddInherrit("BaseGqlTest");
        cm.Modifiers.Clear();

        foreach (var m in Models)
        {
            AddQueryAllTest(cm, m);
            AddQueryOneTest(cm, m);
        }
        
        fm.Build();
    }

    private void AddQueryAllTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldQueryAll" + m.Name + "s()", liner =>
        {
            liner.Add("await CreateTest" + m.Name + "();");
            liner.AddBlankLine();
            liner.Add("var all = await Gql.QueryAll" + m.Name + "s();");
            liner.AddBlankLine();
            AddAssertCollectionOne(liner, m);
            liner.AddBlankLine();

            liner.Add("var entity = all[0];");
            foreach (var f in m.Fields)
            {
                AddAssertEqualsTestEntity(liner, m, f, "All-query incorrect.");
            }
            var foreignProperties = GetForeignProperties(m);
            foreach (var f in foreignProperties)
            {
                if (!f.IsSelfReference)
                {
                    AddAssertIdEquals(liner, m, f, "All-query incorrect.");
                }
            }
        });
    }

    private void AddQueryOneTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldQueryOne" + m.Name + "ById()", liner =>
        {
            liner.Add("await CreateTest" + m.Name + "();");
            liner.AddBlankLine();
            liner.Add("var entity = await Gql.QueryOne" + m.Name + "(TestData.Test" + m.Name + ".Id);");
            liner.AddBlankLine();

            foreach (var f in m.Fields)
            {
                AddAssertEqualsTestEntity(liner, m, f, "One-query incorrect.");
            }
            var foreignProperties = GetForeignProperties(m);
            foreach (var f in foreignProperties)
            {
                if (!f.IsSelfReference)
                {
                    AddAssertIdEquals(liner, m, f, "One-query incorrect.");
                }
            }
        });
    }
}
