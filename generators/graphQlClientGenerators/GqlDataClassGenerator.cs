public class GqlDataClassGenerator : BaseGenerator
{
    public GqlDataClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateGqlDataClass()
    {
        var fm = StartClientFile("GqlData");
        var cm = fm.AddClass("GqlData<T>");
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

        cm.AddBlankLine();
        cm.AddClosure("public void AssertNoErrors()", liner => {
            liner.StartClosure("if (Errors.Any())");
            liner.Add("throw new Exception(\"Expected no errors but found: \" + string.Join(\", \", Errors.Select(e => e.Message)));");
            liner.EndClosure();
        });

        fm.Build();
    }

}
