using System;
using System.Collections.Generic;
using System.Globalization;

public class TestDataClassGenerator : BaseGenerator
{
    private int dummyInt;
    private float dummyFloat;
    private double dummyDouble;

    public TestDataClassGenerator(GeneratorConfig config)
        : base(config)
    {
        dummyInt = 100;
        dummyFloat = 100.0f;
        dummyDouble = 100.0;
    }

    public void GenerateTestData()
    {
        var fm = StartUnitTestUtilsFile("TestData");
        fm.AddUsing(Config.GenerateNamespace);
        fm.AddUsing("System");
        var cm = fm.AddClass("TestData");
        cm.Modifiers.Clear();

        cm.AddProperty("String")
            .IsType("string")
            .Build();

        cm.AddProperty("Int")
            .IsType("int")
            .Build();

        cm.AddProperty("Bool")
            .IsType("bool")
            .Build();

        cm.AddProperty("Float")
            .IsType("float")
            .Build();

        cm.AddProperty("Double")
            .IsType("double")
            .Build();

        cm.AddProperty("DateTime")
            .IsType("DateTime")
            .Build();

        foreach (var m in Models)
        {
            cm.AddProperty(m.Name + "1")
                .IsType(m.Name)
                .NoInitializer()
                .Build();

            cm.AddProperty(m.Name + "2")
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
            liner.Add("String = \"TestString\";");
            liner.Add("Int = 12345;");
            liner.Add("Bool = true;");
            liner.Add("Float = 12.34f;");
            liner.Add("Double = 23.45;");
            liner.Add("DateTime = new DateTime(2022, 1, 2, 11, 12, 13, DateTimeKind.Utc);");
            liner.AddBlankLine();
            IterateModelsInDependencyOrder(m =>
            {
                InitializeModel(liner, m, "1");
                InitializeModel(liner, m, "2");
            });
        });
    }

    private void InitializeModel(Liner liner, GeneratorConfig.ModelConfig m, string propertyPostfix)
    {
        var foreign = GetForeignProperties(m);
        liner.StartClosure(m.Name + propertyPostfix + " = new " + m.Name);
        liner.Add("Id = " + DummyId() + ",");
        foreach (var f in m.Fields)
        {
            liner.Add(f.Name + " = " + DummyForType(m, f.Type, f.Name, propertyPostfix) + ",");
        }
        foreach (var f in foreign)
        {
            if (!f.IsSelfReference)
            {
                liner.Add(f.Name + " = " + f.Name + propertyPostfix + ",");
                liner.Add(f.WithId + " = " + f.Name + propertyPostfix + ".Id,"); 
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

    private string DummyForType(GeneratorConfig.ModelConfig m, string type, string name, string propertyPostfix)
    {
        if (type == "int") return DummyInt();
        if (type == "float") return DummyFloat();
        if (type == "string") return DummyString(m, name, propertyPostfix);
        if (type == "double") return DummyDouble();
        if (type == "bool") return "true";
        if (type == "DateTime") return DummyDateTime();
        throw new Exception("Unknown type: " + type);
    }

    private string DummyString(GeneratorConfig.ModelConfig m, string fieldName, string propertyPostfix)
    {
        return "\"Test" + m.Name + propertyPostfix + "_" + fieldName + "\"";
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
