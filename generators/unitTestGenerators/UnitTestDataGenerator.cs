using System;
using System.Collections.Generic;
using System.Globalization;

public class UnitTestDataGenerator : BaseGenerator
{
    private int dummyInt;
    private float dummyFloat;
    private double dummyDouble;

    public UnitTestDataGenerator(GeneratorConfig config)
        : base(config)
    {
        dummyInt = 100;
        dummyFloat = 100.0f;
        dummyDouble = 100.0;
    }

    public void GenerateUnitTestData()
    {
        var fm = StartUnitTestUtilsFile("UnitTestData");
        fm.AddUsing(Config.GenerateNamespace);
        fm.AddUsing("System");
        var cm = fm.AddClass("UnitTestData");
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
                liner.Add(f.Name + " = " + m.Name + "1." + f.Name + ",");
            }
            foreach (var f in foreignProperties)
            {
                if (!f.IsRequiredSingular())
                {
                    liner.Add(f.WithId + " = " + f.WithId.FirstToLower() + ",");
                }
            }
            var requiredSubModels = GetMyRequiredSubModels(m);
            foreach (var subModel in requiredSubModels)
            {
                var subModelInputNames = GetInputTypeNames(subModel);
                liner.Add(subModel.Name + " = To" + subModelInputNames.Create + "(),");
            }
            liner.EndClosure(";");
        });
    }

    private void AddToUpdateInputMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
    {
        cm.AddClosure("public " + inputNames.Update + " To" + inputNames.Update + "()", liner =>
        {
            liner.StartClosure("return new " + inputNames.Update);
            liner.Add(m.Name + "Id = " + m.Name + "1.Id,");
            foreach (var f in m.Fields)
            {
                liner.Add(f.Name + " = " + f.Type.FirstToUpper() + ",");
            }
            var foreignProperties = GetForeignProperties(m);
            foreach (var f in foreignProperties)
            {
                if (!f.IsRequiredSingular())
                {
                    if (!f.IsSelfReference)
                    {
                        liner.Add(f.WithId + " = " + f.Name + "1.Id,");
                    }
                }
            }
            liner.EndClosure(";");
        });
    }

    private void AddToDeleteInputMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
    {
        if (IsRequiredSubModel(m)) return;
        cm.AddClosure("public " + inputNames.Delete + " To" + inputNames.Delete + "()", liner =>
        {
            liner.StartClosure("return new " + inputNames.Delete);
            liner.Add(m.Name + "Id = " + m.Name + "1.Id,");
            liner.EndClosure(";");
        });
    }

    private string GetCreateArguments(ForeignProperty[] foreignProperties)
    {
        var args = new List<string>();
        foreach (var f in foreignProperties)
        {
            if (!f.IsRequiredSingular())
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
        }

        return "(" + string.Join(", ", args) + ")";
    }

    private void AddConstructor(ClassMaker cm)
    {
        cm.AddClosure("public UnitTestData()", liner =>
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
