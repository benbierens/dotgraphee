using System;

public abstract class BaseUnitTestGenerator : BaseGenerator
{
    protected BaseUnitTestGenerator(GeneratorConfig config) 
        : base(config)
    {
    }

    protected void AddTest(ClassMaker cm, string name, Action<Liner> liner)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public void " + name + "()", liner);
    }

    protected void AddAsyncTest(ClassMaker cm, string name, Action<Liner> liner)
    {
        cm.AddLine("[Test]");
        cm.AddClosure("public async Task " + name + "()", liner);
    }

    protected string GetDbAccessorName()
    {
        return Config.Database.DbAccesserClassName;
    }

    protected string GetMockDbServiceQueryableFunctionName()
    {
        return "Mock" + GetDbAccessorName() + "Queryable";
    }

    protected string GetMockDbServiceQueryableEntityFunctionName()
    {
        return GetMockDbServiceQueryableFunctionName() + "Entity";
    }
}
