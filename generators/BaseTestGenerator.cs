public abstract class BaseTestGenerator : BaseGenerator
{
    public BaseTestGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    protected AssertMaker AddAssert(Liner liner, string target = "entity")
    {
        return new AssertMaker(this, liner, target);
    }

    protected void AddCreateLine(Liner liner, GeneratorConfig.ModelConfig m)
    {
        if (IsRequiredSubModel(m))
        {
            var requiringModels = GetMyRequiredSuperModels(m);
            liner.Add("await CreateTest" + requiringModels[0].Name + "();");
        }
        else
        {
            liner.Add("await CreateTest" + m.Name + "();");
        }
    }

    protected string GetDereferenceForGqlData(GeneratorConfig.ModelConfig m)
    {
        if (!m.HasPagingFeature())
        {
            return ".Data?." + m.Name + "s";
        }
        return ".Data?." + m.Name + "s?.Nodes";
    }

    protected void AddDereferenceToAllVariable(Liner liner, GeneratorConfig.ModelConfig m)
    {
        liner.Add("var all = gqlData" + GetDereferenceForGqlData(m) + ";");
    }
}
