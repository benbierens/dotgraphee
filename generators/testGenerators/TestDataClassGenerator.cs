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

        cm.AddProperty("TestBool")
            .IsType("bool")
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

        foreach (var m in Models)
        {
            var inputNames = GetInputTypeNames(m);
            AddToCreateInputMethod(cm, m, inputNames);
            AddToUpdateInputMethod(cm, m, inputNames);
            AddToDeleteInputMethod(cm, m, inputNames);
        }

        fm.Build();
    }

    private void AddToCreateInputMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
    {
        var foreignProperties = GetForeignProperties(m);
        var l = m.Name.FirstToLower();

        cm.AddClosure("public " + inputNames.Create + " To" + inputNames.Create + GetCreateArguments(foreignProperties), liner =>
        {
            liner.StartClosure("return new " + inputNames.Create);
            foreach (var f in m.Fields)
            {
                liner.Add(f.Name + " = Test" + m.Name + "." + f.Name + ",");
            }
            foreach (var f in foreignProperties)
            {
                liner.Add(f.WithId + " = " + f.WithId.FirstToLower() + ",");
            }
            liner.EndClosure(";");
        });
    }

    private void AddToUpdateInputMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
    {
        cm.AddClosure("public " + inputNames.Update + " To" + inputNames.Update + "()", liner =>
        {
            liner.StartClosure("return new " + inputNames.Update);
            liner.Add(m.Name + "Id = Test" + m.Name + ".Id,");
            foreach (var f in m.Fields)
            {
                liner.Add(f.Name + " = Test" + f.Type.FirstToUpper() + ",");
            }
            liner.EndClosure(";");
        });
    }

    private void AddToDeleteInputMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
    {
        cm.AddClosure("public " + inputNames.Delete + " To" + inputNames.Delete + "()", liner =>
        {
            liner.StartClosure("return new " + inputNames.Delete);
            liner.Add(m.Name + "Id = Test" + m.Name + ".Id,");
            liner.EndClosure(";");
        });
    }

    private string GetCreateArguments(ForeignProperty[] foreignProperties)
    {
        var args = new List<string>();
        foreach (var f in foreignProperties)
        {
            if (f.IsSelfReference)
            {
                args.Add(Config.IdType + "? " + f.WithId.FirstToLower());
            }
            else
            {
                args.Add(Config.IdType + " " + f.WithId.FirstToLower());
            }
        }

        return "(" + string.Join(", ", args) + ")";
    }

    private void AddConstructor(ClassMaker cm)
    {
        cm.AddClosure("public TestData()", liner =>
        {
            liner.Add("TestString = \"TestString\";");
            liner.Add("TestInt = 12345;");
            liner.Add("TestBool = true;");
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
