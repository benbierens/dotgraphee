using System.Collections.Generic;

public class TestInputClassGenerator : BaseGenerator
{
    public TestInputClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateTestInputClass()
    {
        var fm = StartIntegrationTestUtilsFile("TestInput");
        fm.AddUsing(Config.GenerateNamespace + ".Client");
        fm.AddUsing("UnitTests");

        var cm = fm.AddClass("TestInput");
        cm.Modifiers.Clear();

        cm.AddLine("private readonly TestData testData;");
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

        cm.AddClosure("public " + inputNames.Create + " To" + inputNames.Create + "()", liner =>
        {
            liner.StartClosure("return new " + inputNames.Create);
            foreach (var f in m.Fields)
            {
                liner.Add(f.Name + " = testData." + m.Name + "1." + f.Name + ",");
            }
            foreach (var f in foreignProperties)
            {
                if (!f.IsRequiredSingular() && !f.IsSelfReference)
                {
                    liner.Add(f.WithId + " = testData." + f.Name + "1.Id,");
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
            liner.Add(m.Name + "Id = testData." + m.Name + "1.Id,");
            foreach (var f in m.Fields)
            {
                liner.Add(f.Name + " = testData." + f.Type.FirstToUpper() + ",");
            }
            var foreignProperties = GetForeignProperties(m);
            foreach (var f in foreignProperties)
            {
                if (!f.IsRequiredSingular())
                {
                    if (!f.IsSelfReference)
                    {
                        liner.Add(f.WithId + " = testData." + f.Name + "1.Id,");
                    }
                }
            }
            liner.EndClosure(";");
        });
    }

    private void AddToDeleteInputMethod(ClassMaker cm, GeneratorConfig.ModelConfig m, InputTypeNames inputNames)
    {
        if (IsRequiredSubModel(m)) return;
        cm.AddClosure("public " + Config.IdType + " To" + inputNames.Delete + "()", liner =>
        {
            liner.Add("return testData." + m.Name + "1.Id;");
        });
    }

    private void AddConstructor(ClassMaker cm)
    {
        cm.AddClosure("public TestInput(TestData testData)", liner =>
        {
            liner.Add("this.testData = testData;");
        });
    }
}
