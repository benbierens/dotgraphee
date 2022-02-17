using System.Collections.Generic;

public class BaseGqlTestClassGenerator : BaseTestGenerator
{
    public BaseGqlTestClassGenerator(GeneratorConfig config)
        : base(config)
    {
    }

    public void CreateBaseGqlTestClass()
    {
        var fm = StartTestUtilsFile("BaseGqlTest");
        AddBaseGqlTestClass(fm.AddClass("BaseGqlTest"));
        AddDockerInitializer(fm.AddClass("DockerInitializer"));
        fm.Build();
    }

    private void AddBaseGqlTestClass(ClassMaker cm)
    {
        cm.AddUsing("NUnit.Framework");
        cm.AddUsing("System.Threading.Tasks");

        cm.AddAttribute("Category(\"" + Config.Tests.TestCategory + "\")");

        cm.AddLine("[SetUp]");
        cm.AddClosure("public async Task GqlSetUp()", liner =>
        {
            liner.Add("TestData = new TestData();");
            liner.Add("await DockerController.Up();");
        });

        cm.AddLine("[TearDown]");
        cm.AddClosure("public async Task GqlTearDown()", liner =>
        {
            liner.Add("await Gql.CloseActiveSubscriptionHandles();");
            liner.Add("DockerController.Down();");
            liner.Add("DockerController.ClearData();");
        });

        cm.AddProperty("TestData")
            .IsType("TestData")
            .Build();

        cm.AddProperty("Gql")
            .IsType("Gql")
            .Build();

        cm.AddBlankLine();
        AddCreateTestModelMethods(cm);
    }

    private void AddCreateTestModelMethods(ClassMaker cm)
    {
        cm.AddUsing(Config.GenerateNamespace);

        IterateModelsInDependencyOrder(m =>
        {
            if (!IsTargetOfRequiredSingularRelation(m))
            {
                AddCreateTestModelMethod(cm, m);
            }
        });
    }

    private void AddCreateTestModelMethod(ClassMaker cm, GeneratorConfig.ModelConfig m)
    {
        var inputTypes = GetInputTypeNames(m);

        cm.AddClosure("public async Task<" + m.Name + "> CreateTest" + m.Name + "()", liner =>
        {
            var args = GetCreateInputArguments(liner, m);
            liner.Add("var gqlData = await Gql.Create" + m.Name + "(TestData.To" + inputTypes.Create + "(" + args + "));");
            AddAssert(liner).NoErrors();
            liner.Add("var entity = gqlData.Data." + Config.GraphQl.GqlMutationsCreateMethod + m.Name + ";");
            liner.Add("TestData.Test" + m.Name + ".Id = entity.Id;");
            liner.Add("return entity;");
        });
    }

    private string GetCreateInputArguments(Liner liner, GeneratorConfig.ModelConfig m)
    {
        var foreignProperties = GetForeignProperties(m);

        var arguments = new List<string>();
        foreach (var f in foreignProperties)
        {
            if (!f.IsSelfReference)
            {
                liner.Add("await CreateTest" + f.Type + "();");
                arguments.Add("TestData.Test" + f.Type + ".Id");
            }
            else
            {
                arguments.Add("null");
            }
        }

        return string.Join(", ", arguments);
    }

    private void AddDockerInitializer(ClassMaker cm)
    {
        cm.AddAttribute("Category(\"" + Config.Tests.TestCategory + "\")");
        cm.AddAttribute("SetUpFixture");

        cm.AddLine("[OneTimeSetUp]");
        cm.AddClosure("public async Task OneTimeGqlSetUp()", liner =>
        {
            liner.Add("await DockerController.BuildImage();");
        });

        cm.AddLine("[OneTimeTearDown]");
        cm.AddClosure("public void OneTimeGqlTearDown()", liner =>
        {
            liner.Add("DockerController.DeleteImage();");
        });
    }
}