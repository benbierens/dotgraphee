public class InclusionBuilderGenerator : BaseGenerator
{
    public InclusionBuilderGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateInclusionBuilderClass()
    {
        var fm = StartIntegrationTestUtilsFile("InclusionBuilder");
        var cm = fm.AddClass("InclusionBuilder<T>");
        cm.Modifiers.Clear();

        cm.AddUsing("Microsoft.EntityFrameworkCore.Infrastructure;");
        cm.AddUsing("System;");
        cm.AddUsing("System.Collections.Generic;");
        cm.AddUsing("System.Collections.Immutable;");
        cm.AddUsing("System.Linq;");
        cm.AddUsing("System.Linq.Expressions;");

        cm.AddLine("private readonly List<string> _inclusionPaths = new();");
        cm.AddBlankLine();

        cm.AddClosure("public class Result", liner =>
        {
            liner.Add("public ImmutableList<string> InclusionPaths { get; }");
            liner.StartClosure("public Result(List<string> inclusionPaths)");
            liner.Add("InclusionPaths = inclusionPaths.ToImmutableList();");
            liner.EndClosure();
            liner.Add("public bool IsIncluded(string path) => InclusionPaths.Contains(path);");
        });

        cm.AddClosure("public InclusionBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> selector)", liner =>
        {
            liner.Add("_inclusionPaths.Add(GetPropertyName(selector));");
            liner.Add("return this;");
        });


        cm.AddClosure("public InclusionBuilder<T> Include<TProperty>(Expression<Func<T, TProperty>> selector, Action<InclusionBuilder<TProperty>> nestedBuilder)", liner =>
        {
            liner.Add("var propertyName = GetPropertyName(selector);");
            liner.Add("var builder = new InclusionBuilder<TProperty>();");
            liner.Add("nestedBuilder(builder);");
            liner.Add("var nestedPaths = builder._inclusionPaths;");
            liner.Add("var paths = nestedPaths.Select(nestedPath => propertyName + \".\" + nestedPath);");
            liner.Add("_inclusionPaths.Add(propertyName);");
            liner.Add("_inclusionPaths.AddRange(paths);");
            liner.Add("return this;");
        });

        cm.AddClosure("public Result Build()", liner =>
        {
            liner.Add("return new Result(_inclusionPaths);");
        });

        cm.AddClosure("private string GetPropertyName<TProperty>(Expression<Func<T, TProperty>> selector)", liner =>
        {
            liner.Add("var propertyInfo = selector.GetPropertyAccess();");
            liner.Add("var propertyName = propertyInfo.Name;");
            liner.Add("return propertyName;");
        });

        fm.Build();
    }
}
