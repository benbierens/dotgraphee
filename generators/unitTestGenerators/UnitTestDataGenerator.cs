public class UnitTestDataGenerator : BaseGenerator
{
    public UnitTestDataGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateUnitTestData()
    {
        var fm = StartUnitTestFile("UnitTestData", "");
        fm.AddUsing(Config.GenerateNamespace);

        var cm = fm.AddClass("UnitTestData");
        foreach (var m in Models)
        {
            cm.AddProperty(m.Name + "1")
                .IsType(m.Name)
                id assigners!
                .Build();

            cm.AddProperty(m.Name + "2")
                .IsType(m.Name)
                .Build();
        }

        fm.Build();
    }
}
