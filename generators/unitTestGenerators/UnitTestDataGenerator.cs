using System;

public class UnitTestDataGenerator : BaseGenerator
{
    public UnitTestDataGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    private int intId = 100;

    public void GenerateUnitTestData()
    {
        var fm = StartUnitTestUtilsFile("UnitTestData");
        fm.AddUsing(Config.GenerateNamespace);

        var cm = fm.AddClass("UnitTestData");
        foreach (var m in Models)
        {
            cm.AddProperty(m.Name + "1")
                .IsType(m.Name)
                .WithCustomInitializer(" = new " + m.Name + "() { Id = " + GetUniqueId() + " }")
                .Build();

            cm.AddProperty(m.Name + "2")
                .IsType(m.Name)
                .WithCustomInitializer(" = new " + m.Name + "() { Id = " + GetUniqueId() + " }")
                .Build();
        }

        fm.Build();
    }

    private string GetUniqueId()
    {
        if (Config.IdType == "int")
        {
            intId++;
            return intId.ToString();
        }
        else if (Config.IdType == "string")
        {
            return Guid.NewGuid().ToString();
        }
        else
        {
            throw new Exception("Unknown IdType: " + Config.IdType);
        }
    }
}
