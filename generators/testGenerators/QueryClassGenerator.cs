using System.Linq;

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
        CreateNodesWrapperClass(fm);

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

        if (m.HasPagingFeature())
        {
            cm.AddProperty(m.Name + "s")
                .IsType("NodesWrapper<" + m.Name + ">")
                .Build();
        }
        else 
        { 
            cm.AddProperty(m.Name)
                .IsListOfType(m.Name)
                .Build();
        }
    }

    private void CreateOneQueryClass(FileMaker fm, GeneratorConfig.ModelConfig m)
    {
        var cm = AddClass(fm, "One" + m.Name + "Query");
        cm.AddProperty(m.Name)
            .IsType(m.Name)
            .IsNullable()
            .Build();
    }

    private void CreateMutationResponseClassesForModel(FileMaker fm, GeneratorConfig.ModelConfig m)
    {
        AddMutationResponseClass(fm, m, m.Name, Config.GraphQl.GqlMutationsCreateMethod);
        AddMutationNullableResponseClass(fm, m, m.Name, Config.GraphQl.GqlMutationsUpdateMethod);
        AddMutationNullableResponseClass(fm, m, Config.IdType, Config.GraphQl.GqlMutationsDeleteMethod);
    }

    private void AddMutationResponseClass(FileMaker fm, GeneratorConfig.ModelConfig m, string type, string mutationMethod)
    {
        var cm = AddClass(fm, mutationMethod + m.Name + "Response");
        cm.AddProperty(mutationMethod + m.Name)
            .IsType(type)
            .Build();
    }

    private void AddMutationNullableResponseClass(FileMaker fm, GeneratorConfig.ModelConfig m, string type, string mutationMethod)
    {
        if (IsFailedToFindStrategyNullObject())
        {
            var cm = AddClass(fm, mutationMethod + m.Name + "Response");
            cm.AddProperty(mutationMethod + m.Name)
                .IsType(type)
                .IsNullable()
                .Build();
        }
        else
        {
            AddMutationResponseClass(fm, m, type, mutationMethod);
        }
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

    private void CreateNodesWrapperClass(FileMaker fm)
    {
        if (!Models.Any(m => m.HasPagingFeature())) return;

        var cm = AddClass(fm, "NodesWrapper<T>");
        cm.AddProperty("Node")
            .IsListOfType("T")
            .Build();
    }

    private static ClassMaker AddClass(FileMaker fm, string name)
    {
        var cm = fm.AddClass(name);
        cm.Modifiers.Clear();
        return cm;
    }
}
