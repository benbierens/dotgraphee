
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
        }
        
        fm.Build();
    }

    private void AddQueryAllTest(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public async Task ShouldQuery" + m.Name + "()", liner =>
        {
            liner.Add("await CreateTest" + m.Name + "();");
            liner.AddBlankLine();
            liner.Add("var all = await Gql.QueryAll" + m.Name + "s();");
            liner.AddBlankLine();
            liner.Add("Assert.That(all.Count, Is.EqualTo(1), \"Expected only 1 " + m.Name + "\");");

            foreach (var f in m.Fields)
            {
                liner.Add("Assert.That(all[0]." + f.Name + ", Is.EqualTo(TestData.Test" + m.Name + "." + f.Name + "), \"Queried incorrect " + m.Name + "." + f.Name + "\");");
            }
            var foreignProperties = GetForeignProperties(m);
            foreach (var f in foreignProperties)
            {
                if (!f.IsSelfReference)
                {
                    liner.Add("Assert.That(all[0]." + f.WithId + ", Is.EqualTo(TestData.Test" + f.Type + ".Id), \"Queried incorrect " + m.Name + "." + f.WithId + "\");");
                }
            }
        });
    }
}
