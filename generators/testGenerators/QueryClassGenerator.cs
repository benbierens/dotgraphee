public class QueryClassGenerator : BaseGenerator
{
    public QueryClassGenerator(GeneratorConfig config) : base(config)
    {
    }
    
    public void CreateQueryClasses()
    {
        var fm = StartTestUtilsFile("QueryClasses");
        CreateQueryDataClass(fm);

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

        cm.AddProperty("Data")
            .IsType("T")
            .IsNullable()
            .Build();

        var cm2 = AddClass(fm, "MutationResponse");
        cm2.AddProperty("Id")
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
