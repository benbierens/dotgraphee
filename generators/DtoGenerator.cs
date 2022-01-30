using System.Linq;

public class DtoGenerator : BaseGenerator
{
    public DtoGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void GenerateDtos()
    {
        MakeSrcDir(Config.Output.GeneratedFolder, Config.Output.DtoSubFolder);

        foreach (var model in Models)
        {
            var fm = StartSrcFile(Config.Output.DtoSubFolder, model.Name);

            var cm = StartClass(fm, model.Name);
            AddDtoInherritance(cm);

            if (model.HasMany.Any())
            {
                cm.AddUsing("System.Collections.Generic");
            }

            cm.AddProperty("Id")
                .IsType(Config.IdType)
                .Build();

            AddModelFields(cm, model);

            foreach (var m in model.HasMany)
            {
                cm.AddProperty(m)
                    .WithModifier("virtual")
                    .IsListOfType(m)
                    .Build();
            }
            AddForeignProperties(cm, model);

            fm.Build();
        }
    }

    private void AddDtoInherritance(ClassMaker cm)
    {
        if (Config.IdType == "string")
        {
            cm.AddInherrit("IHasId");
        }
    }

    private void AddForeignProperties(ClassMaker cm, GeneratorConfig.ModelConfig model)
    {
        var foreignProperties = GetForeignProperties(model);
        foreach (var f in foreignProperties)
        {
            if (f.IsSelfReference)
            {
                AddNullableForeignProperties(cm, f);
            }
            else
            {
                AddExplicitForeignProperties(cm, f);
            }
        }
    }

    private void AddExplicitForeignProperties(ClassMaker cm, ForeignProperty f)
    {
        cm.AddProperty(f.WithId)
            .IsType(Config.IdType)
            .Build();

        cm.AddProperty(f.Name)
            .WithModifier("virtual")
            .IsType(f.Type)
            .InitializeAsExplicitNull()
            .Build();
    }

    private void AddNullableForeignProperties(ClassMaker cm, ForeignProperty f)
    {
        cm.AddProperty(f.WithId)
            .IsType(Config.IdType)
            .IsNullable()
            .Build();

        cm.AddProperty(f.Name)
            .WithModifier("virtual")
            .IsType(f.Type)
            .IsNullable()
            .Build();
    }
}