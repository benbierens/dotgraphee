using System.Collections.Generic;

public class ConverterClassGenerator : BaseGenerator
{
    public ConverterClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateConverterClass()
    {
        var fm = StartTestUtilsFile("Converters");
        var cm = fm.AddClass("Converters");
        cm.Modifiers.Clear();
        cm.Modifiers.Add("static");
        cm.AddUsing(Config.GenerateNamespace);

        foreach (var m in Models)
        {
            AddConvertMethods(cm, m);
        }

        fm.Build();
    }

    private void AddConvertMethods(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        var inputTypes = GetInputTypeNames(model);
        var l = model.Name.FirstToLower();
        var foreignProperties = GetForeignProperties(model);

        cm.AddClosure("public static " + inputTypes.Create + " To" + Config.GraphQl.GqlMutationsCreateMethod + GetCreateArguments(model, l, foreignProperties), liner =>
        {
            liner.StartClosure("return new " + inputTypes.Create);
            foreach (var f in model.Fields)
            {
                liner.Add(f.Name + " = " + l + "." + f.Name + ",");
            }
            foreach (var f in foreignProperties)
            {
                liner.Add(f.WithId + " = " + f.WithId.FirstToLower() + ",");
            }
            liner.EndClosure(";");
        });
    }

    private string GetCreateArguments(GeneratorConfig.ModelConfig model, string l, ForeignProperty[] foreignProperties)
    {
        var args = new List<string>();
        args.Add("this " + model.Name + " " + l);
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














    //private void AddConvertMethod(ClassMaker cm, GeneratorConfig.ModelConfig model, string inputType, string mutationMethod, string firstLine = "")
    //{
    //    var l = model.Name.FirstToLower();
    //    cm.AddClosure("public static " + inputType + " To" + mutationMethod + "(this " + model.Name + " " + l + ")", liner =>
    //    {
    //        liner.StartClosure("return new " + inputType);
    //        if (!string.IsNullOrWhiteSpace(firstLine)) liner.Add(firstLine);
    //        foreach (var f in model.Fields)
    //        {
    //            liner.Add(f.Name + " = " + l + "." + f.Name + ",");
    //        }
    //        var foreignProperties = GetForeignProperties(model);
    //        foreach (var f in foreignProperties)
    //        {
    //            liner.Add(f.WithId + " = " + l + "." + f.WithId + ",");
    //        }
    //        liner.EndClosure(";");
    //    });
    //}

    //private void AddCreateConvertMethod(ClassMaker cm, GeneratorConfig.ModelConfig model, InputTypeNames inputType, string mutationMethod, string firstLine = "")
    //{

    //}
}
