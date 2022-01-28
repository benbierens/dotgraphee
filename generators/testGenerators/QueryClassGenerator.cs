public class QueryClassGenerator : BaseGenerator
{
    public QueryClassGenerator(GeneratorConfig config) : base(config)
    {
    }
    
    public void CreateQueryClasses()
    {
        var fm = StartTestUtilsFile("QueryClasses");
        CreateQueryDataClass(fm);
        CreateQueryErrorClass(fm);
        CreateDeleteMutationResponseClass(fm);

        foreach (var m in Models)
        {
            CreateQueryClassForModel(fm, m);
            CreateMutationResponseClassesForModel(fm, m);
        }

        fm.Build();
    }

    private void CreateQueryClassForModel(FileMaker fm, GeneratorConfig.ModelConfig m)
    {
        CreateAllQueryClass(fm, m);
        CreateOneQueryClass(fm, m);
    }

    private void CreateAllQueryClass(FileMaker fm, GeneratorConfig.ModelConfig m)
    {
        var cm = AddClass(fm, "All" + m.Name + "sQuery");
        cm.AddUsing("System.Collections.Generic");
        cm.AddProperty(m.Name)
            .IsListOfType(m.Name)
            .Build();
    }

    private void CreateOneQueryClass(FileMaker fm, GeneratorConfig.ModelConfig m)
    {
        var cm = AddClass(fm, "One" + m.Name + "Query");
        cm.AddProperty(m.Name)
            .IsType(m.Name)
            .Build();
    }

    private void CreateMutationResponseClassesForModel(FileMaker fm, GeneratorConfig.ModelConfig m)
    {
        AddMutationResponseClass(fm, m, Config.GraphQl.GqlMutationsCreateMethod);
        AddMutationResponseClass(fm, m, Config.GraphQl.GqlMutationsUpdateMethod);
    }

    private void AddMutationResponseClass(FileMaker fm, GeneratorConfig.ModelConfig m, string mutationMethod)
    {
        var cm = AddClass(fm, mutationMethod + m.Name + "Response");
        cm.AddProperty(mutationMethod + m.Name)
            .IsType(m.Name)
            .Build();
    }

    private void CreateQueryDataClass(FileMaker fm)
    {
        var cm = AddClass(fm, "GqlData<T>");
        cm.AddUsing(Config.GenerateNamespace);
        cm.AddUsing("System");
        cm.AddUsing("System.Linq");

        cm.AddProperty("Data")
            .IsType("T")
            .DefaultInitializer()
            .Build();

        cm.AddProperty("Error")
            .IsListOfType("GqlError")
            .Build();


        cm.AddClosure("public void AssertNoErrors()", liner =>
        {
            liner.StartClosure("if (Errors.Any())");
            liner.Add("throw new Exception(\"Expected no errors but found: \" + string.Join(\", \", Errors.Select(e => e.Message)));");
            liner.EndClosure();
        });
    }

    private void CreateQueryErrorClass(FileMaker fm)
    {
        var cm = AddClass(fm, "GqlError");
        cm.AddProperty("Message")
            .IsType("string")
            .Build();
    }

    private void CreateDeleteMutationResponseClass(FileMaker fm)
    {
        var cm = AddClass(fm, "DeleteMutationResponse");
        cm.AddProperty("Id")
            .IsType(Config.IdType)
            .Build();
    }

    private static ClassMaker AddClass(FileMaker fm, string name)
    {
        var cm = fm.AddClass(name);
        cm.Modifiers.Clear();
        return cm;
    }
}
