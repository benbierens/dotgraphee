using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class ConfigValidator
{
    public List<string> Errors { get; private set; } = new List<string>();

    public bool IsValid { get { return !Errors.Any(); } }

    public void Validate(GeneratorConfig config)
    {
        ValidateConfig(config.Config);
        ValidateModels(config.Models);

        if (!IsValid)
        {
            Log.Write("Configuration errors:");
            foreach (var error in Errors) Log.Write(error);
        }
    }

    private void ValidateConfig(GeneratorConfig.ConfigSection config)
    {
        ProcessCheckAttributes(config, "Root");
        ProcessCheckAttributes(config.Output, "Output");
        ProcessCheckAttributes(config.Database, "Database");
        ProcessCheckAttributes(config.Database.LocalDev, "Database.LocalDev");
        ProcessCheckAttributes(config.Database.Docker, "Database.Docker");
        ProcessCheckAttributes(config.GraphQl, "GraphQl");
        ProcessCheckAttributes(config.Tests, "Tests");
    }

    private void ValidateModels(GeneratorConfig.ModelConfig[] models)
    {
        if (!AreNamesUnique(models, m => m.Name))
        {
            Errors.Add("Duplicate model names found.");
        }
        foreach (var model in models) ValidateModel(models, model);
    }

    private void ValidateModel(GeneratorConfig.ModelConfig[] models, GeneratorConfig.ModelConfig model)
    {
        ProcessCheckAttributes(model, "Model");
        foreach (var f in model.Fields) ProcessCheckAttributes(f, model.Name);

        if (!AreNamesUnique(model.Fields, f => f.Name.ToLowerInvariant()))
        {
            Errors.Add("Model '" + model.Name + "' contains duplicate field names.");
        }

        if ((!model.Fields.Any()) && !HasAnyRelation(models, model))
        {
            Errors.Add("Model '" + model.Name + "' has 0 field entries and no relations.");
        }
    }

    private bool AreNamesUnique<T>(IEnumerable<T> elements, Func<T, string> getName)
    {
        var allNames = elements.Select(getName);
        var distinct = allNames.Distinct();
        return elements.Count() == distinct.Count();
    }

    private bool HasAnyRelation(GeneratorConfig.ModelConfig[] models, GeneratorConfig.ModelConfig m)
    {
        return m.HasMany.Any() || models.Any(m => m.HasMany.Contains(m.Name));
    }

    private void ProcessCheckAttributes(object target, string sectionName)
    {
        if (target == null)
        {
            Errors.Add("Missing section: " + sectionName);
            return;
        }

        var properties = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string));

        foreach (var p in properties)
        {
            var check = p.GetCustomAttribute<CheckAttribute>();
            if (check != null)
            {
                check.Validate(Errors, p.Name, p.GetValue(target));
            }
        }
    }
}
