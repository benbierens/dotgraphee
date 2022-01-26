using System;
using System.Globalization;

public class TestDataClassGenerator : BaseGenerator
{
    private int dummyInt;
    private float dummyFloat;
    private double dummyDouble;

    public TestDataClassGenerator(GeneratorConfig config)
        : base(config)
    {
        dummyInt = 10000;
        dummyFloat = 10000.0f;
        dummyDouble = 10000.0;
    }

    public void CreateTestDataClass()
    {
        var fm = StartTestUtilsFile("TestData");
        var cm = fm.AddClass("TestData");
        cm.AddUsing("System");
        cm.AddUsing(Config.GenerateNamespace);

        cm.AddProperty("TestString")
            .IsType("string")
            .Build();

        cm.AddProperty("TestInt")
            .IsType("int")
            .Build();

        cm.AddProperty("TestFloat")
            .IsType("float")
            .Build();

        cm.AddProperty("TestDouble")
            .IsType("double")
            .Build();

        cm.AddProperty("TestDateTime")
            .IsType("DateTime")
            .Build();

        foreach (var m in Models)
        {
            cm.AddProperty("Test" + m.Name)
                .IsType(m.Name)
                .NoInitializer()
                .Build();
        }

        cm.AddBlankLine();

        AddConstructor(cm);

        fm.Build();
    }

    private void AddConstructor(ClassMaker cm)
    {
        cm.AddClosure("public TestData()", liner =>
        {
            liner.Add("TestString = \"TestString\";");
            liner.Add("TestInt = 12345;");
            liner.Add("TestFloat = 12.34f;");
            liner.Add("TestDouble = 23.45;");
            liner.Add("TestDateTime = new DateTime(2022, 1, 2, 11, 12, 13, DateTimeKind.Utc);");
            liner.AddBlankLine();

            IterateModelsInDependencyOrder(m =>
            {
                InitializeModel(liner, m);
            });
        });
    }

    private void InitializeModel(Liner liner, GeneratorConfig.ModelConfig m)
    {
        var foreign = GetForeignProperties(m);
        liner.StartClosure("Test" + m.Name + " = new " + m.Name);
        liner.Add("Id = " + DummyId() + ",");
        foreach (var f in m.Fields)
        {
            liner.Add(f.Name + " = " + DummyForType(m, f.Type, f.Name) + ",");
        }
        foreach (var f in foreign)
        {
            if (!f.IsSelfReference)
            {
                liner.Add(f.Name + " = Test" + f.Name + ",");
                liner.Add(f.WithId + " = Test" + f.Name + ".Id,");
            }
        }
        liner.EndClosure(";");
    }

    private string DummyId()
    {
        if (Config.IdType == "int") return DummyInt();
        if (Config.IdType == "string") return "\"" + Guid.NewGuid().ToString() + "\"";
        throw new Exception("Unknown ID type: " + Config.IdType);
    }

    private string DummyForType(GeneratorConfig.ModelConfig m, string type, string name)
    {
        if (type == "int") return DummyInt();
        if (type == "float") return DummyFloat();
        if (type == "string") return DummyString(m, name);
        if (type == "double") return DummyDouble();
        if (type == "bool") return "true";
        if (type == "DateTime") return DummyDateTime();
        throw new Exception("Unknown type: " + type);
    }

    private string DummyString(GeneratorConfig.ModelConfig m, string fieldName)
    {
        return "\"Test" + m.Name + "_" + fieldName + "\"";
    }

    private string DummyInt()
    {
        return (++dummyInt).ToString(CultureInfo.InvariantCulture);
    }

    private string DummyFloat()
    {
        dummyFloat += 0.1f;
        return dummyFloat.ToString(CultureInfo.InvariantCulture) + "f";
    }

    private string DummyDouble()
    {
        dummyDouble += 0.1;
        return dummyDouble.ToString(CultureInfo.InvariantCulture);
    }

    public string DummyDateTime()
    {
        return "DateTime.UtcNow";
    }

}
